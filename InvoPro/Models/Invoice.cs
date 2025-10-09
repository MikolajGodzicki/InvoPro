using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace InvoPro.Models
{
    public class Invoice : INotifyPropertyChanged
    {
        private int _id;
        private string _number = string.Empty;
        private DateTime _issueDate;
        private DateTime _dueDate;
        private string _clientName = string.Empty;
        private string _clientAddress = string.Empty;
        private string _clientNip = string.Empty;
        private string _description = string.Empty;

        public Invoice()
        {
            Items = new ObservableCollection<InvoiceItem>();
            Items.CollectionChanged += Items_CollectionChanged;
        }

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Number
        {
            get => _number;
            set => SetProperty(ref _number, value);
        }

        public DateTime IssueDate
        {
            get => _issueDate;
            set => SetProperty(ref _issueDate, value);
        }

        public DateTime DueDate
        {
            get => _dueDate;
            set => SetProperty(ref _dueDate, value);
        }

        public string ClientName
        {
            get => _clientName;
            set => SetProperty(ref _clientName, value);
        }

        public string ClientAddress
        {
            get => _clientAddress;
            set => SetProperty(ref _clientAddress, value);
        }

        public string ClientNip
        {
            get => _clientNip;
            set => SetProperty(ref _clientNip, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public ObservableCollection<InvoiceItem> Items { get; set; }

        // Obliczane w³aœciwoœci na podstawie pozycji
        public decimal TotalNet => Math.Round(Items?.Sum(item => item.TotalNet) ?? 0, 2);
        public decimal TotalVat => Math.Round(Items?.Sum(item => item.VatAmount) ?? 0, 2);
        public decimal TotalAmount => Math.Round(Items?.Sum(item => item.TotalGross) ?? 0, 2);

        // Formatowanie w PLN
        public string TotalNetFormatted => $"{TotalNet:F2} PLN";
        public string TotalVatFormatted => $"{TotalVat:F2} PLN";
        public string TotalAmountFormatted => $"{TotalAmount:F2} PLN";

        private void Items_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Aktualizuj obliczane w³aœciwoœci gdy zmieni siê lista pozycji
            OnPropertyChanged(nameof(TotalNet));
            OnPropertyChanged(nameof(TotalVat));
            OnPropertyChanged(nameof(TotalAmount));

            // Pod³¹cz/od³¹cz nas³uchiwanie zmian w pozycjach
            if (e.OldItems != null)
            {
                foreach (InvoiceItem item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (InvoiceItem item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Gdy zmieni siê w³aœciwoœæ w pozycji, aktualizuj sumy
            if (e.PropertyName == nameof(InvoiceItem.TotalNet) ||
                e.PropertyName == nameof(InvoiceItem.VatAmount) ||
                e.PropertyName == nameof(InvoiceItem.TotalGross))
            {
                OnPropertyChanged(nameof(TotalNet));
                OnPropertyChanged(nameof(TotalVat));
                OnPropertyChanged(nameof(TotalAmount));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}