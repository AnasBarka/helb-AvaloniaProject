using System;
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
    [ObservableProperty] private ViewModelBase _currentPage;
    private readonly CsvServices _csvService;

    public MainWindowViewModel(TopLevel topLevel)
    {
        _csvService = new CsvServices(topLevel);
        for (var i = 0; i < 5; i++)
        {
            MyGlobals.MyStrangeAnimals.Add(new StrangeAnimal
            {
                Id = ObjectId.GenerateNewId(),
                Name = "Python molure",
                Description = "Python",
                Origin = "Amrérique du sud",
                Picture = ImageHelper.LoadFromResource(new Uri("avares://MyProjectBase/Assets/python.png"))
            });
            
            MyGlobals.MyStrangeAnimals.Add(new StrangeAnimal
            {
                Id = ObjectId.GenerateNewId(),
                Name = "Morelia viridis",
                Description = "Python",
                Origin = "Australien",
                Picture = ImageHelper.LoadFromResource(new Uri("avares://MyProjectBase/Assets/morelia.png"))
            });
        }
        
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand);
    }
    partial void OnCurrentPageChanging(ViewModelBase? oldValue, ViewModelBase? newValue)
    {
        oldValue?.Dispose();
    }
    
    [RelayCommand]
    private void GoToDetailsFromChild(ObjectId animalId)
    {
        CurrentPage = new CollectionDetailsViewModel(animalId);
    }
    
    [RelayCommand]
    private void BackToMain()
    {
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand);
    }
    
    // IMPORT CSV
    [RelayCommand]
    private async Task ImportCsv()
    {
        var data = await _csvService.LoadDataAsync();

        if (data.Count == 0)
            return;

        MyGlobals.MyStrangeAnimals.Clear();

        foreach (var item in data)
            MyGlobals.MyStrangeAnimals.Add(item);

        // 🔥 Refresh UI
        CurrentPage = new CollectionViewModel(GoToDetailsFromChildCommand);
    }

    // EXPORT CSV
    [RelayCommand]
    private async Task ExportCsv()
    {
        await _csvService.SaveDataAsync(MyGlobals.MyStrangeAnimals);
    }
}

