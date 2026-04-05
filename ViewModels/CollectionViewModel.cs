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

    public CollectionViewModel(IRelayCommand<string> fromParentCommand)
    {
        FromParentCommand = fromParentCommand;

        ProductsFish = new ObservableCollection<ProductFish>();

        // Charger tous les produits depuis MyGlobals
        foreach (var p in MyGlobals.ProductsFish)
        {
            ProductsFish.Add(p);
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
    }
}