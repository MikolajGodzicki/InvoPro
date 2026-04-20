using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using InvoPro.Commands;
using InvoPro.Models;
using InvoPro.Services;
using InvoPro.Views;
using Microsoft.Win32;
using System.IO;

namespace InvoPro.ViewModels
{
    public class InvoiceEditViewModel : ViewModelBase
    {
        private Invoice _invoice;
        private bool _isEditMode;
        private string _windowTitle = string.Empty;
        private InvoiceItem? _selectedItem;
        private readonly IPdfService _pdfService;

        public ObservableCollection<string> Contractors { get; } = new();
        public ObservableCollection<string> DocumentTitles { get; } = new() { "WZ", "Dokument Dostawy" };

        public Invoice Invoice
        {
            get => _invoice;
            set => SetProperty(ref _invoice, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        public InvoiceItem? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        // W³aœciwoœci bindowane do pól formularza
        public string Number
        {
            get => Invoice.Number;
            set
            {
                Invoice.Number = value;
                OnPropertyChanged();
            }
        }

        public DateTime IssueDate
        {
            get => Invoice.IssueDate;
            set
            {
                Invoice.IssueDate = value;
                OnPropertyChanged();
                DueDate = value;
            }
        }

        public DateTime DueDate
        {
            get => Invoice.DueDate;
            set
            {
                Invoice.DueDate = value;
                OnPropertyChanged();
            }
        }

        public string ClientName
        {
            get => Invoice.ClientName;
            set
            {
                Invoice.ClientName = value;
                OnPropertyChanged();
            }
        }

        public string ClientAddress
        {
            get => Invoice.ClientAddress;
            set
            {
                Invoice.ClientAddress = value;
                OnPropertyChanged();
            }
        }

        public string IssuedBy
        {
            get => Invoice.ClientAddress;
            set
            {
                Invoice.ClientAddress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ClientAddress));
            }
        }

        public string ClientNip
        {
            get => Invoice.ClientNip;
            set
            {
                Invoice.ClientNip = value;
                OnPropertyChanged();
            }
        }

