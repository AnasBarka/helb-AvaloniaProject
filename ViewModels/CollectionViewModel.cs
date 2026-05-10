using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OceanStock.Models;
using OceanStock.Services;

namespace OceanStock.ViewModels;

public partial class CollectionViewModel : ViewModelBase
{
    public IRelayCommand<string> FromParentCommand { get; set; }

    private readonly ObservableCollection<ProductFish> _allFish = new();
    public ObservableCollection<ProductFish> ProductsFish { get; } = new();

    [ObservableProperty] private ProductFish? selectedProduct;
    [ObservableProperty] private string filterText = "";

    private readonly MainWindowViewModel _mainVM;
    private readonly ScannerManager _scanner = new();
    private string _scanBuffer = "";

    public CollectionViewModel(IRelayCommand<string> fromParentCommand, MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;
        FromParentCommand = fromParentCommand;

        _scanner.OpenPort();
        _scanner.SerialBuffer.Changed += OnScannerData;

        foreach (var p in MyGlobals.ProductsFish)
        {
            bool include = Session.SelectedUserIds != null && Session.SelectedUserIds.Any()
                ? Session.SelectedUserIds.Contains(p.IdOwner)
                : p.IdOwner == Session.CurrentUser?.Id;

            if (include)
            {
                _allFish.Add(p);
                ProductsFish.Add(p);
            }
        }
    }

    partial void OnFilterTextChanged(string value)
    {
        ProductsFish.Clear();

        foreach (var p in _allFish)
        {
            if (string.IsNullOrWhiteSpace(value)
                || p.Name.Contains(value, StringComparison.OrdinalIgnoreCase)
                || p.Group.Contains(value, StringComparison.OrdinalIgnoreCase)
                || p.Id.Contains(value, StringComparison.OrdinalIgnoreCase))
            {
                ProductsFish.Add(p);
            }
        }
    }

    private void OnScannerData(object? sender, EventArgs e)
    {
        while (_scanner.SerialBuffer.Count > 0)
            _scanBuffer += _scanner.SerialBuffer.Dequeue()?.ToString() ?? "";

        var idx = _scanBuffer.IndexOfAny(['\r', '\n']);
        if (idx < 0)
            return;

        var scanned = _scanBuffer[..idx].Trim();
        _scanBuffer = "";

        if (string.IsNullOrWhiteSpace(scanned))
            return;

        var fish = _allFish.FirstOrDefault(f => f.Id == scanned);
        if (fish != null)
            Dispatcher.UIThread.Post(() => FromParentCommand.Execute(scanned));
    }

    public override void Dispose()
    {
        _scanner.SerialBuffer.Changed -= OnScannerData;
        _scanner.Dispose();
        base.Dispose();
    }
}
