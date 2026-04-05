using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OceanStock.Models;
using OceanStock.Services;

namespace OceanStock.ViewModels;

public partial class ExportPreviewViewModel : ViewModelBase
{
    public ObservableCollection<ExportPreviewItem> PreviewList { get; } = new();

    private readonly MainWindowViewModel _mainVM;
    private readonly CsvServices _csvService;

    public int SelectedCount => PreviewList.Count(x => x.IsSelected);

    public bool IsAllSelected =>
        PreviewList.Any() && PreviewList.All(x => x.IsSelected);

    public bool CanExport => SelectedCount > 0;

    public ExportPreviewViewModel(
        IEnumerable<ProductFish> data,
        MainWindowViewModel mainVM,
        CsvServices csvService)
    {
        _mainVM = mainVM;
        _csvService = csvService;

        foreach (var fish in data)
        {
            var item = new ExportPreviewItem(fish);

            item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ExportPreviewItem.IsSelected))
                {
                    OnPropertyChanged(nameof(SelectedCount));
                    OnPropertyChanged(nameof(IsAllSelected));
                    OnPropertyChanged(nameof(CanExport));
                }
            };

            PreviewList.Add(item);
        }
    }

    // ✅ Sélection globale
    [RelayCommand]
    private void SelectAll()
    {
        bool allSelected = IsAllSelected;

        foreach (var item in PreviewList)
        {
            item.IsSelected = !allSelected;
        }

        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(IsAllSelected));
        OnPropertyChanged(nameof(CanExport));
    }

    // ❌ Annuler
    [RelayCommand]
    private void Cancel()
    {
        _mainVM.BackToMainCommand.Execute(null);
    }

    // ✅ Export
    [RelayCommand]
    private async Task ConfirmExport()
    {
        var selected = PreviewList
            .Where(x => x.IsSelected)
            .Select(x => x.Fish)
            .ToList();

        if (selected.Count == 0)
            return;

        await _csvService.SaveDataAsync(selected);

        _mainVM.BackToMainCommand.Execute(null);
    }
}