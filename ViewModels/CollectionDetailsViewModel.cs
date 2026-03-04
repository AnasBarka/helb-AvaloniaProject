using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MongoDB.Bson;
using MyProjectBase.Helpers;
using MyProjectBase.Models;

namespace MyProjectBase.ViewModels;

public partial class CollectionDetailsViewModel:ViewModelBase
{
    [ObservableProperty] private StrangeAnimal _myAnimal;
    
    public CollectionDetailsViewModel(ObjectId id)
    {
        MyAnimal = MyGlobals.MyStrangeAnimals.First(animal => animal.Id == id);
    }
}