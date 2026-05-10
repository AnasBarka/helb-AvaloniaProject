using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OceanStock.Services;
using OceanStock.Models;

namespace OceanStock.ViewModels;

public partial class RegisterViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainVM;
    private readonly DatabaseServices _db;

    [ObservableProperty] private string firstName = "";
    [ObservableProperty] private string lastName = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";

    public RegisterViewModel(MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;
        _db = new DatabaseServices();
    }

    [RelayCommand]
    private async Task Register()
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(Password);

        var user = new User
        {
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            PasswordHash = hashedPassword,
            IsAdmin = false
        };

        await _db.CreateUserAsync(user);

        // ✅ retour login après création
        _mainVM.CurrentPage = new LoginViewModel(_mainVM);
    }

    [RelayCommand]
    private void Back()
    {
        _mainVM.CurrentPage = new LoginViewModel(_mainVM);
    }
}