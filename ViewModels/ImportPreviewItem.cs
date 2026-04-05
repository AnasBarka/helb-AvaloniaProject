using CommunityToolkit.Mvvm.ComponentModel;
using OceanStock.Models;

namespace OceanStock.ViewModels;


public enum ImportStatus
{
    New,
    Modified,
    Conflict
}

/// <summary>
/// Représente UNE ligne dans la preview d'import
/// (wrapper autour de ProductFish)
/// </summary>
public partial class ImportPreviewItem : ObservableObject
{
    /// <summary>
    /// Le vrai modèle métier (poisson)
    /// </summary>
    public ProductFish Fish { get; }
    
    public ImportStatus Status { get; }


    /// <summary>
    /// État de sélection (Checkbox)
    /// </summary>
    [ObservableProperty]
    private bool isSelected;
    
    public bool IsNew => Status == ImportStatus.New;
    public bool IsModified => Status == ImportStatus.Modified;
    public bool IsConflict => Status == ImportStatus.Conflict;
    public bool IsSelectable => Status != ImportStatus.Conflict;

    public ImportPreviewItem(ProductFish fish,  ImportStatus status)
    {
        Fish = fish;
        IsSelected = false; // par défaut non sélectionné
        Status = status;

    }
}