
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MongoDB.Bson;
using MyProjectBase.Helpers;
using MyProjectBase.Models;
    
namespace MyProjectBase.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _currentPage;

    public MainWindowViewModel()
    {
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
}

