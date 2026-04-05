using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyProjectBase.Models;
using System.Threading.Tasks;

namespace MyProjectBase.ViewModels;

public partial class EditFishViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainVM;
    private readonly ProductFish _originalFish;

    // Propriétés bindées à la vue
    [ObservableProperty] private string id;
    [ObservableProperty] private string name;
    [ObservableProperty] private string group;
    [ObservableProperty] private int stock;
    [ObservableProperty] private int price;
    [ObservableProperty] private string description;

    public EditFishViewModel(ProductFish fish, MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;
        _originalFish = fish;

        // Pré-remplissage des champs
        Id = fish.ID;
        Name = fish.Name;
        Group = fish.Group;
        Stock = fish.Stock;
        Price = fish.Price;
        Description = fish.Description;
    }

    // ✅ Annuler → retour à la page précédente
    [RelayCommand]
    private void Cancel()
    {
        _mainVM.GoToDetailsFromChildCommand.Execute(Id);
    }

    // ✅ Sauvegarder → mettre à jour le poisson + sauvegarder JSON
    [RelayCommand]
    private async Task Save()
    {
        // ✅ Modifier l'objet original
        _originalFish.Name = Name;
        _originalFish.Group = Group;
        _originalFish.Stock = Stock;
        _originalFish.Price = Price;
        _originalFish.Description = Description;

        // ✅ Sauvegarde JSON
        await _mainVM.SaveJsonCommand.ExecuteAsync(null);

        // ✅ Retour à la page détail
        _mainVM.GoToDetailsFromChildCommand.Execute(Id);
    }
}