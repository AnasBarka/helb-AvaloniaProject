using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OceanStock.Models;
using OceanStock.Services;

namespace OceanStock.ViewModels;

public partial class UserFormViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainVM;
    private readonly DatabaseServices _db;

    private readonly bool _isEdit;
    private string _id = "";
    private string _existingPasswordHash = "";

    [ObservableProperty] private string firstName = "";
    [ObservableProperty] private string lastName = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private RoleOption selectedRole;

    public List<RoleOption> Roles { get; } = new()
    {
        new RoleOption { Name = "User", Value = false },
        new RoleOption { Name = "Admin", Value = true }
    };

    public UserFormViewModel(MainWindowViewModel mainVM, User? user)
    {
        _mainVM = mainVM;
        _db = DatabaseServices.Instance;

        if (user != null)
        {
            _isEdit = true;
            _id = user.Id;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Email = user.Email;
            SelectedRole = Roles.First(r => r.Value == user.IsAdmin);
            _existingPasswordHash = user.PasswordHash;
        }
        else
        {
            SelectedRole = Roles.First(r => r.Value == false);
        }
    }

    [ObservableProperty] private string? errorMessage;

    [RelayCommand]
    private async Task Save()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Prénom, nom et email sont obligatoires.";
            return;
        }

        if (!_isEdit && string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Le mot de passe est obligatoire pour un nouvel utilisateur.";
            return;
        }

        try
        {
            if (!_isEdit)
            {
                var user = new User
                {
                    FirstName    = FirstName.Trim(),
                    LastName     = LastName.Trim(),
                    Email        = Email.Trim(),
                    IsAdmin      = SelectedRole.Value,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password)
                };
                await _db.CreateUserAsync(user);
            }
            else
            {
                string finalHash = string.IsNullOrWhiteSpace(Password)
                    ? _existingPasswordHash
                    : BCrypt.Net.BCrypt.HashPassword(Password);

                var user = new User
                {
                    Id           = _id,
                    FirstName    = FirstName.Trim(),
                    LastName     = LastName.Trim(),
                    Email        = Email.Trim(),
                    IsAdmin      = SelectedRole.Value,
                    PasswordHash = finalHash
                };
                await _db.UpdateUserAsync(user);
            }

            _mainVM.CurrentPage = new AdminViewModel(_mainVM);
        }
        catch (Exception)
        {
            ErrorMessage = "Erreur lors de la sauvegarde. Vérifiez votre connexion.";
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _mainVM.CurrentPage = new AdminViewModel(_mainVM);
    }
}
