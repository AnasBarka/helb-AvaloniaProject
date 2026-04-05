using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
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

    // ── Champs du formulaire ────────────────────────────────────────────────

    [ObservableProperty] private string id          = string.Empty;
    [ObservableProperty] private string name        = string.Empty;
    [ObservableProperty] private string group       = string.Empty;
    [ObservableProperty] private int    stock       = 0;
    [ObservableProperty] private int    price       = 0;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private string? picturePath;
    [ObservableProperty] private IImage? picture;

    // Message d'erreur de validation affiché dans la vue
    [ObservableProperty] private string? errorMessage;

    // Groupes prédéfinis pour le ComboBox
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
        _mainVM      = mainVM;
        _jsonService = new JSONServices();
    }

    // ── Commande : choisir une image ────────────────────────────────────────

    [RelayCommand]
    private async Task ChooseImage()
    {
        var files = await _mainVM.TopLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title         = "Choisir une image",
                AllowMultiple = false,
                FileTypeFilter = [FilePickerFileTypes.ImageAll]
            });

        if (files.Count == 0)
            return;

        var file = files[0];
        var sourcePath = file.Path.AbsolutePath;

        try
        {
            // Copier le fichier dans Assets/ et obtenir le chemin avares://
            var cleanPath = ImageHelper.EnsureInAssets(sourcePath, Id);

            Picture     = ImageHelper.LoadFromResource(new Uri(cleanPath));
            PicturePath = cleanPath;
        }
        catch
        {
            // Chargement direct depuis le disque en fallback
            Picture     = new Bitmap(sourcePath);
            PicturePath = sourcePath;
        }
    }

    // ── Commande : ajouter le poisson ───────────────────────────────────────

    [RelayCommand]
    private async Task AddFish()
    {
        // ── Validation ──────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(Id))
        {
            ErrorMessage = "⚠ Le champ ID est obligatoire.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "⚠ Le champ Nom est obligatoire.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Group))
        {
            ErrorMessage = "⚠ Veuillez sélectionner un groupe.";
            return;
        }

        // Vérifier que l'ID n'est pas déjà utilisé
        if (MyGlobals.ProductsFish.Any(f => f.ID == Id.Trim()))
        {
            ErrorMessage = $"⚠ Un poisson avec l'ID « {Id.Trim()} » existe déjà.";
            return;
        }

        ErrorMessage = null;

        // ── Création de l'objet ─────────────────────────────────────────────
        var newFish = new ProductFish
        {
            ID          = Id.Trim(),
            Name        = Name.Trim(),
            Group       = Group.Trim(),
            Stock       = Stock,
            Price       = Price,
            Description = Description.Trim(),
            PicturePath = PicturePath,
            Picture     = Picture
        };

        // ── Ajout dans la liste globale ─────────────────────────────────────
        MyGlobals.ProductsFish.Add(newFish);

        // ── Sauvegarde automatique sur le serveur JSON ──────────────────────
        await _mainVM.SaveJsonCommand.ExecuteAsync(null);

        // ── Retour à la collection ──────────────────────────────────────────
        _mainVM.BackToMainCommand.Execute(null);
    }

    // ── Commande : annuler ──────────────────────────────────────────────────

    [RelayCommand]
    private void Cancel()
    {
        _mainVM.BackToMainCommand.Execute(null);
    }
}
