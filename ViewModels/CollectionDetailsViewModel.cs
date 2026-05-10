using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OceanStock.Models;

namespace OceanStock.ViewModels;

public partial class CollectionDetailsViewModel : ViewModelBase
{
    [ObservableProperty]
    private ProductFish _myFish;
    private readonly MainWindowViewModel _mainVM;

    public CollectionDetailsViewModel(string id, MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;
        MyFish = MyGlobals.ProductsFish.FirstOrDefault(fish => fish.Id == id)
                 ?? new ProductFish { Name = "Produit introuvable" };
    }

    [RelayCommand]
    private async Task Delete()
    {
        await _mainVM.DeleteFishAsync(MyFish);
    }

    [RelayCommand]
    private void Edit()
    {
        _mainVM.GoToEditFish(MyFish);
    }
}
