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
    private string role = "User";

    [ObservableProperty]
    private string password = "";
    


    public UserFormViewModel(MainWindowViewModel mainVM, User? user)
    {
        _mainVM = mainVM;

        _db = new DatabaseServices();

        // UPDATE MODE
        if (user != null)
        {
            _isEdit = true;
            _id = user.Id;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Email = user.Email;
            Role = user.Role;
            
            
            _existingPasswordHash = user.PasswordHash;

            
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        // CREATE
        if (!_isEdit)
        {
            var user = new User
            {
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                Role = Role,

                // HASH PASSWORD
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password)
                
            };

            await _db.CreateUserAsync(user);
        }

        // UPDATE
        else
        {
            var user = new User
            {
                
                Id = _id, 
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                Role = Role,

                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password)
            };

            await _db.UpdateUserAsync(user);
        }

        // Retour admin
        _mainVM.CurrentPage =
            new AdminViewModel(_mainVM);
    }

    [RelayCommand]
    private void Cancel()
    {
        _mainVM.CurrentPage =
            new AdminViewModel(_mainVM);
    }
}