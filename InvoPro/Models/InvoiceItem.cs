using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InvoPro.Models
{
    public class InvoiceItem : INotifyPropertyChanged
    {
        private int _id;
        private string _name = string.Empty;
        private string _description = string.Empty;
        private decimal _quantity = 1;
        private string _unit = "szt.";
        private decimal _unitPriceNet;
        private decimal _discountPercentage;
        private decimal _vatRate = 23; // domyœlnie 23% VAT

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    OnPropertyChanged(nameof(TotalNet));
                    OnPropertyChanged(nameof(TotalGross));
                    OnPropertyChanged(nameof(VatAmount));
                }
            }
        }

        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        public decimal UnitPriceNet
        {
            get => _unitPriceNet;
            set
            {
                if (SetProperty(ref _unitPriceNet, value))
                {
                    OnPropertyChanged(nameof(UnitPriceGross));
                    OnPropertyChanged(nameof(TotalNet));
                    OnPropertyChanged(nameof(TotalGross));
                    OnPropertyChanged(nameof(VatAmount));
                }
            }
        }

        public decimal DiscountPercentage
        {
            get => _discountPercentage;
            set
            {
                if (SetProperty(ref _discountPercentage, value))
                {
                    OnPropertyChanged(nameof(TotalNet));
                    OnPropertyChanged(nameof(TotalGross));
                    OnPropertyChanged(nameof(VatAmount));
                }
            }
        }

        public decimal VatRate
        {
            get => _vatRate;
            set
            {
                if (SetProperty(ref _vatRate, value))
                {
                    OnPropertyChanged(nameof(UnitPriceGross));
                    OnPropertyChanged(nameof(TotalNet));
                    OnPropertyChanged(nameof(TotalGross));
                    OnPropertyChanged(nameof(VatAmount));
                }
            }
        }

        // Obliczane w³aœciwoœci
        public decimal UnitPriceGross 
        { 
            get => UnitPriceNet * (1 + VatRate / 100);
            set
            {
                // Oblicz cenê netto na podstawie ceny brutto
                var newNetPrice = value / (1 + VatRate / 100);
                UnitPriceNet = Math.Round(newNetPrice, 2);
            }
        }

        public decimal TotalNet
        {
            get
            {
                var netBeforeDiscount = Quantity * UnitPriceNet;
                return Math.Round(netBeforeDiscount * (1 - DiscountPercentage / 100), 2);
            }
        }

        public decimal VatAmount => Math.Round(TotalNet * (VatRate / 100), 2);

        public decimal TotalGross => Math.Round(TotalNet + VatAmount, 2);

        // Formatowanie w PLN
        public string UnitPriceNetFormatted => $"{UnitPriceNet:F2} PLN";
        public string UnitPriceGrossFormatted => $"{UnitPriceGross:F2} PLN";
        public string TotalNetFormatted => $"{TotalNet:F2} PLN";
        public string TotalGrossFormatted => $"{TotalGross:F2} PLN";
        public string VatAmountFormatted => $"{VatAmount:F2} PLN";

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