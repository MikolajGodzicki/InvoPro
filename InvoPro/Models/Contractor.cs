using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InvoPro.Models
{
    public class Contractor : INotifyPropertyChanged
    {
        private int _id;
        private string _name = string.Empty;
        private string? _nip;
        private string? _address;
        private string? _regon;
        private string? _gln;

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

        public string? Nip
        {
            get => _nip;
            set => SetProperty(ref _nip, value);
        }

        public string? Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        public string? Regon
        {
            get => _regon;
            set => SetProperty(ref _regon, value);
        }

        public string? Gln
        {
            get => _gln;
            set => SetProperty(ref _gln, value);
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
