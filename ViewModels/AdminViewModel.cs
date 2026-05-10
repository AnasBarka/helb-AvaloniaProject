using System;
using System.Collections.Generic;
using System.Linq; 
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using OceanStock.Models;
using OceanStock.Services;

namespace OceanStock.ViewModels;

public partial class AdminViewModel : ViewModelBase
{
    // =========================================================
    // Référence vers le MainWindowViewModel
    // Permet la navigation entre les pages
    // =========================================================
    private readonly MainWindowViewModel _mainVM;

    // =========================================================
    // Service MongoDB
    // Gère les appels vers la base de données
    // =========================================================
    private readonly DatabaseServices _db;

    // =========================================================
    // Liste COMPLÈTE des utilisateurs
    // Contient tous les users venant de MongoDB
    // =========================================================
    public ObservableCollection<User> Users { get; } = new();

    // =========================================================
    // Liste FILTRÉE affichée dans le DataGrid
    // C’est cette liste qui est liée à la vue
    // =========================================================
    public ObservableCollection<User> FilteredUsers { get; } = new();

    // =========================================================
    // Texte de recherche
    // Dès qu'il change → filtre automatique
    // =========================================================
    [ObservableProperty]
    private string searchText = "";
    
    [ObservableProperty]
    private User? selectedUser;
    
    
    [ObservableProperty]
    private ObservableCollection<User> selectedUsers = new();


    // =========================================================
    // Constructeur
    // =========================================================
    public AdminViewModel(MainWindowViewModel mainVM)
    {
        _mainVM = mainVM;

        // Initialisation MongoDB
        _db = new DatabaseServices();

        // =====================================================
        // Sécurité :
        // Si l'utilisateur n'est pas admin
        // retour à la page principale
        // =====================================================
        if (Session.CurrentUser?.IsAdmin != true)
        {
            _mainVM.BackToMainCommand.Execute(null);
            return;
        }

        // =====================================================
        // Chargement des utilisateurs MongoDB
        // =====================================================
        _ = LoadUsers();
    }

    // =========================================================
    // Chargement des users depuis MongoDB
    // =========================================================
    private async Task LoadUsers()
    {
        // Récupère tous les utilisateurs
        var users = await _db.GetUsersAsync();

        // Nettoie la liste avant rechargement
        Users.Clear();

        // Ajoute les utilisateurs dans la collection
        foreach (var u in users)
        {
            Users.Add(u);

            // DEBUG console
            Console.WriteLine("User : " + u.Email);
        }

        // DEBUG count
        Console.WriteLine("COUNT = " + Users.Count);

        // Applique le filtre après chargement
        ApplyFilter();
    }

    // =========================================================
    // Détecte automatiquement les changements
    // du texte de recherche
    // =========================================================
    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    // =========================================================
    // Filtre les utilisateurs selon :
    // prénom / nom / email / rôle
    // =========================================================
    private void ApplyFilter()
    {
        // Vide la liste affichée
        FilteredUsers.Clear();

        // LINQ : filtre les users
        var filtered = Users.Where(u =>

            // Si recherche vide → tout afficher
            string.IsNullOrWhiteSpace(SearchText)

            // Recherche prénom
            || u.FirstName.Contains(SearchText,
                StringComparison.OrdinalIgnoreCase)

            // Recherche nom
            || u.LastName.Contains(SearchText,
                StringComparison.OrdinalIgnoreCase)

            // Recherche email
            || u.Email.Contains(SearchText,
                StringComparison.OrdinalIgnoreCase)

            // Recherche rôle
            || (u.IsAdmin ? "Admin" : "User")
            .Contains(SearchText, StringComparison.OrdinalIgnoreCase)

        );

        // Ajoute les users filtrés
        foreach (var user in filtered)
        {
            FilteredUsers.Add(user);
        }
    }

    // =========================================================
    // Bouton retour
    // Retour à la collection principale
    // =========================================================
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

        _mainVM.CurrentPage =
            new UserFormViewModel(_mainVM, SelectedUser);
    }
    
    [RelayCommand]
    private void CreateUser()
    {
        _mainVM.CurrentPage =
            new UserFormViewModel(_mainVM, null);
    }
    
    

    [RelayCommand]
    private void ViewCollection()
    {
        if (SelectedUser == null)
            return;

        Session.SelectedUserIds = new List<string>
        {
            SelectedUser.Id
        };

        _mainVM.BackToMainCommand.Execute(null);
    }


}