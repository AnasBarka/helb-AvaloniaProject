using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Media;
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
    [ObservableProperty]
    private IImage? picture;

    public EditFishViewModel(ProductFish fish, MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;
        _originalFish = fish;

        Id = fish.ID;
        Name = fish.Name;
        Group = fish.Group;
        Stock = fish.Stock;
        Price = fish.Price;
        Description = fish.Description;
        PicturePath = fish.PicturePath;
        Picture = fish.Picture;
    }

    // ✅ CLIQUE SUR L’IMAGE
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

        var file = files[0];
        var path = file.Path.AbsolutePath;

        try
        {
            // ✅ Charger l’image depuis le fichier local
            var bitmap = new Avalonia.Media.Imaging.Bitmap(path);

            // ✅ Mettre à jour le ViewModel
            Picture = bitmap;
            var cleanPath = ImageHelper.EnsureInAssets(path, Id);

            Picture = ImageHelper.LoadFromResource(new Uri(cleanPath));
            PicturePath = cleanPath;

            _originalFish.Picture = Picture;
            _originalFish.PicturePath = cleanPath;
            // ✅ Mettre à jour le modèle
            _originalFish.Picture = bitmap;
            _originalFish.PicturePath = path;
        }
        catch
        {
            // ignore image invalide
        }
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