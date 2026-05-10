using CommunityToolkit.Mvvm.ComponentModel;
using OceanStock.Models;

namespace OceanStock.ViewModels;

public partial class ExportPreviewItem : ObservableObject
{
    public ProductFish Fish { get; }

    [ObservableProperty]
    private bool isSelected;

    public ExportPreviewItem(ProductFish fish)
    {
        Fish = fish;
        IsSelected = true;
    }
}
