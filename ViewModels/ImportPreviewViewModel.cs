using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Threading.Tasks;
using OceanStock.Models;


namespace OceanStock.ViewModels
{
    public partial class ImportPreviewViewModel : ViewModelBase
    {

        /// <summary>
        /// Liste affichée dans la vue
        /// (pas ProductFish directement !)
        /// </summary>
        public ObservableCollection<ImportPreviewItem> PreviewList { get; } = new();

        // Référence vers MainWindowViewModel pour la navigation
        private readonly MainWindowViewModel _mainVM;

        // Compteur dynamique
        public int SelectedCount => PreviewList.Count(x => x.IsSelected);

        // Bleu UNIQUEMENT quand TOUT est sélectionné
        public bool IsAllSelected =>
            PreviewList.Any() && PreviewList.All(x => x.IsSelected);
       
        public bool CanImport => SelectedCount > 0;
        
        public ImportPreviewViewModel(
            IEnumerable<ProductFish> imported,
            IEnumerable<ProductFish> existing,
            MainWindowViewModel mainVM)
        {
            _mainVM = mainVM;

            foreach (var fish in imported)
            {
                var existingFish =
                    existing.FirstOrDefault(x =>
                        x.Id == fish.Id &&
                        x.IdOwner == Session.CurrentUser.Id
                    );
                
                var status = GetStatus(fish, existingFish);

                var item = new ImportPreviewItem(fish, status);

                item.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(ImportPreviewItem.IsSelected))
                    {
                        OnPropertyChanged(nameof(SelectedCount));
                        OnPropertyChanged(nameof(IsAllSelected));
                        OnPropertyChanged(nameof(IsAllModifiedSelected));
                        OnPropertyChanged(nameof(IsAllNewSelected));
                        OnPropertyChanged(nameof(CanImport));
                    }
                };

                PreviewList.Add(item);
            }
        }
        
        [RelayCommand]
        private void SelectAll()
        {
            bool allSelected = PreviewList.Any() &&
                               PreviewList.All(x => x.IsSelected);

            foreach (var item in PreviewList)
            {
                item.IsSelected = !allSelected;
            }

            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(IsAllSelected));
            OnPropertyChanged(nameof(IsAllNewSelected));
            OnPropertyChanged(nameof(IsAllModifiedSelected));
        }
        
        // Bouton "Nouveaux" vert UNIQUEMENT si tous les nouveaux sont sélectionnés
        public bool IsAllNewSelected
        {
            get
            {
                var newItems = PreviewList.Where(x => x.Status == ImportStatus.New).ToList();

                // ❌ S’il n’y a aucun nouveau → jamais vert
                if (newItems.Count == 0)
                    return false;

                // ✅ Vert seulement si TOUS les nouveaux sont sélectionnés
                return newItems.All(x => x.IsSelected);
            }
        }
        
        // Bouton "Modifiés" orange UNIQUEMENT si tous les modifiés sont sélectionnés
        public bool IsAllModifiedSelected
        {
            get
            {
                var modifiedItems = PreviewList
                    .Where(x => x.Status == ImportStatus.Modified)
                    .ToList();

                // ❌ Aucun modifié → jamais orange
                if (modifiedItems.Count == 0)
                    return false;

                // ✅ Orange seulement si TOUS les modifiés sont sélectionnés
                return modifiedItems.All(x => x.IsSelected);
            }
        }
        
        [RelayCommand]
        private void ToggleNew()
        {
            ToggleByStatus(ImportStatus.New, nameof(IsAllNewSelected));
        }

        [RelayCommand]
        private void ToggleModified()
        {
            ToggleByStatus(ImportStatus.Modified, nameof(IsAllModifiedSelected));
        }
        
        private void ToggleByStatus(ImportStatus status, string propertyToRefresh)
        {
            var items = PreviewList
                .Where(x => x.Status == status)
                .ToList();

            if (items.Count == 0)
                return;

            bool allSelected = items.All(x => x.IsSelected);

            foreach (var item in items)
            {
                item.IsSelected = !allSelected;
            }

            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(IsAllSelected));
            OnPropertyChanged(propertyToRefresh);
        }
        
        

        
        private ImportStatus GetStatus(ProductFish imported, ProductFish? existing)
        {
            if (existing == null)
                return ImportStatus.New;

            bool isDifferent =
                imported.Price != existing.Price ||
                imported.Stock != existing.Stock ||
                imported.Group != existing.Group ||
                imported.Description != existing.Description;

            return isDifferent
                ? ImportStatus.Modified
                : ImportStatus.Conflict;
        }


        // ✅ Bouton Annuler → retour à la page principale
        [RelayCommand]
        private void Cancel()
        {
            _mainVM.BackToMainCommand.Execute(null);
        }

        // Bouton Import → sera codé dans l’étape suivante
        [RelayCommand]
        private async Task ConfirmImport()
        {
            // Items sélectionnés (hors conflits)
            var selectedItems = PreviewList
                .Where(x => x.IsSelected && x.Status != ImportStatus.Conflict)
                .ToList();

            if (selectedItems.Count == 0)
                return;

            foreach (var item in selectedItems)
            {
                var fish = item.Fish;

                // Cherche s’il existe déjà
                var existing = MyGlobals.ProductsFish
                    .FirstOrDefault(x => 
                        x.Name == fish.Name &&
                        x.IdOwner == Session.CurrentUser.Id
                        ); // ou ID

                if (existing == null)
                {
                    // NOUVEAU → ajouter
                    
                    fish.IdOwner = Session.CurrentUser.Id; // sécurité
                    MyGlobals.ProductsFish.Add(fish);
                }
                else
                {
                    // MODIFIÉ → mettre à jour
                    existing.Price = fish.Price;
                    existing.Stock = fish.Stock;
                    existing.Group = fish.Group;
                    existing.Description = fish.Description;
                    existing.Picture = fish.Picture;
                    existing.PicturePath = fish.PicturePath;
                    existing.IdOwner = Session.CurrentUser.Id;
                }
            }

            // Sauvegarde serveur
            await _mainVM.SaveJsonCommand.ExecuteAsync(null);

            // Retour à la collection principale
            _mainVM.BackToMainCommand.Execute(null);
        }
        
    }
}