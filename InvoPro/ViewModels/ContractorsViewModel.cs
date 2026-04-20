using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using InvoPro.Commands;
using InvoPro.Models;
using InvoPro.Services;

namespace InvoPro.ViewModels
{
    public class ContractorsViewModel : ViewModelBase
    {
        private readonly IContractorService _contractorService;
        private Contractor _current = new();
        private Contractor? _selectedContractor;

        public ObservableCollection<Contractor> Contractors { get; } = new();

        public Contractor Current
        {
            get => _current;
            set => SetProperty(ref _current, value);
        }

        public Contractor? SelectedContractor
        {
            get => _selectedContractor;
            set
            {
                if (SetProperty(ref _selectedContractor, value) && value != null)
                {
                    Current = new Contractor
                    {
                        Id = value.Id,
                        Name = value.Name,
                        Nip = value.Nip,
                        Address = value.Address,
                        Regon = value.Regon,
                        Gln = value.Gln
                    };

                    OnPropertyChanged(nameof(Name));
                    OnPropertyChanged(nameof(Nip));
                    OnPropertyChanged(nameof(Address));
                    OnPropertyChanged(nameof(Regon));
                    OnPropertyChanged(nameof(Gln));
                }
            }
        }

        public string Name
        {
            get => Current.Name;
            set
            {
                Current.Name = value;
                OnPropertyChanged();
            }
        }

        public string Nip
        {
            get => Current.Nip;
            set
            {
                Current.Nip = value;
                OnPropertyChanged();
            }
        }

        public string Address
        {
            get => Current.Address;
            set
            {
                Current.Address = value;
                OnPropertyChanged();
            }
        }

        public string Regon
        {
            get => Current.Regon;
            set
            {
                Current.Regon = value;
                OnPropertyChanged();
            }
        }

        public string Gln
        {
            get => Current.Gln;
            set
            {
                Current.Gln = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveContractorCommand { get; }
        public ICommand DeleteContractorCommand { get; }
        public ICommand NewContractorCommand { get; }

        public ContractorsViewModel()
        {
            _contractorService = new ContractorService();

            SaveContractorCommand = new RelayCommand(SaveContractor, CanSaveContractor);
            DeleteContractorCommand = new RelayCommand(DeleteContractor, CanDeleteContractor);
            NewContractorCommand = new RelayCommand(NewContractor);

            _ = LoadContractorsAsync();
        }

        private async Task LoadContractorsAsync()
        {
            try
            {
                var contractors = await _contractorService.GetAllContractorsAsync();
                Contractors.Clear();
                foreach (var contractor in contractors)
                {
                    Contractors.Add(contractor);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas ładowania kontrahentów: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveContractor()
        {
            if (!CanSaveContractor())
                return;

            try
            {
                await _contractorService.SaveContractorAsync(Current);
                await LoadContractorsAsync();
                NewContractor();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas zapisywania kontrahenta: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSaveContractor()
        {
            return !string.IsNullOrWhiteSpace(Name);
        }

        private async void DeleteContractor()
        {
            if (SelectedContractor == null)
                return;

            var result = MessageBox.Show("Czy usunąć wybranego kontrahenta?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await _contractorService.DeleteContractorAsync(SelectedContractor.Id);
                await LoadContractorsAsync();
                NewContractor();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas usuwania kontrahenta: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanDeleteContractor()
        {
            return SelectedContractor != null;
        }

        private void NewContractor()
        {
            Current = new Contractor();
            SelectedContractor = null;

            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Nip));
            OnPropertyChanged(nameof(Address));
            OnPropertyChanged(nameof(Regon));
            OnPropertyChanged(nameof(Gln));
        }
    }
}
