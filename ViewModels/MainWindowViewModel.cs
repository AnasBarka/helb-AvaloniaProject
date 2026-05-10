using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using OceanStock.Helpers;
using OceanStock.Models;
using OceanStock.Services;

namespace OceanStock.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // =====================================================
    // PAGE ACTUELLE
    // =====================================================

    [ObservableProperty]
    private ViewModelBase _currentPage;

    // =====================================================
    // SERVICES
    // =====================================================

    private readonly CsvServices _csvService;
    private readonly JSONServices _jsonService;

    // =====================================================
    // FENÊTRE PRINCIPALE
    // =====================================================

    private readonly Window _topLevelWindow;

    public TopLevel TopLevel => _topLevelWindow;

    // =====================================================
    // ADMIN
    // =====================================================

    public bool IsAdmin => Session.CurrentUser?.IsAdmin == true;

    // =====================================================
    // CONSTRUCTEUR
    // =====================================================

    public MainWindowViewModel(TopLevel topLevel)
    {
        _csvService = new CsvServices(topLevel);
        _jsonService = new JSONServices();

        _topLevelWindow = (Window)topLevel;

        // ✅ Page de départ
        CurrentPage = new LoginViewModel(this);

        // ✅ Chargement JSON au démarrage
        _ = LoadDataFromServer();
    }

    // =====================================================
    // CLEAN PAGE
    // =====================================================

    partial void OnCurrentPageChanging(
        ViewModelBase? oldValue,
        ViewModelBase? newValue)
    {
        oldValue?.Dispose();
    }

    // =====================================================
    // NAVIGATION DETAILS
    // =====================================================

    [RelayCommand]
    private void GoToDetailsFromChild(string productId)
    {
        CurrentPage = new CollectionDetailsViewModel(
            productId,
            this);
    }

    // =====================================================
    // RETOUR COLLECTION
    // =====================================================

    [RelayCommand]
    private void BackToMain()
    {
        CurrentPage = new CollectionViewModel(
            GoToDetailsFromChildCommand,
            this);
    }

    // =====================================================
    // MA COLLECTION
    // =====================================================

    [RelayCommand]
    public void ShowMyCollection()
    {
        Session.SelectedUserIds = null;

        CurrentPage = new CollectionViewModel(
            GoToDetailsFromChildCommand,
            this);
    }

    // =====================================================
    // LOAD JSON
    // =====================================================

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
                    // =========================================
                    // IMAGE
                    // =========================================

                    if (!string.IsNullOrWhiteSpace(p.PicturePath))
                    {
                        try
                        {
                            p.PicturePath = ImageHelper.EnsureInAssets(
                                p.PicturePath,
                                p.Id);

                            p.Picture = ImageHelper.LoadFromResource(
                                new Uri(p.PicturePath));
                        }
                        catch
                        {
                            p.Picture = ImageHelper.LoadFromResource(
                                new Uri("avares://OceanStock/Assets/placeholder.png"));
                        }
                    }

                    // =========================================
                    // AJOUT LISTE
                    // =========================================

                    MyGlobals.ProductsFish.Add(p);
                }
            }
            else
            {
                Console.WriteLine("⚠ API vide : aucun poisson trouvé.");

                MyGlobals.ProductsFish.Add(new ProductFish
                {
                    Id = "N/A",
                    Name = "Aucun poisson disponible",
                    Group = "API vide",
                    Stock = 0,
                    Price = 0,
                    Description = "Aucune donnée disponible",
                    PicturePath = "avares://OceanStock/Assets/placeholder.png",
                    Picture = ImageHelper.LoadFromResource(
                        new Uri("avares://OceanStock/Assets/placeholder.png"))
                });
            }
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
                Group = "API DOWN",
                Stock = 0,
                Price = 0
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "Erreur inattendue lors du chargement JSON :\n"
                + ex.Message);
        }
        
        // =========================================
        // SYNC MONGO AU DEMARRAGE
        // =========================================

        var db = new DatabaseServices();

        foreach (var fish in MyGlobals.ProductsFish)
        {
            var existing = await db.GetFishByCustomIdAsync(fish.Id);

            if (existing == null)
            {
                await db.CreateFishAsync(fish);
            }
            else
            {
                await db.UpdateFishAsync(fish);
            }
        }

        Console.WriteLine("✅ Sync Mongo au démarrage OK");
    }

    // =====================================================
    // SAVE JSON
    // =====================================================

    [RelayCommand]
    private async Task SaveJson()
    {
        try
        {
            await _jsonService.SetProductsAsync(
                MyGlobals.ProductsFish.ToList());

            Console.WriteLine("✅ Sauvegarde JSON réussie !");
        }
        catch (HttpRequestException)
        {
            await DialogHelper.ShowError(
                _topLevelWindow,
                "Impossible de contacter le serveur JSON.\nAssurez-vous que Docker tourne.");
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowError(
                _topLevelWindow,
                "Erreur inattendue lors de la sauvegarde JSON :\n"
                + ex.Message);
        }
        var db = new DatabaseServices();
        // ===============================
        // CREATE + UPDATE Mongo
        // ===============================
        foreach (var fish in MyGlobals.ProductsFish)
        {
            var existing = await db.GetFishByCustomIdAsync(fish.Id);

            if (existing == null)
            {
                Console.WriteLine("🟢 CREATE Mongo: " + fish.Name);
                await db.CreateFishAsync(fish);
            }
            else
            {
                
                // ✅ comparer les champs
                bool hasChanged =
                    existing.Name != fish.Name ||
                    existing.Group != fish.Group ||
                    existing.Stock != fish.Stock ||
                    existing.Price != fish.Price ||
                    existing.Description != fish.Description ||
                    existing.PicturePath != fish.PicturePath ||
                    existing.IdOwner != fish.IdOwner;

                if (hasChanged)
                {
                    Console.WriteLine("🟡 UPDATE Mongo: " + fish.Name);
                    await db.UpdateFishAsync(fish);
                }

                //Console.WriteLine("🟡 UPDATE Mongo: " + fish.Name);
               //await db.UpdateFishAsync(fish);
            }
        }

        // =========================================
        // SUPPRIMER DANS MONGO SI PLUS DANS JSON
        // =========================================
        var mongoFish = await db.GetFishAsync();

        foreach (var mongoItem in mongoFish)
        {
            bool existeDansJson = MyGlobals.ProductsFish
                .Any(f => f.Id == mongoItem.Id);

            if (!existeDansJson)
            {
                await db.DeleteFishAsync(mongoItem.Id);
            }
        }
    }

    // =====================================================
    // IMPORT CSV
    // =====================================================

    [RelayCommand]
    private async Task ImportCsv()
    {
        var imported =
            await _csvService.LoadDataAsync<ProductFish>();

        foreach (var fish in imported)
        {
            fish.IdOwner = Session.CurrentUser.Id;
        }

        if (imported.Count == 0)
            return;

        var existing = MyGlobals.ProductsFish;

        CurrentPage = new ImportPreviewViewModel(
            imported,
            existing,
            this);
    }

    // =====================================================
    // EXPORT CSV
    // =====================================================

    [RelayCommand]
    private void ExportCsv()
    {
        List<ProductFish> filteredFish;

        // =========================================
        // ADMIN
        // =========================================

        if (Session.SelectedUserIds != null
            && Session.SelectedUserIds.Any())
        {
            filteredFish = MyGlobals.ProductsFish
                .Where(p =>
                    Session.SelectedUserIds.Contains(p.IdOwner))
                .ToList();
        }

        // =========================================
        // USER NORMAL
        // =========================================

        else
        {
            filteredFish = MyGlobals.ProductsFish
                .Where(p =>
                    p.IdOwner == Session.CurrentUser?.Id)
                .ToList();
        }

        CurrentPage = new ExportPreviewViewModel(
            filteredFish,
            this,
            _csvService);
    }

    // =====================================================
    // DELETE FISH
    // =====================================================

    public async Task DeleteFishAsync(ProductFish fish)
    {
        var result = await DialogHelper.ShowConfirm(
            _topLevelWindow,
            $"Voulez-vous vraiment supprimer {fish.Name} ?");

        if (!result)
            return;

        // =========================================
        // DELETE LOCAL
        // =========================================

        MyGlobals.ProductsFish.Remove(fish);

        // =========================================
        // SAVE JSON
        // =========================================

        //await _jsonService.SetProductsAsync(
        //  MyGlobals.ProductsFish.ToList());
        await SaveJson();

        // =========================================
        // REFRESH PAGE
        // =========================================

        CurrentPage = new CollectionViewModel(
            GoToDetailsFromChildCommand,
            this);
    }

    // =====================================================
    // EDIT FISH
    // =====================================================

    public void GoToEditFish(ProductFish fish)
    {
        CurrentPage = new EditFishViewModel(
            fish,
            this);
    }

    // =====================================================
    // ADD FISH
    // =====================================================

    [RelayCommand]
    public void GoToAddFish()
    {
        CurrentPage = new AddFishViewModel(this);
    }

    // =====================================================
    // REFRESH ADMIN UI
    // =====================================================

    public void RefreshIsAdmin()
    {
        OnPropertyChanged(nameof(IsAdmin));
    }

    // =====================================================
    // PAGE ADMIN
    // =====================================================

    [RelayCommand]
    private void GoToAdmin()
    {
        if (Session.CurrentUser?.IsAdmin != true)
            return;

        CurrentPage = new AdminViewModel(this);
    }

    // =====================================================
    // TEST CREATE USER
    // =====================================================

    private async Task TestCreateUser()
    {
        var db = new DatabaseServices();

        var hashedPassword =
            BCrypt.Net.BCrypt.HashPassword("123");

        var user = new User
        {
            FirstName = "Anas",
            LastName = "Test",
            Email = "anas@test.com",
            PasswordHash = hashedPassword,
            IsAdmin = false
        };

        await db.CreateUserAsync(user);

        Console.WriteLine("✅ User créé !");
    }

    // =====================================================
    // TEST GET USER
    // =====================================================

    private async Task TestGetUser()
    {
        var db = new DatabaseServices();

        var user =
            await db.GetUserByEmailAsync("anas@test.com");

        if (user != null)
            Console.WriteLine("✅ trouvée: " + user.Email);
        else
            Console.WriteLine("❌ user non trouvé");
    }

    // =====================================================
    // TEST LOGIN
    // =====================================================

    private async Task TestLogin()
    {
        var db = new DatabaseServices();

        var user =
            await db.LoginAsync("anas@test.com", "123");

        if (user != null)
        {
            Session.CurrentUser = user;

            Console.WriteLine(
                "✅ LOGIN OK : " + user.Email);
        }
        else
        {
            Console.WriteLine("❌ LOGIN FAIL");
        }

        Console.WriteLine(
            "User connecté : "
            + Session.CurrentUser?.Email);
    }
}