using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;
using MyProjectBase.Helpers;
using MyProjectBase.Models;
using MyProjectBase.Services;

namespace MyProjectBase.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // Page UI active
    [ObservableProperty] 
    private ViewModelBase _currentPage;

    // Services applicatifs
    private readonly CsvServices _csvService;
    private readonly JSONServices _jsonService;

    // Référence à la fenêtre principale (pour MessageBox)
    private readonly Window _topLevelWindow;

    public MainWindowViewModel(TopLevel topLevel)
    {
        _csvService = new CsvServices(topLevel);
        _jsonService = new JSONServices();
        _topLevelWindow = (Window)topLevel;
        
        // ✅ Affiche immédiatement la liste avec la bonne commande
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand); 

        // Chargement JSON automatique au démarrage
        _ = LoadDataFromServer();
    }

    partial void OnCurrentPageChanging(ViewModelBase? oldValue, ViewModelBase? newValue)
    {
        oldValue?.Dispose();
    }

    // ✅ Navigation vers détails d’un produit
    [RelayCommand]
    private void GoToDetailsFromChild(string productId)
    {
        CurrentPage = new CollectionDetailsViewModel(productId);
    }

    // ✅ Retour à la vue collection
    [RelayCommand]
    private void BackToMain()
    {
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand);
    }

    /// <summary>
    /// ✅ Charge la liste des produits depuis l’API JSON locale.
    /// Ajoute un placeholder si le serveur renvoie une liste vide.
    /// </summary>
    


    private async Task LoadDataFromServer()
    {
        try
        {
            var data = await _jsonService.GetProductsAsync();

            MyGlobals.ProductsFish.Clear();

            if (data.Count > 0)
            {
                foreach (var p in data)
                {
                    // ✅ Charger l'image depuis PicturePath
                    if (!string.IsNullOrWhiteSpace(p.PicturePath))
                    {
                        try
                        {
                            // ✅ Image locale de type AVARES
                            if (p.PicturePath.StartsWith("avares://"))
                            {
                                p.Picture = ImageHelper.LoadFromResource(new Uri(p.PicturePath));
                            }
                        }
                        catch
                        {
                            p.Picture = null; // Pas d'image → pas de crash
                        }
                    }

                    MyGlobals.ProductsFish.Add(p);
                }
            }
            else
            {
                Console.WriteLine("⚠ API vide : aucun poisson trouvé.");

                MyGlobals.ProductsFish.Add(new ProductFish
                {
                    ID = "N/A",
                    Name = "Aucun poisson disponible",
                    Group = "API vide",
                    Stock = 0,
                    Price = 0,
                    Description = "Aucune donnée disponible",
                    PicturePath = "avares://MyProjectBase/Assets/placeholder.png",
                    Picture = ImageHelper.LoadFromResource(
                        new Uri("avares://MyProjectBase/Assets/placeholder.png"))
                });
            }
        }
        catch (HttpRequestException)
        {
            await DialogHelper.ShowError(_topLevelWindow,
                "Impossible de contacter le serveur JSON.\nAssurez-vous que le serveur est disponible.");

            MyGlobals.ProductsFish.Clear();
            MyGlobals.ProductsFish.Add(new ProductFish
            {
                ID = "ERR",
                Name = "Serveur inaccessible",
                Group = "API DOWN",
                Stock = 0,
                Price = 0
            });
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowError(_topLevelWindow,
                "Erreur inattendue lors du chargement JSON :\n" + ex.Message);
        }

        // ✅ Mise à jour de la page
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand);
    }


    /// <summary>
    /// ✅ Envoie la liste de produits à l’API JSON.
    /// </summary>
    [RelayCommand]
    private async Task SaveJson()
    {
        try
        {
            await _jsonService.SetProductsAsync(MyGlobals.ProductsFish.ToList());
            Console.WriteLine("✅ Sauvegarde JSON réussie !");
        }
        catch (HttpRequestException)
        {
            await DialogHelper.ShowError(_topLevelWindow,
                "Impossible de contacter le serveur JSON.\nAssurez-vous que Docker tourne.");
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowError(_topLevelWindow,
                "Erreur inattendue lors de la sauvegarde JSON :\n" + ex.Message);
        }
    }

    // ✅ Import CSV
    [RelayCommand]
    private async Task ImportCsv()
    {
        var data = await _csvService.LoadDataAsync<ProductFish>();

        if (data.Count == 0)
            return;

        MyGlobals.ProductsFish.Clear();
        foreach (var p in data)
            MyGlobals.ProductsFish.Add(p);

        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand);
    }

    // ✅ Export CSV
    [RelayCommand]
    private async Task ExportCsv()
    {
        await _csvService.SaveDataAsync(MyGlobals.ProductsFish.ToList());
    }
}