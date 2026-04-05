using CommunityToolkit.Mvvm.ComponentModel;
using MyProjectBase.Models;

namespace MyProjectBase.ViewModels;

/// <summary>
/// Représente UNE ligne dans la preview d’export
/// </summary>
public partial class ExportPreviewItem : ObservableObject
{
    /// <summary>
    /// Le modèle réel
    /// </summary>
    public ProductFish Fish { get; }

    /// <summary>
    /// Checkbox sélection
    /// </summary>
    [ObservableProperty]
    private bool isSelected;

    public ExportPreviewItem(ProductFish fish)
    {
        Fish = fish;
        IsSelected = true; // ✅ par défaut sélectionné (logique pour export)
    }
}