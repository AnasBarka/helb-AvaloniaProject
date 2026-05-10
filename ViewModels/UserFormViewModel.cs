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

    [ObservableProperty]
    private string firstName = "";

    [ObservableProperty]
    private string lastName = "";

    [ObservableProperty]
    private string email = "";

    [ObservableProperty]
    private string password = "";

    // ✅ Liste des rôles (SANS tuple)
    public List<RoleOption> Roles { get; } = new()
    {
        new RoleOption { Name = "User", Value = false },
        new RoleOption { Name = "Admin", Value = true }
    };

    // ✅ Role sélectionné
    [ObservableProperty]
    private RoleOption selectedRole;

    public UserFormViewModel(MainWindowViewModel mainVM, User? user)
    {
        _mainVM = mainVM;
        _db = new DatabaseServices();

        if (user != null)
        {
            _isEdit = true;

            _id = user.Id;

            FirstName = user.FirstName;
            LastName = user.LastName;
            Email = user.Email;

            // ✅ sélection correcte
            SelectedRole = Roles.First(r => r.Value == user.IsAdmin);

            _existingPasswordHash = user.PasswordHash;
        }
        else
        {
            // ✅ défaut = User
            SelectedRole = Roles.First(r => r.Value == false);
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (!_isEdit)
        {
            if (string.IsNullOrWhiteSpace(Password))
                return;

            var user = new User
            {
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                IsAdmin = SelectedRole.Value,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password)
            };

            await _db.CreateUserAsync(user);
        }
        else
        {
            string finalPasswordHash;

            if (!string.IsNullOrWhiteSpace(Password))
            {
                finalPasswordHash = BCrypt.Net.BCrypt.HashPassword(Password);
            }
            else
            {
                finalPasswordHash = _existingPasswordHash;
            }

            var user = new User
            {
                Id = _id,
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                IsAdmin = SelectedRole.Value,
                PasswordHash = finalPasswordHash
            };

            await _db.UpdateUserAsync(user);
        }

        _mainVM.CurrentPage = new AdminViewModel(_mainVM);
    }

    [RelayCommand]
    private void Cancel()
    {
        _mainVM.CurrentPage = new AdminViewModel(_mainVM);
    }
}