using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using InvoPro.Commands;
using InvoPro.Models;

namespace InvoPro.ViewModels
{
    public class InvoiceEditViewModel : ViewModelBase
    {
        private Invoice _invoice;
        private bool _isEditMode;
        private string _windowTitle = string.Empty;
        private InvoiceItem? _selectedItem;

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
                // Automatyczne ustawienie terminu p³atnoœci na 30 dni
                if (DueDate <= IssueDate)
                {
                    DueDate = value.AddDays(30);
                }
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

        public string ClientNip
        {
            get => Invoice.ClientNip;
            set
            {
                Invoice.ClientNip = value;
                OnPropertyChanged();
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

        // Wynik dialogu
        public bool? DialogResult { get; set; }

        // Konstruktor dla dodawania nowej faktury
        public InvoiceEditViewModel()
        {
            _invoice = new Invoice
            {
                IssueDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(30)
            };

            InitializeViewModel(false);
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

            // Skopiuj pozycje faktury
            foreach (var item in invoiceToEdit.Items)
            {
                _invoice.Items.Add(new InvoiceItem
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPriceNet = item.UnitPriceNet,
                    DiscountPercentage = item.DiscountPercentage,
                    VatRate = item.VatRate
                });
            }

            InitializeViewModel(true);
        }

        private void InitializeViewModel(bool isEditMode)
        {
            IsEditMode = isEditMode;
            WindowTitle = isEditMode ? $"Edycja faktury - {Invoice.Number}" : "Nowa faktura";

            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            GenerateNumberCommand = new RelayCommand(GenerateNumber, () => !IsEditMode);
            AddItemCommand = new RelayCommand(AddItem);
            EditItemCommand = new RelayCommand(EditItem, CanEditItem);
            DeleteItemCommand = new RelayCommand(DeleteItem, CanDeleteItem);

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
                Id = Invoice.Items.Count > 0 ? Invoice.Items.Max(i => i.Id) + 1 : 1,
                Name = "Nowa pozycja",
                Quantity = 1,
                Unit = "szt.",
                UnitPriceNet = 0,
                VatRate = 23
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
                   !string.IsNullOrWhiteSpace(Number);
                   // Usun¹³em wymaganie pozycji, ¿eby sprawdziæ czy to nie blokuje
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
            var day = DateTime.Now.Day;
            var time = DateTime.Now.ToString("HHmmss");
            
            Number = $"FV/{year}/{month:D2}/{day:D2}/{time}";
        }

        private bool ValidateInvoice()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Number))
                errors.Add("Numer faktury jest wymagany.");

            if (string.IsNullOrWhiteSpace(ClientName))
                errors.Add("Nazwa klienta jest wymagana.");

            if (string.IsNullOrWhiteSpace(ClientAddress))
                errors.Add("Adres klienta jest wymagany.");

            if (string.IsNullOrWhiteSpace(ClientNip))
                errors.Add("NIP klienta jest wymagany.");

            // Tymczasowo usunê wymaganie pozycji dla testów
            // if (Invoice.Items.Count == 0)
            //     errors.Add("Faktura musi zawieraæ co najmniej jedn¹ pozycjê.");

            if (Invoice.Items.Any(item => string.IsNullOrWhiteSpace(item.Name)))
                errors.Add("Wszystkie pozycje musz¹ mieæ nazwê.");

            if (Invoice.Items.Any(item => item.Quantity <= 0))
                errors.Add("Iloœæ dla wszystkich pozycji musi byæ wiêksza od zera.");

            if (Invoice.Items.Any(item => item.UnitPriceNet < 0))
                errors.Add("Cena jednostkowa netto nie mo¿e byæ ujemna.");

            if (DueDate <= IssueDate)
                errors.Add("Termin p³atnoœci musi byæ póŸniejszy ni¿ data wystawienia.");

            // Walidacja NIP (opcjonalna - sprawdzenie d³ugoœci)
            if (!string.IsNullOrWhiteSpace(ClientNip) && ClientNip.Length != 10)
                errors.Add("NIP powinien sk³adaæ siê z 10 cyfr.");

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