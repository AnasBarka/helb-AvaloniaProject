using CommunityToolkit.Mvvm.ComponentModel;
using OceanStock.Models;

namespace OceanStock.ViewModels;

public enum ImportStatus
{
    New,
    Modified,
    Conflict
}

public partial class ImportPreviewItem : ObservableObject
{
    public ProductFish Fish { get; }
    public ImportStatus Status { get; }

    [ObservableProperty]
    private bool isSelected;

    public bool IsNew => Status == ImportStatus.New;
    public bool IsModified => Status == ImportStatus.Modified;
    public bool IsConflict => Status == ImportStatus.Conflict;
    public bool IsSelectable => Status != ImportStatus.Conflict;

    public ImportPreviewItem(ProductFish fish, ImportStatus status)
    {
        Fish = fish;
        Status = status;
        IsSelected = false;
    }
}
