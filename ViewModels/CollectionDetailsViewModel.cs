using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OceanStock.Helpers;
using OceanStock.Models;

namespace OceanStock.ViewModels;

public partial class CollectionDetailsViewModel : ViewModelBase
{
    // Propriété bindable vers la Vue
    [ObservableProperty] 
    private ProductFish _myFish;
    private readonly MainWindowViewModel _mainVM;

    
    // On reçoit l'ID (string) du poisson sélectionné
    public CollectionDetailsViewModel(string id,MainWindowViewModel mainVM)
    {
        
        _mainVM = mainVM;

        // Recherche du produit correspondant dans la liste globale
        // Pas de crach meme si ID pas trouver
        MyFish = MyGlobals.ProductsFish.FirstOrDefault(fish => fish.ID == id)
                 ?? new ProductFish { Name = "Produit introuvable" };    
    }
    
  
    // Commande du bouton "Supprimer"
    [RelayCommand]
    private async Task Delete()
    {
        //  On appelle la méthode de MainWindowViewModel
        // _mainVM = parent (MainWindowViewModel)
        // MyFish = poisson actuel sélectionné
        await _mainVM.DeleteFishAsync(MyFish);

        //  Rien d’autre à gérer ici
        // Le parent gère tout : suppression, JSON, navigation
    }


    // ✅ MODIFIER
    [RelayCommand]
    private void Edit()
    {
        _mainVM.GoToEditFish(MyFish);
    }


 
}