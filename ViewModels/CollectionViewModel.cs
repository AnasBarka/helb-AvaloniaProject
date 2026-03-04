using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MongoDB.Bson;
using MyProjectBase.Helpers;
using MyProjectBase.Models;

namespace MyProjectBase.ViewModels;

public partial class CollectionViewModel: ViewModelBase
{
    public IRelayCommand<ObjectId> FromParentCommand { get; set; }
    public ObservableCollection<StrangeAnimal> MyObservableStrangeAnimals { get; }
    
    [ObservableProperty] 
    private StrangeAnimal? _selectedAnimal;
    
    public CollectionViewModel(IRelayCommand<ObjectId> fromParentCommand)
    {
        FromParentCommand = fromParentCommand;
        
        MyObservableStrangeAnimals = [];
            
        foreach (var animal in MyGlobals.MyStrangeAnimals)
        {
            MyObservableStrangeAnimals.Add(new StrangeAnimal
            {
                Id = animal.Id,
                Name = animal.Name,
                Description = animal.Description,
                Origin = animal.Origin,
                Picture = animal.Picture
            });
        }
    }
}