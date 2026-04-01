using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyProjectBase.Helpers;
using MyProjectBase.Models;

namespace MyProjectBase.ViewModels;

public partial class CollectionDetailsViewModel : ViewModelBase
{
    // Propriété bindable vers la Vue
    [ObservableProperty] 
    private ProductFish _myFish;

    // On reçoit l'ID (string) du poisson sélectionné
    public CollectionDetailsViewModel(string id)
    {
        // Recherche du produit correspondant dans la liste globale
        // Pas de crach meme si ID pas trouver
        MyFish = MyGlobals.ProductsFish.FirstOrDefault(fish => fish.ID == id)
                 ?? new ProductFish { Name = "Produit introuvable" };    
    }

 
}