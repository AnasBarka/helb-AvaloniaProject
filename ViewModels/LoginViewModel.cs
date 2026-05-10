using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OceanStock.Services;

namespace OceanStock.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainVM;
    private readonly DatabaseServices _db;

    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string? errorMessage;

    public LoginViewModel(MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;
        _db = new DatabaseServices();
    }

    [RelayCommand]
    private async Task Login()
    {
        ErrorMessage = null;

        var user = await _db.LoginAsync(Email, Password);

        if (user != null)
        {
            Session.CurrentUser = user;
            _mainVM.RefreshIsAdmin();
            _mainVM.BackToMainCommand.Execute(null);
        }
        else
        {
            ErrorMessage = "Email ou mot de passe incorrect.";
        }
    }

    [RelayCommand]
    private void GoToRegister()
    {
        _mainVM.CurrentPage = new RegisterViewModel(_mainVM);
    }
}
