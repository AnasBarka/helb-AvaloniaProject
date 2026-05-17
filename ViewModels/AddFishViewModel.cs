using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OceanStock.Helpers;
using OceanStock.Models;
using OceanStock.Services;

namespace OceanStock.ViewModels;

public partial class AddFishViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainVM;
    private readonly JSONServices _jsonService;
    private readonly ScannerManager _scanner = new();
    private string _scanBuffer = "";

    [ObservableProperty] private string id = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string group = string.Empty;
    [ObservableProperty] private int stock = 0;
    [ObservableProperty] private int price = 0;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private string? picturePath;
    [ObservableProperty] private IImage? picture;
    [ObservableProperty] private string? errorMessage;

    public string[] Groups { get; } =
    [
        "Eau douce",
        "Eau salée",
        "Eau saumâtre",
        "Tropical",
        "Eau froide",
        "Autre"
    ];

    public AddFishViewModel(MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;
        _jsonService = new JSONServices();

        _scanner.OpenPort();
        _scanner.SerialBuffer.Changed += OnScannerData;
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

        if (!string.IsNullOrWhiteSpace(scanned))
            Dispatcher.UIThread.Post(() => Id = scanned);
    }

    public override void Dispose()
    {
        _scanner.SerialBuffer.Changed -= OnScannerData;
        _scanner.Dispose();
        base.Dispose();
    }

    [RelayCommand]
    private async Task ChooseImage()
    {
        var files = await _mainVM.TopLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Choisir une image",
                AllowMultiple = false,
                FileTypeFilter = [FilePickerFileTypes.ImageAll]
            });

        if (files.Count == 0)
            return;

        var sourcePath = files[0].Path.LocalPath;

        var cleanPath = ImageHelper.EnsureInAssets(sourcePath, Id);
        var finalPath = !string.IsNullOrEmpty(cleanPath) && File.Exists(cleanPath)
            ? cleanPath
            : sourcePath;

        Picture = ImageHelper.LoadFromDisk(finalPath);
        PicturePath = finalPath;
    }

    [RelayCommand]
    private async Task AddFish()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            ErrorMessage = "Le champ ID est obligatoire.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Le champ Nom est obligatoire.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Group))
        {
            ErrorMessage = "Veuillez sélectionner un groupe.";
            return;
        }

        if (Price < 0)
        {
            ErrorMessage = "Le prix ne peut pas etre negatif";
            return;
        }

        if (Stock < 0)
        {
            ErrorMessage = "Le stock ne peut pas etre negatif";
            return;
        }

        if (MyGlobals.ProductsFish.Any(f => f.Id == Id.Trim()))
        {
            ErrorMessage = $"Un article avec l'ID « {Id.Trim()} » existe déjà.";
            return;
        }

        ErrorMessage = null;

        var newFish = new ProductFish
        {
            Id = Id.Trim(),
            Name = Name.Trim(),
            Group = Group.Trim(),
            Stock = Stock,
            Price = Price,
            Description = Description.Trim(),
            PicturePath = PicturePath,
            Picture = Picture,
            IdOwner = Session.CurrentUser!.Id
        };

        MyGlobals.ProductsFish.Add(newFish);
        await _mainVM.SaveJsonCommand.ExecuteAsync(null);
        _mainVM.BackToMainCommand.Execute(null);
    }

    [RelayCommand]
    private void Cancel()
    {
        _mainVM.BackToMainCommand.Execute(null);
    }

    [RelayCommand]
    private void SimulateScan()
    {
        _scanner.SimulateScan("TEST-" + new Random().Next(100, 999));
    }
}
