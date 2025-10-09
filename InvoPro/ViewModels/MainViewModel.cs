using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using InvoPro.Commands;
using InvoPro.Models;
using InvoPro.Views;
using InvoPro.Services;

namespace InvoPro.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private Invoice? _selectedInvoice;
        private string _searchText = string.Empty;
        private bool _isLoading = false;
        private string _companyName = "Nie ustawiono";
        private readonly IInvoiceService _invoiceService;
        private readonly ICompanyService _companyService;

        public ObservableCollection<Invoice> Invoices { get; set; }
        public ObservableCollection<Invoice> FilteredInvoices { get; set; }

        public Invoice? SelectedInvoice
        {
            get => _selectedInvoice;
            set => SetProperty(ref _selectedInvoice, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterInvoices();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string CompanyName
        {
            get => _companyName;
            set => SetProperty(ref _companyName, value);
        }

        public ICommand AddInvoiceCommand { get; }
        public ICommand DeleteInvoiceCommand { get; }
        public ICommand EditInvoiceCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SettingsCommand { get; }
        public ICommand DiagnosticsCommand { get; }

        public MainViewModel()
        {
            _invoiceService = new InvoiceService();
            _companyService = new CompanyService();
            
            Invoices = new ObservableCollection<Invoice>();
            FilteredInvoices = new ObservableCollection<Invoice>();

            AddInvoiceCommand = new RelayCommand(AddInvoice);
            DeleteInvoiceCommand = new RelayCommand(DeleteInvoice, CanDeleteInvoice);
            EditInvoiceCommand = new RelayCommand(EditInvoice, CanEditInvoice);
            RefreshCommand = new RelayCommand(RefreshInvoices);
            SettingsCommand = new RelayCommand(OpenSettings);
            DiagnosticsCommand = new RelayCommand(OpenDiagnostics);

            // Inicjalizuj bazê danych i za³aduj dane
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                IsLoading = true;
                
                // Inicjalizuj bazê danych
                await _invoiceService.InitializeDatabaseAsync();
                
                // Za³aduj faktury
                await LoadInvoicesAsync();
                
                // Za³aduj nazwê firmy
                await LoadCompanyNameAsync();
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException?.Message ?? "";
                var fullMessage = $"B³¹d podczas inicjalizacji bazy danych:\n\n{ex.Message}";
                if (!string.IsNullOrEmpty(innerException))
                {
                    fullMessage += $"\n\nSzczegó³y: {innerException}";
                }
                
                // Jeœli b³¹d zwi¹zany z tabel¹ ju¿ istniej¹c¹, spróbuj zresetowaæ bazê
                if (ex.Message.Contains("already exists"))
                {
                    var result = MessageBox.Show($"{fullMessage}\n\nCzy chcesz zresetowaæ bazê danych?", 
                        "Konflikt bazy danych", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            await _invoiceService.ResetDatabaseAsync();
                            await LoadInvoicesAsync();
                            await LoadCompanyNameAsync();
                            MessageBox.Show("Baza danych zosta³a zresetowana pomyœlnie.", 
                                "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception resetEx)
                        {
                            MessageBox.Show($"B³¹d podczas resetowania bazy: {resetEx.Message}", 
                                "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show(fullMessage, "B³¹d bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void AddInvoice()
        {
            var viewModel = new InvoiceEditViewModel();
            var window = new Views.InvoiceEditWindow(viewModel)
            {
                Owner = Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    
                    // Zapisz do bazy danych
                    var savedInvoice = await _invoiceService.SaveInvoiceAsync(viewModel.Invoice);
                    
                    // Odœwie¿ listê
                    await LoadInvoicesAsync();
                    
                    // Wybierz nowo dodan¹ fakturê
                    SelectedInvoice = Invoices.FirstOrDefault(i => i.Id == savedInvoice.Id);

                    MessageBox.Show($"Faktura {savedInvoice.Number} zosta³a dodana pomyœlnie.", 
                        "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"B³¹d podczas zapisywania faktury: {ex.Message}", 
                        "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async void DeleteInvoice()
        {
            if (SelectedInvoice == null) return;

            var result = MessageBox.Show($"Czy na pewno chcesz usun¹æ fakturê {SelectedInvoice.Number}?", 
                "Potwierdzenie usuniêcia", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    
                    // Usuñ z bazy danych
                    var deleted = await _invoiceService.DeleteInvoiceAsync(SelectedInvoice.Id);
                    
                    if (deleted)
                    {
                        // Odœwie¿ listê
                        await LoadInvoicesAsync();
                        SelectedInvoice = null;
                        
                        MessageBox.Show("Faktura zosta³a usuniêta pomyœlnie.", 
                            "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Nie uda³o siê usun¹æ faktury.", 
                            "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"B³¹d podczas usuwania faktury: {ex.Message}", 
                        "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private bool CanDeleteInvoice()
        {
            return SelectedInvoice != null && !IsLoading;
        }

        private async void EditInvoice()
        {
            if (SelectedInvoice == null) return;

            var viewModel = new InvoiceEditViewModel(SelectedInvoice);
            var window = new Views.InvoiceEditWindow(viewModel)
            {
                Owner = Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    
                    // Zapisz zmiany do bazy danych
                    await _invoiceService.SaveInvoiceAsync(viewModel.Invoice);
                    
                    // Odœwie¿ listê
                    await LoadInvoicesAsync();
                    
                    // Zachowaj selekcjê
                    SelectedInvoice = Invoices.FirstOrDefault(i => i.Id == viewModel.Invoice.Id);

                    MessageBox.Show($"Faktura {viewModel.Invoice.Number} zosta³a zaktualizowana pomyœlnie.", 
                        "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"B³¹d podczas zapisywania faktury: {ex.Message}", 
                        "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private bool CanEditInvoice()
        {
            return SelectedInvoice != null && !IsLoading;
        }

        private async void RefreshInvoices()
        {
            await LoadInvoicesAsync();
        }

        private async Task LoadInvoicesAsync()
        {
            try
            {
                IsLoading = true;
                
                var invoices = await _invoiceService.GetAllInvoicesAsync();
                
                Invoices.Clear();
                foreach (var invoice in invoices)
                {
                    Invoices.Add(invoice);
                }
                
                FilterInvoices();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"B³¹d podczas ³adowania faktur: {ex.Message}", 
                    "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterInvoices()
        {
            FilteredInvoices.Clear();

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? Invoices
                : Invoices.Where(i => 
                    i.Number.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    i.ClientName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    i.ClientNip.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var invoice in filtered)
            {
                FilteredInvoices.Add(invoice);
            }
        }

        private void OpenSettings()
        {
            var settingsWindow = new CompanySettingsWindow
            {
                Owner = Application.Current.MainWindow
            };

            settingsWindow.ShowDialog();
        }

        private void OpenDiagnostics()
        {
            var diagnosticsWindow = new DiagnosticsWindow
            {
                Owner = Application.Current.MainWindow
            };

            diagnosticsWindow.ShowDialog();
        }

        private async Task LoadCompanyNameAsync()
        {
            try
            {
                var companyService = new CompanyService();
                var company = await companyService.GetCompanyInfoAsync();
                CompanyName = company?.Name ?? "Nie ustawiono";
            }
            catch
            {
                CompanyName = "B³¹d ³adowania";
            }
        }
    }
}