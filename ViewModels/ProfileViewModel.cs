using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OceanStock.Services;

namespace OceanStock.ViewModels;

public partial class ProfileViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainVM;
    private readonly DatabaseServices _db;

    [ObservableProperty] private string firstName = "";
    [ObservableProperty] private string lastName = "";
    [ObservableProperty] private string newPassword = "";
    [ObservableProperty] private string? errorMessage;
    [ObservableProperty] private string? successMessage;

    public string Email => Session.CurrentUser!.Email;

    public ProfileViewModel(MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;
        _db = new DatabaseServices();
        FirstName = Session.CurrentUser!.FirstName;
        LastName = Session.CurrentUser!.LastName;
    }

    [RelayCommand]
    private async Task Save()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
        {
            ErrorMessage = "Le prénom et le nom sont obligatoires.";
            return;
        }

        var user = Session.CurrentUser!;
        user.FirstName = FirstName.Trim();
        user.LastName = LastName.Trim();

        if (!string.IsNullOrWhiteSpace(NewPassword))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword);

        await _db.UpdateUserAsync(user);
        Session.CurrentUser = user;

        SuccessMessage = "Profil mis à jour avec succès.";
    }

    [RelayCommand]
    private void Back()
    {
        _mainVM.BackToMainCommand.Execute(null);
    }
}
