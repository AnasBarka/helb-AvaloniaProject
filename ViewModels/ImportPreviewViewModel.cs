using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OceanStock.Models;

namespace OceanStock.ViewModels;

public partial class ImportPreviewViewModel : ViewModelBase
{
    public ObservableCollection<ImportPreviewItem> PreviewList { get; } = new();

    private readonly MainWindowViewModel _mainVM;

    public int SelectedCount => PreviewList.Count(x => x.IsSelected);
    public bool IsAllSelected => PreviewList.Any() && PreviewList.All(x => x.IsSelected);
    public bool CanImport => SelectedCount > 0;

    public bool IsAllNewSelected
    {
        get
        {
            var items = PreviewList.Where(x => x.Status == ImportStatus.New).ToList();
            return items.Count > 0 && items.All(x => x.IsSelected);
        }
    }

    public bool IsAllModifiedSelected
    {
        get
        {
            var items = PreviewList.Where(x => x.Status == ImportStatus.Modified).ToList();
            return items.Count > 0 && items.All(x => x.IsSelected);
        }
    }

    public ImportPreviewViewModel(
        IEnumerable<ProductFish> imported,
        IEnumerable<ProductFish> existing,
        MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;

        foreach (var fish in imported)
        {
            var existingFish = existing.FirstOrDefault(x =>
                x.Id == fish.Id && x.IdOwner == Session.CurrentUser!.Id);

            var status = GetStatus(fish, existingFish);
            var item = new ImportPreviewItem(fish, status);

            item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ImportPreviewItem.IsSelected))
                {
                    OnPropertyChanged(nameof(SelectedCount));
                    OnPropertyChanged(nameof(IsAllSelected));
                    OnPropertyChanged(nameof(IsAllModifiedSelected));
                    OnPropertyChanged(nameof(IsAllNewSelected));
                    OnPropertyChanged(nameof(CanImport));
                }
            };

            PreviewList.Add(item);
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        bool allSelected = PreviewList.Any() && PreviewList.All(x => x.IsSelected);

        foreach (var item in PreviewList)
            item.IsSelected = !allSelected;

        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(IsAllSelected));
        OnPropertyChanged(nameof(IsAllNewSelected));
        OnPropertyChanged(nameof(IsAllModifiedSelected));
    }

    [RelayCommand]
    private void ToggleNew()
    {
        ToggleByStatus(ImportStatus.New, nameof(IsAllNewSelected));
    }

    [RelayCommand]
    private void ToggleModified()
    {
        ToggleByStatus(ImportStatus.Modified, nameof(IsAllModifiedSelected));
    }

    private void ToggleByStatus(ImportStatus status, string propertyToRefresh)
    {
        var items = PreviewList.Where(x => x.Status == status).ToList();

        if (items.Count == 0)
            return;

        bool allSelected = items.All(x => x.IsSelected);

        foreach (var item in items)
            item.IsSelected = !allSelected;

        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(IsAllSelected));
        OnPropertyChanged(propertyToRefresh);
    }

    private ImportStatus GetStatus(ProductFish imported, ProductFish? existing)
    {
        if (existing == null)
            return ImportStatus.New;

        bool isDifferent =
            imported.Price != existing.Price ||
            imported.Stock != existing.Stock ||
            imported.Group != existing.Group ||
            imported.Description != existing.Description;

        return isDifferent ? ImportStatus.Modified : ImportStatus.Conflict;
    }

    [RelayCommand]
    private void Cancel()
    {
        _mainVM.BackToMainCommand.Execute(null);
    }

    [RelayCommand]
    private async Task ConfirmImport()
    {
        var selectedItems = PreviewList
            .Where(x => x.IsSelected && x.Status != ImportStatus.Conflict)
            .ToList();

        if (selectedItems.Count == 0)
            return;

        foreach (var item in selectedItems)
        {
            var fish = item.Fish;

            var existing = MyGlobals.ProductsFish
                .FirstOrDefault(x => x.Id == fish.Id && x.IdOwner == Session.CurrentUser!.Id);

            if (existing == null)
            {
                fish.IdOwner = Session.CurrentUser!.Id;
                MyGlobals.ProductsFish.Add(fish);
            }
            else
            {
                existing.Price = fish.Price;
                existing.Stock = fish.Stock;
                existing.Group = fish.Group;
                existing.Description = fish.Description;
                existing.Picture = fish.Picture;
                existing.PicturePath = fish.PicturePath;
                existing.IdOwner = Session.CurrentUser!.Id;
            }
        }

        await _mainVM.SaveJsonCommand.ExecuteAsync(null);
        _mainVM.BackToMainCommand.Execute(null);
    }
}
