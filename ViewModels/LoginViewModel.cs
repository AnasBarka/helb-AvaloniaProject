using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OceanStock.Services;

namespace OceanStock.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainVM;
    private readonly DatabaseServices _db;

    [ObservableProperty]
    private string email = "";

    [ObservableProperty]
    private string password = "";

    public LoginViewModel(MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;
        _db = new DatabaseServices();
    }

    [RelayCommand]
    private async Task Login()
    {
        var user = await _db.LoginAsync(Email, Password);

        if (user != null)
        {
            Session.CurrentUser = user;
            _mainVM.RefreshIsAdmin();           
            // Naviguer vers la collection
            _mainVM.BackToMainCommand.Execute(null);
        }
        else
        {
            // 👉 tu peux améliorer après
            System.Console.WriteLine("❌ Login failed");
        }
    }
    
    [RelayCommand]
    private void GoToRegister()
    {
        _mainVM.CurrentPage = new RegisterViewModel(_mainVM);
    }
}