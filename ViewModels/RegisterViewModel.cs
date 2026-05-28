using System;
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
    [ObservableProperty] private string? errorMessage;

    public RegisterViewModel(MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;
        _db = DatabaseServices.Instance;
    }

    [RelayCommand]
    private async Task Register()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName)
            || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Tous les champs sont obligatoires.";
            return;
        }

        if (Password.Length < 6)
        {
            ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères.";
            return;
        }

        try
        {
            var existing = await _db.GetUserByEmailAsync(Email.Trim());
            if (existing != null)
            {
                ErrorMessage = "Un compte avec cet email existe déjà.";
                return;
            }

            var user = new User
            {
                FirstName = FirstName.Trim(),
                LastName  = LastName.Trim(),
                Email     = Email.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                IsAdmin = false
            };

            await _db.CreateUserAsync(user);
            _mainVM.CurrentPage = new LoginViewModel(_mainVM);
        }
        catch (Exception)
        {
            ErrorMessage = "Impossible de contacter la base de données. Vérifiez votre connexion.";
        }
    }

    [RelayCommand]
    private void Back()
    {
        _mainVM.CurrentPage = new LoginViewModel(_mainVM);
    }
}
