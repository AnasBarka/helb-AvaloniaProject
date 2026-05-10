using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OceanStock.Models;
using OceanStock.Services;

namespace OceanStock.ViewModels;

public partial class AdminViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainVM;
    private readonly DatabaseServices _db;

    public ObservableCollection<User> Users { get; } = new();
    public ObservableCollection<User> FilteredUsers { get; } = new();

    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private User? selectedUser;
    [ObservableProperty] private ObservableCollection<User> selectedUsers = new();

    public AdminViewModel(MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;
        _db = new DatabaseServices();

        if (Session.CurrentUser?.IsAdmin != true)
        {
            _mainVM.BackToMainCommand.Execute(null);
            return;
        }

        _ = LoadUsers();
    }

    private async Task LoadUsers()
    {
        var users = await _db.GetUsersAsync();

        Users.Clear();

        foreach (var u in users)
            Users.Add(u);

        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredUsers.Clear();

        var filtered = Users.Where(u =>
            string.IsNullOrWhiteSpace(SearchText)
            || u.FirstName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || u.LastName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || u.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || (u.IsAdmin ? "Admin" : "User").Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        foreach (var user in filtered)
            FilteredUsers.Add(user);
    }

    [RelayCommand]
    private void GoBack()
    {
        _mainVM.BackToMainCommand.Execute(null);
    }

    [RelayCommand]
    private async Task DeleteUser()
    {
        if (SelectedUser == null)
            return;

        await _db.DeleteUserAsync(SelectedUser.Id);
        Users.Remove(SelectedUser);
        ApplyFilter();
    }

    [RelayCommand]
    private void UpdateUser()
    {
        if (SelectedUser == null)
            return;

        _mainVM.CurrentPage = new UserFormViewModel(_mainVM, SelectedUser);
    }

    [RelayCommand]
    private void CreateUser()
    {
        _mainVM.CurrentPage = new UserFormViewModel(_mainVM, null);
    }

    [RelayCommand]
    private void ViewCollection()
    {
        if (SelectedUser == null)
            return;

        Session.SelectedUserIds = new List<string> { SelectedUser.Id };
        _mainVM.BackToMainCommand.Execute(null);
    }
}
