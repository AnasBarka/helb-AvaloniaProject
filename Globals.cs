using System.Collections.ObjectModel;
using OceanStock.Models;

namespace OceanStock;

public static class MyGlobals
{
    public static ObservableCollection<ProductFish> ProductsFish { get;} = new();
}