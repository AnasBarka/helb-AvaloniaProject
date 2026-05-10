using System.IO;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OceanStock.Helpers;
using OceanStock.Models;

namespace OceanStock.ViewModels;

public partial class EditFishViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainVM;
    private readonly ProductFish _originalFish;

    [ObservableProperty] private string id;
    [ObservableProperty] private string name;
    [ObservableProperty] private string group;
    [ObservableProperty] private int stock;
    [ObservableProperty] private int price;
    [ObservableProperty] private string description;
    [ObservableProperty] private string? picturePath;
    [ObservableProperty] private IImage? picture;

    public EditFishViewModel(ProductFish fish, MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;
        _originalFish = fish;

        Id = fish.Id;
        Name = fish.Name;
        Group = fish.Group;
        Stock = fish.Stock;
        Price = fish.Price;
        Description = fish.Description;
        PicturePath = fish.PicturePath;
        Picture = fish.Picture;
    }

    [RelayCommand]
    private async Task ChangeImage()
    {
        var files = await _mainVM.TopLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Choisir une image",
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
            });

        if (files.Count == 0)
            return;

        var sourcePath = files[0].Path.LocalPath;

        var cleanPath = ImageHelper.EnsureInAssets(sourcePath, Id);
        var finalPath = !string.IsNullOrEmpty(cleanPath) && File.Exists(cleanPath)
            ? cleanPath
            : sourcePath;

        Picture = ImageHelper.LoadFromDisk(finalPath) ?? Picture;
        PicturePath = finalPath;

        _originalFish.Picture = Picture;
        _originalFish.PicturePath = PicturePath;
    }

    [RelayCommand]
    private void Cancel()
    {
        _mainVM.GoToDetailsFromChildCommand.Execute(Id);
    }

    [RelayCommand]
    private async Task Save()
    {
        _originalFish.Name = Name;
        _originalFish.Group = Group;
        _originalFish.Stock = Stock;
        _originalFish.Price = Price;
        _originalFish.Description = Description;
        _originalFish.PicturePath = PicturePath;

        await _mainVM.SaveJsonCommand.ExecuteAsync(null);
        _mainVM.GoToDetailsFromChildCommand.Execute(Id);
    }
}
