using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OceanStock.Helpers;
using OceanStock.Models;

namespace OceanStock.ViewModels;

public partial class CollectionViewModel : ViewModelBase
{
    // Command venant du parent (MainWindowViewModel)
    public IRelayCommand<string> FromParentCommand { get; set; }

    // Collection observable de Products (poissons)
    public ObservableCollection<ProductFish> ProductsFish { get; }

    // Élément sélectionné dans la liste
    [ObservableProperty] 
    private ProductFish? _selectedProduct;
    
    private readonly MainWindowViewModel _mainVM;


    public CollectionViewModel(IRelayCommand<string> fromParentCommand, MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;
        
        FromParentCommand = fromParentCommand;

        ProductsFish = new ObservableCollection<ProductFish>();

        // Charger tous les produits depuis MyGlobals
        foreach (var p in MyGlobals.ProductsFish)
        {
            
            // ✅ CAS ADMIN sélection
            if (Session.SelectedUserIds != null && Session.SelectedUserIds.Any())
            {
                if (Session.SelectedUserIds.Contains(p.IdOwner))
                {
                    ProductsFish.Add(p);
                }
                /* Equivalent de;
                  * ProductsFish.Add(new ProductFish
                    {
                        ID = p.ID,
                        Name = p.Name,
                        Group = p.Group,
                        Stock = p.Stock,
                        Price = p.Price
                    });
                  */
            }
            else
            {
                // ✅ comportement normal
                if (p.IdOwner == Session.CurrentUser?.Id)
                {
                    ProductsFish.Add(p);
                }
            }
        }
    }
}