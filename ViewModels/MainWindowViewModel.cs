using System;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MongoDB.Bson;
using Avalonia.Controls;
using MyProjectBase.Helpers;
using MyProjectBase.Models;
using MyProjectBase.Services;

namespace MyProjectBase.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // Page actuellement affichée dans l’UI
    [ObservableProperty] private ViewModelBase _currentPage;

    // Service pour gérer CSV (import/export)
    private readonly CsvServices _csvService;

    // Service pour gérer JSON via serveur (API)
    private readonly JSONServices _jsonService;
    
    private readonly Window _topLevelWindow;


    public MainWindowViewModel(TopLevel topLevel)
    {
        // Initialisation des services
        _csvService = new CsvServices(topLevel);
        _jsonService = new JSONServices();
        _topLevelWindow = (Window)topLevel;


        // Chargement des données depuis le serveur au démarrage
        _ = LoadDataFromServer();
    }

    // Nettoyage de l’ancienne page (bonne pratique MVVM)
    partial void OnCurrentPageChanging(ViewModelBase? oldValue, ViewModelBase? newValue)
    {
        oldValue?.Dispose();
    }

    // Navigation vers page détail
    [RelayCommand]
    private void GoToDetailsFromChild(ObjectId animalId)
    {
        CurrentPage = new CollectionDetailsViewModel(animalId);
    }

    // Retour à la page principale
    [RelayCommand]
    private void BackToMain()
    {
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand);
    }

    // 🔥 Chargement des données depuis le serveur JSON
    /// <summary>
    /// Charge les données depuis le serveur JSON (API locale Docker).
    /// Si aucune donnée n'est trouvée, on affiche un message dans la console
    /// et on ajoute un élément "placeholder" dans la liste.
    /// </summary>
    private async Task LoadDataFromServer()
    {
        // ✅ 1. On tente de récupérer la liste depuis l'API JSON
        var data = await _jsonService.GetStrangeAnimalsAsync();

        // ✅ 2. On vide la collection globale
        MyGlobals.MyStrangeAnimals.Clear();

        // ✅ 3. Si le serveur renvoie des données → on les ajoute normalement
        if (data.Count > 0)
        {
            foreach (var item in data)
                MyGlobals.MyStrangeAnimals.Add(item);
        }
        else
        {
            // ✅ 4. Si l'API est vide → message d’avertissement dans la console
            Console.WriteLine("⚠ Aucune donnée trouvée sur le serveur JSON (API vide).");

            // ✅ 5. Ajouter un STRANGE ANIMAL FACTICE pour ne pas laisser l’UI vide
            MyGlobals.MyStrangeAnimals.Add(new StrangeAnimal
            {
                Id = ObjectId.GenerateNewId(),
                Name = "Aucune donnée disponible",
                Description = "Le serveur JSON n’a retourné aucun élément.",
                Origin = "Serveur vide",
                Picture = ImageHelper.LoadFromResource(
                    new Uri("avares://MyProjectBase/Assets/python.png"))
            });
        }

        // ✅ 6. On met à jour la page affichée dans l’UI (ListView)
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand);
    }

    // 🔥 Sauvegarde des données vers le serveur JSON
    [RelayCommand]
    private async Task SaveDataToServer()
    {
        try
        {
            await _jsonService.SetStrangeAnimalsAsync(MyGlobals.MyStrangeAnimals);

            Console.WriteLine("✅ Sauvegarde JSON réussie !");
        }
        catch (HttpRequestException)
        {
            await DialogHelper.ShowError(_topLevelWindow, 
                "Impossible de contacter le serveur JSON.\n\nVérifiez que Docker tourne.");
        }
        catch (Exception ex)
        {
            await DialogHelper.ShowError(_topLevelWindow, 
                "Erreur inattendue lors de la sauvegarde JSON :\n" + ex.Message);
        }
    }

    // 🔥 Commande appelée par le bouton "Save JSON"
    [RelayCommand]
    private async Task SaveJson()
    {
        await SaveDataToServer();
    }

    // 📥 Import CSV
    [RelayCommand]
    private async Task ImportCsv()
    {
        var data = await _csvService.LoadDataAsync();

        if (data.Count == 0)
            return;

        MyGlobals.MyStrangeAnimals.Clear();

        foreach (var item in data)
            MyGlobals.MyStrangeAnimals.Add(item);

        // Refresh UI
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand);
    }

    // 📤 Export CSV
    [RelayCommand]
    private async Task ExportCsv()
    {
        await _csvService.SaveDataAsync(MyGlobals.MyStrangeAnimals);
    }
}