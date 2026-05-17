using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OceanStock.Models;
using OceanStock.Services;

namespace OceanStock.ViewModels;

// Représente une colonne sélectionnable
public partial class ColumnItem : ObservableObject
{
    public string Name { get; set; } = "";          // texte affiché à l'écran
    public string PropertyName { get; set; } = "";  // nom réel de la propriété C#
    [ObservableProperty] private bool isSelected = true; // cochée par défaut
}

public partial class ExportPreviewViewModel : ViewModelBase
{
    public ObservableCollection<ExportPreviewItem> PreviewList { get; } = new();
    public ObservableCollection<ColumnItem> Columns { get; } = new();

    private readonly MainWindowViewModel _mainVM;
    private readonly CsvServices _csvService;

    public int SelectedCount => PreviewList.Count(x => x.IsSelected);
    public int SelectedColumnCount => Columns.Count(x => x.IsSelected);
    public bool IsAllSelected => PreviewList.Any() && PreviewList.All(x => x.IsSelected);
    public bool CanExport => SelectedCount > 0 && SelectedColumnCount > 0;

    public ExportPreviewViewModel(IEnumerable<ProductFish> data, MainWindowViewModel mainVM, CsvServices csvService)
    {
        _mainVM = mainVM;
        _csvService = csvService;

        // Colonnes disponibles
        var availableColumns = new List<ColumnItem>
        {
            new() { Name = "ID",          PropertyName = "Id" },
            new() { Name = "Nom",         PropertyName = "Name" },
            new() { Name = "Groupe",      PropertyName = "Group" },
            new() { Name = "Stock",       PropertyName = "Stock" },
            new() { Name = "Prix",        PropertyName = "Price" },
            new() { Name = "Description", PropertyName = "Description" },
        };

        foreach (var col in availableColumns)
        {
            col.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ColumnItem.IsSelected))
                {
                    OnPropertyChanged(nameof(SelectedColumnCount));
                    OnPropertyChanged(nameof(CanExport));
                }
            };
            Columns.Add(col);
        }

        // Lignes (poissons)
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

    [RelayCommand]
    private void SelectAll()
    {
        bool allSelected = IsAllSelected;
        foreach (var item in PreviewList)
            item.IsSelected = !allSelected;

        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(IsAllSelected));
        OnPropertyChanged(nameof(CanExport));
    }

    [RelayCommand]
    private void Cancel()
    {
        _mainVM.BackToMainCommand.Execute(null);
    }

    [RelayCommand]
    private async Task ConfirmExport()
    {
        var selectedFish = PreviewList
            .Where(x => x.IsSelected)
            .Select(x => x.Fish)
            .ToList();

        var selectedColumns = Columns
            .Where(x => x.IsSelected)
            .Select(x => x.PropertyName)
            .ToList();

        if (selectedFish.Count == 0 || selectedColumns.Count == 0)
            return;

        await _csvService.SaveDataAsync(selectedFish, selectedColumns);
        _mainVM.BackToMainCommand.Execute(null);
    }
}