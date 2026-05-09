using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;
using OceanStock.Helpers;
using OceanStock.Models;
using OceanStock.Services;
using BCrypt.Net;

namespace OceanStock.ViewModels;

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
    
    public TopLevel TopLevel => _topLevelWindow;
    
    // Propriété pour savoir si admin
    public bool IsAdmin => Session.CurrentUser?.Role == "Admin";

    public MainWindowViewModel(TopLevel topLevel)
    {
        _csvService = new CsvServices(topLevel);
        _jsonService = new JSONServices();
        _topLevelWindow = (Window)topLevel;
        
        // ✅ Affiche immédiatement la liste avec la bonne commande
        //CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand); 
        CurrentPage = new LoginViewModel(this);
        // Chargement JSON automatique au démarrage
        _ = LoadDataFromServer();
        
        //_ = TestCreateUser();
        //_ = TestLogin();
        

    }

    partial void OnCurrentPageChanging(ViewModelBase? oldValue, ViewModelBase? newValue)
    {
        oldValue?.Dispose();
    }

    // ✅ Navigation vers détails d’un produit
    [RelayCommand]
    private void GoToDetailsFromChild(string productId)
    {
        CurrentPage = new CollectionDetailsViewModel(productId,this);
    }

    // ✅ Retour à la vue collection
    [RelayCommand]
    private void BackToMain()
    {
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand, this);
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
                            // 🔥 1. Corrige automatiquement le chemin
                            p.PicturePath = ImageHelper.EnsureInAssets(p.PicturePath, p.ID);

                            // 🔥 2. Charge l'image
                            p.Picture = ImageHelper.LoadFromResource(new Uri(p.PicturePath));
                        }
                        catch
                        {
                            // 🔥 fallback propre
                            p.Picture = ImageHelper.LoadFromResource(
                                new Uri("avares://OceanStock/Assets/placeholder.png"));
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
                    PicturePath = "avares://OceanStock/Assets/placeholder.png",
                    Picture = ImageHelper.LoadFromResource(
                        new Uri("avares://OceanStock/Assets/placeholder.png"))
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
            Console.WriteLine("Erreur inattendue lors du chargement JSON :\n" + ex.Message);

        }

        // ✅ Mise à jour de la page
        //CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand);
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

    //  Import CSV
    [RelayCommand]
    private async Task ImportCsv()
    {
        var imported = await _csvService.LoadDataAsync<ProductFish>();

        if (imported.Count == 0)
            return;

        // ✅ Liste EXISTANTE de poissons (source de vérité)
        var existing = MyGlobals.ProductsFish; // ou CollectionView / Products / etc.

        // ✅ Ouvre la page intermédiaire
        CurrentPage = new ImportPreviewViewModel(imported, existing, this);
    }

    // ✅ Export CSV
    [RelayCommand]
    private void ExportCsv()
    {
        CurrentPage = new ExportPreviewViewModel(
            MyGlobals.ProductsFish,
            this,
            _csvService
        );
    }



    public async Task DeleteFishAsync(ProductFish fish)
    {
        // On demande confirmation à l'utilisateur
        // "Êtes-vous sûr de vouloir supprimer X ?"
        var result = await DialogHelper.ShowConfirm(_topLevelWindow,
            $"Voulez-vous vraiment supprimer {fish.Name} ?");

        // Si l'utilisateur clique "Non" → on annule
        if (!result)
            return;

        // On supprime le poisson de la liste globale
        MyGlobals.ProductsFish.Remove(fish);

        // On sauvegarde automatiquement sur le serveur JSON
        await _jsonService.SetProductsAsync(MyGlobals.ProductsFish.ToList());

        
        // Auto-save JSON
        await _jsonService.SetProductsAsync(MyGlobals.ProductsFish.ToList());

        // On revient à la liste des poissons
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
    // Méthode publique pour notifier changement admin
    public void RefreshIsAdmin()
    {
        OnPropertyChanged(nameof(IsAdmin));
    }
    
    [RelayCommand]
    private void GoToAdmin()
    {
        if (Session.CurrentUser?.Role != "Admin")
            return;

        CurrentPage = new AdminViewModel(this);
    }
    
    private async Task TestCreateUser()
    {
        var db = new DatabaseServices();

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("123");

        var user = new User
        {
            FirstName = "Anas",
            LastName = "Test",
            Email = "anas@test.com",
            PasswordHash = hashedPassword,
            Role = "User"
        };

        await db.CreateUserAsync(user);

        Console.WriteLine("✅ User créé !");
    }
    private async Task TestGetUser()
    {
        var db = new DatabaseServices();

        var user = await db.GetUserByEmailAsync("anas@test.com");

        if (user != null)
            Console.WriteLine("✅ trouvée: " + user.Email);
        else
            Console.WriteLine("❌ user non trouvé");
    }
    
    private async Task TestLogin()
    {
        var db = new DatabaseServices();

        var user = await db.LoginAsync("anas@test.com", "123");

        if (user != null)
        {
            Session.CurrentUser = user; // 🔥 IMPORTANT
            Console.WriteLine("✅ LOGIN OK : " + user.Email);
        }
        else
        {
            Console.WriteLine("❌ LOGIN FAIL");
        }
        Console.WriteLine("User connecté : " + Session.CurrentUser?.Email);

    }

}