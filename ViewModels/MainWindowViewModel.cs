using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using OceanStock.Helpers;
using OceanStock.Models;
using OceanStock.Services;

namespace OceanStock.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage;

    private readonly CsvServices _csvService;
    private readonly JSONServices _jsonService;
    private readonly Window _topLevelWindow;

    public TopLevel TopLevel => _topLevelWindow;
    public bool IsLoggedIn => Session.CurrentUser != null;
    public bool IsAdmin => Session.CurrentUser?.IsAdmin == true;

    public MainWindowViewModel(TopLevel topLevel)
    {
        _csvService = new CsvServices(topLevel);
        _jsonService = new JSONServices();
        _topLevelWindow = (Window)topLevel;

        CurrentPage = new LoginViewModel(this);

        _ = LoadDataFromServer();
    }

    partial void OnCurrentPageChanging(ViewModelBase? oldValue, ViewModelBase newValue)
    {
        oldValue?.Dispose();
    }

    [RelayCommand]
    private void GoToDetailsFromChild(string productId)
    {
        CurrentPage = new CollectionDetailsViewModel(productId, this);
    }

    [RelayCommand]
    private void BackToMain()
    {
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand, this);
    }

    [RelayCommand]
    public void ShowMyCollection()
    {
        Session.SelectedUserIds = null;
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand, this);
    }

    private async Task LoadDataFromServer()
    {
        List<ProductFish> data;

        try
        {
            data = await _jsonService.GetProductsAsync();
        }
        catch (HttpRequestException)
        {
            await DialogHelper.ShowError(
                _topLevelWindow,
                "Impossible de contacter le serveur JSON.\nAssurez-vous que le serveur est disponible.");

            MyGlobals.ProductsFish.Clear();
            MyGlobals.ProductsFish.Add(new ProductFish
            {
                Id = "ERR",
                Name = "Serveur inaccessible",
                Group = "Erreur réseau",
                Stock = 0,
                Price = 0
            });
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erreur chargement JSON : " + ex.Message);
            return;
        }

        if (data.Count == 0)
        {
            MyGlobals.ProductsFish.Clear();
            MyGlobals.ProductsFish.Add(new ProductFish
            {
                Id = "N/A",
                Name = "Aucun poisson disponible",
                Group = "Serveur vide",
                Stock = 0,
                Price = 0,
                Description = "Aucune donnée disponible"
            });
            return;
        }

        // Étape 1 : chargement des images (sans toucher à MyGlobals)
        bool pathsUpdated = false;

        foreach (var p in data)
        {
            if (!string.IsNullOrWhiteSpace(p.PicturePath))
            {
                try
                {
                    var diskPath = ImageHelper.EnsureInAssets(p.PicturePath, p.Id);
                    if (!string.IsNullOrEmpty(diskPath) && File.Exists(diskPath))
                    {
                        if (p.PicturePath != diskPath)
                        {
                            p.PicturePath = diskPath;
                            pathsUpdated = true;
                        }
                        p.Picture = new Bitmap(diskPath);
                    }
                    else
                    {
                        p.Picture = null;
                    }
                }
                catch
                {
                    p.Picture = null;
                }
            }
        }

        if (pathsUpdated)
        {
            try { await _jsonService.SetProductsAsync(data); }
            catch { /* non bloquant */ }
        }

        // Étape 2 : sync MongoDB et récupération IdOwner AVANT d'afficher
        var db = DatabaseServices.Instance;

        foreach (var fish in data)
        {
            var existing = await db.GetFishByCustomIdAsync(fish.Id);

            if (existing == null)
            {
                await db.CreateFishAsync(fish);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(fish.IdOwner))
                    fish.IdOwner = existing.IdOwner;

                await db.UpdateFishAsync(fish);
            }
        }

        // Étape 3 : ajout dans MyGlobals SEULEMENT après que IdOwner est correct
        MyGlobals.ProductsFish.Clear();

        foreach (var p in data)
            MyGlobals.ProductsFish.Add(p);
    }

    [RelayCommand]
    private async Task SaveJson()
    {
        try
        {
            await _jsonService.SetProductsAsync(MyGlobals.ProductsFish.ToList());
        }
        catch (HttpRequestException)
        {
            await DialogHelper.ShowError(
                _topLevelWindow,
                "Impossible de contacter le serveur JSON.\nAssurez-vous que le serveur est disponible.");
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowError(
                _topLevelWindow,
                "Erreur inattendue lors de la sauvegarde : " + ex.Message);
        }

        var db = DatabaseServices.Instance;

        foreach (var fish in MyGlobals.ProductsFish)
        {
            var existing = await db.GetFishByCustomIdAsync(fish.Id);

            if (existing == null)
            {
                await db.CreateFishAsync(fish);
            }
            else
            {
                bool hasChanged =
                    existing.Name != fish.Name ||
                    existing.Group != fish.Group ||
                    existing.Stock != fish.Stock ||
                    existing.Price != fish.Price ||
                    existing.Description != fish.Description ||
                    existing.PicturePath != fish.PicturePath ||
                    existing.IdOwner != fish.IdOwner;

                if (hasChanged)
                    await db.UpdateFishAsync(fish);
            }
        }

        var mongoFish = await db.GetFishAsync();

        foreach (var mongoItem in mongoFish)
        {
            bool existsInJson = MyGlobals.ProductsFish.Any(f => f.Id == mongoItem.Id);

            if (!existsInJson)
                await db.DeleteFishAsync(mongoItem.Id);
        }
    }

    [RelayCommand]
    private async Task ImportCsv()
    {
        var imported = await _csvService.LoadDataAsync<ProductFish>();

        foreach (var fish in imported)
            fish.IdOwner = Session.CurrentUser!.Id;

        if (imported.Count == 0)
            return;

        CurrentPage = new ImportPreviewViewModel(imported, MyGlobals.ProductsFish, this);
    }

    [RelayCommand]
    private void ExportCsv()
    {
        List<ProductFish> filteredFish;

        if (Session.SelectedUserIds != null && Session.SelectedUserIds.Any())
        {
            filteredFish = MyGlobals.ProductsFish
                .Where(p => Session.SelectedUserIds.Contains(p.IdOwner))
                .ToList();
        }
        else
        {
            filteredFish = MyGlobals.ProductsFish
                .Where(p => p.IdOwner == Session.CurrentUser?.Id)
                .ToList();
        }

        CurrentPage = new ExportPreviewViewModel(filteredFish, this, _csvService);
    }

    public async Task DeleteFishAsync(ProductFish fish)
    {
        var confirmed = await DialogHelper.ShowConfirm(
            _topLevelWindow,
            $"Voulez-vous vraiment supprimer {fish.Name} ?");

        if (!confirmed)
            return;

        MyGlobals.ProductsFish.Remove(fish);
        await SaveJson();

        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand, this);
    }

    public void GoToEditFish(ProductFish fish)
    {
        CurrentPage = new EditFishViewModel(fish, this);
    }

    [RelayCommand]
    public void GoToAddFish()
    {
        CurrentPage = new AddFishViewModel(this);
    }

    [RelayCommand]
    private void GoToProfile()
    {
        CurrentPage = new ProfileViewModel(this);
    }

    [RelayCommand]
    private void GoToStats()
    {
        CurrentPage = new StatsViewModel(this);
    }

    [RelayCommand]
    private void Logout()
    {
        Session.CurrentUser = null;
        Session.SelectedUserIds = null;
        RefreshIsAdmin();
        CurrentPage = new LoginViewModel(this);
    }

    public void RefreshIsAdmin()
    {
        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(IsLoggedIn));
    }

    [RelayCommand]
    private void GoToAdmin()
    {
        if (Session.CurrentUser?.IsAdmin != true)
            return;

        CurrentPage = new AdminViewModel(this);
    }
}
