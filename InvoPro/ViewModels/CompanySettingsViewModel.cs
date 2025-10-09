using System.Windows;
using System.Windows.Input;
using InvoPro.Commands;
using InvoPro.Models;
using InvoPro.Services;

namespace InvoPro.ViewModels
{
    public class CompanySettingsViewModel : ViewModelBase
    {
        private CompanyInfo _companyInfo;
        private readonly ICompanyService _companyService;
        private bool _isLoading = false;

        public CompanyInfo CompanyInfo
        {
            get => _companyInfo;
            set => SetProperty(ref _companyInfo, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // W³aœciwoœci bindowane do pól formularza
        public string Name
        {
            get => CompanyInfo.Name;
            set
            {
                CompanyInfo.Name = value;
                OnPropertyChanged();
            }
        }

        public string Address
        {
            get => CompanyInfo.Address;
            set
            {
                CompanyInfo.Address = value;
                OnPropertyChanged();
            }
        }

        public string Nip
        {
            get => CompanyInfo.Nip;
            set
            {
                CompanyInfo.Nip = value;
                OnPropertyChanged();
            }
        }

        public string Phone
        {
            get => CompanyInfo.Phone;
            set
            {
                CompanyInfo.Phone = value;
                OnPropertyChanged();
            }
        }

        public string Email
        {
            get => CompanyInfo.Email;
            set
            {
                CompanyInfo.Email = value;
                OnPropertyChanged();
            }
        }

        public string Website
        {
            get => CompanyInfo.Website;
            set
            {
                CompanyInfo.Website = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        // Wynik dialogu
        public bool? DialogResult { get; set; }

        public CompanySettingsViewModel()
        {
            _companyService = new CompanyService();
            _companyInfo = new CompanyInfo();

            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);

            LoadCompanyInfoAsync();
        }

        private async void LoadCompanyInfoAsync()
        {
            try
            {
                IsLoading = true;
                
                var existingInfo = await _companyService.GetCompanyInfoAsync();
                if (existingInfo != null)
                {
                    CompanyInfo = new CompanyInfo
                    {
                        Id = existingInfo.Id,
                        Name = existingInfo.Name,
                        Address = existingInfo.Address,
                        Nip = existingInfo.Nip,
                        Phone = existingInfo.Phone,
                        Email = existingInfo.Email,
                        Website = existingInfo.Website
                    };
                    
                    // Aktualizuj bindowane w³aœciwoœci
                    OnPropertyChanged(nameof(Name));
                    OnPropertyChanged(nameof(Address));
                    OnPropertyChanged(nameof(Nip));
                    OnPropertyChanged(nameof(Phone));
                    OnPropertyChanged(nameof(Email));
                    OnPropertyChanged(nameof(Website));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"B³¹d podczas ³adowania danych firmy: {ex.Message}", 
                    "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void Save()
        {
            if (!ValidateCompanyInfo())
                return;

            try
            {
                IsLoading = true;
                
                await _companyService.SaveCompanyInfoAsync(CompanyInfo);
                
                DialogResult = true;
                CloseWindow();
                
                MessageBox.Show("Dane firmy zosta³y zapisane pomyœlnie.", 
                    "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"B³¹d podczas zapisywania danych firmy: {ex.Message}", 
                    "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanSave()
        {
            return !IsLoading && 
                   !string.IsNullOrWhiteSpace(Name) && 
                   !string.IsNullOrWhiteSpace(Address) && 
                   !string.IsNullOrWhiteSpace(Nip);
        }

        private void Cancel()
        {
            DialogResult = false;
            CloseWindow();
        }

        private bool ValidateCompanyInfo()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Nazwa firmy jest wymagana.");

            if (string.IsNullOrWhiteSpace(Address))
                errors.Add("Adres firmy jest wymagany.");

            if (string.IsNullOrWhiteSpace(Nip))
                errors.Add("NIP firmy jest wymagany.");

            // Walidacja NIP (opcjonalna - sprawdzenie d³ugoœci)
            if (!string.IsNullOrWhiteSpace(Nip) && Nip.Length != 10)
                errors.Add("NIP powinien sk³adaæ siê z 10 cyfr.");

            // Walidacja email (opcjonalna)
            if (!string.IsNullOrWhiteSpace(Email) && !Email.Contains("@"))
                errors.Add("Nieprawid³owy format adresu email.");

            if (errors.Any())
            {
                var message = "Proszê poprawiæ nastêpuj¹ce b³êdy:\n\n" + string.Join("\n", errors);
                MessageBox.Show(message, "B³êdy walidacji", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void CloseWindow()
        {
            // To bêdzie obs³u¿one przez okno
            OnPropertyChanged(nameof(DialogResult));
        }
    }
}