        public string DocumentTitle
        {
            get => string.IsNullOrWhiteSpace(Invoice.ClientNip) ? "WZ" : Invoice.ClientNip;
            set
            {
                Invoice.ClientNip = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ClientNip));
            }
        }

        public string Description
        {
            get => Invoice.Description;
            set
            {
                Invoice.Description = value;
                OnPropertyChanged();
            }
        }

        // W³aœciwoœci dla sumy faktury (tylko do odczytu w widoku)
        public decimal TotalNet => Invoice.TotalNet;
        public decimal TotalVat => Invoice.TotalVat;
        public decimal TotalAmount => Invoice.TotalAmount;

        // Komendy
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand GenerateNumberCommand { get; private set; }
        public ICommand AddItemCommand { get; private set; }
        public ICommand EditItemCommand { get; private set; }
        public ICommand DeleteItemCommand { get; private set; }
        public ICommand OpenContractorsCommand { get; private set; }
        public ICommand GeneratePdfCommand { get; private set; }
        public ICommand PreviewPdfCommand { get; private set; }

        // Wynik dialogu
        public bool? DialogResult { get; set; }

        // Konstruktor dla dodawania nowej faktury
        public InvoiceEditViewModel()
        {
            _invoice = new Invoice
            {
                IssueDate = DateTime.Now,
                DueDate = DateTime.Now,
                ClientNip = "WZ"
            };
            
            // Spróbuj iText, jeœli siê nie uda, u¿yj HTML
            try
            {
                _pdfService = new PdfService();
            }
            catch
            {
                _pdfService = new HtmlToPdfService();
            }
            
            InitializeViewModel(false);
            _ = LoadContractorsAsync();
            _ = LoadDefaultIssuedByAsync();
        }

        // Konstruktor dla edycji istniej¹cej faktury
        public InvoiceEditViewModel(Invoice invoiceToEdit)
        {
            _invoice = new Invoice
            {
                Id = invoiceToEdit.Id,
                Number = invoiceToEdit.Number,
                IssueDate = invoiceToEdit.IssueDate,
                DueDate = invoiceToEdit.DueDate,
                ClientName = invoiceToEdit.ClientName,
                ClientAddress = invoiceToEdit.ClientAddress,
                ClientNip = invoiceToEdit.ClientNip,
                Description = invoiceToEdit.Description
            };

            // Skopiuj pozycje faktury (bez kopiowania ID - bêd¹ przypisane przez bazê)
            foreach (var item in invoiceToEdit.Items)
            {
                _invoice.Items.Add(new InvoiceItem
                {
                    Id = item.Id, // Zachowaj oryginalne ID dla edycji
                    Name = item.Name,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPriceNet = item.UnitPriceNet,
                    DiscountPercentage = item.DiscountPercentage,
                    VatRate = item.VatRate
                });
            }

            // Spróbuj iText, jeœli siê nie uda, u¿yj HTML
            try
            {
                _pdfService = new PdfService();
            }
            catch
            {
                _pdfService = new HtmlToPdfService();
            }
            
            InitializeViewModel(true);
            _ = LoadContractorsAsync();
            _ = LoadDefaultIssuedByAsync();
        }

        private void InitializeViewModel(bool isEditMode)
        {
            IsEditMode = isEditMode;
            WindowTitle = isEditMode ? $"Edycja dokumentu WZ - {Invoice.Number}" : "Nowy dokument WZ";

            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            GenerateNumberCommand = new RelayCommand(GenerateNumber, () => !IsEditMode);
            AddItemCommand = new RelayCommand(AddItem);
            EditItemCommand = new RelayCommand(EditItem, CanEditItem);
            DeleteItemCommand = new RelayCommand(DeleteItem, CanDeleteItem);
            OpenContractorsCommand = new RelayCommand(OpenContractors);
            GeneratePdfCommand = new RelayCommand(GeneratePdf, CanGeneratePdf);
            PreviewPdfCommand = new RelayCommand(PreviewPdf, CanGeneratePdf);

            // Pod³¹cz nas³uchiwanie zmian w fakturze dla aktualizacji sum
            Invoice.PropertyChanged += Invoice_PropertyChanged;
        }

        private void Invoice_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
        if (e.PropertyName == nameof(Invoice.TotalNet) ||
            e.PropertyName == nameof(Invoice.TotalVat) ||
            e.PropertyName == nameof(Invoice.TotalAmount))
        {
            OnPropertyChanged(nameof(TotalNet));
            OnPropertyChanged(nameof(TotalVat));
            OnPropertyChanged(nameof(TotalAmount));
        }
        }

        private void AddItem()
        {
            var newItem = new InvoiceItem
            {
                Id = 0, // ID bêdzie ustawione przez bazê danych
                Name = "Nowa pozycja",
                Quantity = 1,
                Unit = "szt.",
                UnitPriceNet = 0,
                VatRate = 0
            };

            Invoice.Items.Add(newItem);
            SelectedItem = newItem;
        }

        private void EditItem()
        {
            if (SelectedItem == null) return;
            // Tu mo¿na dodaæ okno do edycji pozycji lub edytowaæ inline
            // Na razie pozycje bêd¹ edytowane bezpoœrednio w DataGrid
        }

        private bool CanEditItem()
        {
            return SelectedItem != null;
        }

        private void DeleteItem()
        {
            if (SelectedItem == null) return;

            var result = MessageBox.Show($"Czy na pewno chcesz usun¹æ pozycjê '{SelectedItem.Name}'?",
                "Potwierdzenie usuniêcia", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Invoice.Items.Remove(SelectedItem);
                SelectedItem = null;
            }
        }

        private bool CanDeleteItem()
        {
            return SelectedItem != null;
        }

        private void Save()
        {
            if (!ValidateInvoice())
                return;

            DialogResult = true;
            CloseWindow();
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(ClientName) &&
                   !string.IsNullOrWhiteSpace(Number) &&
                   !string.IsNullOrWhiteSpace(IssuedBy) &&
                   !string.IsNullOrWhiteSpace(DocumentTitle);
        }

        private void Cancel()
        {
            DialogResult = false;
            CloseWindow();
        }

        private void GenerateNumber()
        {
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;
            var sequence = DateTime.Now.ToString("ddHHmmss");

            Number = $"WZ {sequence}/{month:D2}/{year}";
        }

        private bool ValidateInvoice()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Number))
                errors.Add("Numer WZ jest wymagany.");

            if (string.IsNullOrWhiteSpace(ClientName))
                errors.Add("Kontrahent jest wymagany.");

            if (string.IsNullOrWhiteSpace(IssuedBy))
                errors.Add("Pole 'Wystawi³' jest wymagane.");

            if (string.IsNullOrWhiteSpace(DocumentTitle))
                errors.Add("Tytu³ dokumentu jest wymagany.");

            if (Invoice.Items.Count == 0)
                errors.Add("Dokument musi zawieraæ co najmniej jedn¹ pozycjê towarow¹.");

            if (Invoice.Items.Any(item => string.IsNullOrWhiteSpace(item.Name)))
                errors.Add("Wszystkie pozycje musz¹ mieæ nazwê.");

            if (Invoice.Items.Any(item => item.Quantity <= 0))
                errors.Add("Iloœæ dla wszystkich pozycji musi byæ wiêksza od zera.");

            if (Invoice.Items.Any(item => item.UnitPriceNet < 0))
                errors.Add("Cena jednostkowa netto nie mo¿e byæ ujemna.");

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

        private async void GeneratePdf()
        {
            if (!ValidateInvoiceForPdf())
                return;

            try
            {
                // Wybór lokalizacji zapisu
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Pliki PDF (*.pdf)|*.pdf",
                    FileName = $"WZ_{Invoice.Number.Replace("/", "_")}",
                    DefaultExt = "pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var filePath = await _pdfService.GenerateInvoicePdfAsync(Invoice, Path.GetDirectoryName(saveDialog.FileName));
                    
                    var fileExtension = Path.GetExtension(filePath).ToLower();
                    var message = fileExtension == ".pdf" 
                        ? $"Dokument WZ zosta³ zapisany jako PDF:\n{filePath}\n\nCzy chcesz otworzyæ plik?"
                        : $"Dokument WZ zosta³ zapisany jako HTML (do wydruku jako PDF):\n{filePath}\n\nCzy chcesz otworzyæ plik?";
                    
                    var result = MessageBox.Show(message, "Dokument wygenerowany", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        await _pdfService.OpenPdfAsync(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"B³¹d podczas generowania dokumentu: {ex.Message}", 
                    "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void PreviewPdf()
        {
            if (!ValidateInvoiceForPdf())
                return;

            try
            {
                // Generuj w folderze tymczasowym
                var tempPath = Path.GetTempPath();
                var filePath = await _pdfService.GenerateInvoicePdfAsync(Invoice, tempPath);
                
                // Otwórz podgl¹d
                await _pdfService.OpenPdfAsync(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"B³¹d podczas generowania podgl¹du: {ex.Message}", 
                    "B³¹d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanGeneratePdf()
        {
            return !string.IsNullOrWhiteSpace(Invoice.Number) && 
                   !string.IsNullOrWhiteSpace(Invoice.ClientName) &&
                   !string.IsNullOrWhiteSpace(IssuedBy) &&
                   Invoice.Items.Count > 0;
        }

        private bool ValidateInvoiceForPdf()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Number))
                errors.Add("Numer WZ jest wymagany do generowania PDF.");

            if (string.IsNullOrWhiteSpace(ClientName))
                errors.Add("Kontrahent jest wymagany do generowania PDF.");

            if (string.IsNullOrWhiteSpace(IssuedBy))
                errors.Add("Pole 'Wystawi³' jest wymagane do generowania PDF.");

            if (string.IsNullOrWhiteSpace(DocumentTitle))
                errors.Add("Tytu³ dokumentu jest wymagany do generowania PDF.");

            if (Invoice.Items.Count == 0)
                errors.Add("Dokument musi zawieraæ co najmniej jedn¹ pozycjê do generowania PDF.");

            if (Invoice.Items.Any(item => string.IsNullOrWhiteSpace(item.Name)))
                errors.Add("Wszystkie pozycje musz¹ mieæ nazwê do generowania PDF.");

            if (errors.Any())
            {
                var message = "Nie mo¿na wygenerowaæ PDF. Proszê poprawiæ nastêpuj¹ce b³êdy:\n\n" + string.Join("\n", errors);
                MessageBox.Show(message, "B³êdy walidacji PDF", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private async Task LoadContractorsAsync()
        {
            try
            {
                var contractorService = new ContractorService();
                var contractors = await contractorService.GetAllContractorsAsync();

                Contractors.Clear();
                foreach (var contractor in contractors)
                {
                    Contractors.Add(contractor.Name);
                }

                if (!string.IsNullOrWhiteSpace(ClientName) && !Contractors.Contains(ClientName))
                {
                    Contractors.Add(ClientName);
                }
            }
            catch
            {
            }
        }

        private void OpenContractors()
        {
            var contractorsWindow = new ContractorsWindow
            {
                Owner = Application.Current.MainWindow
            };

            contractorsWindow.ShowDialog();
            _ = LoadContractorsAsync();
        }

        private async Task LoadDefaultIssuedByAsync()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(IssuedBy))
                    return;

                var companyService = new CompanyService();
                var company = await companyService.GetCompanyInfoAsync();

                if (!string.IsNullOrWhiteSpace(company?.DefaultIssuedBy))
                {
                    IssuedBy = company.DefaultIssuedBy;
                }
            }
            catch
            {
            }
        }
    }
}