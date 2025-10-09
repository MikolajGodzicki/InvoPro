using System.ComponentModel;
using System.Windows;
using InvoPro.ViewModels;

namespace InvoPro.Views
{
    /// <summary>
    /// Interaction logic for InvoiceEditWindow.xaml
    /// </summary>
    public partial class InvoiceEditWindow : Window
    {
        public InvoiceEditWindow()
        {
            InitializeComponent();
        }

        public InvoiceEditWindow(InvoiceEditViewModel viewModel) : this()
        {
            DataContext = viewModel;
            
            // Nasluchuj zmiany DialogResult w ViewModelu
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InvoiceEditViewModel.DialogResult))
            {
                var viewModel = (InvoiceEditViewModel)sender!;
                DialogResult = viewModel.DialogResult;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Jesli okno jest zamykane bez ustawienia DialogResult, ustaw na false (Anuluj)
            if (DialogResult == null)
            {
                DialogResult = false;
            }
            
            base.OnClosing(e);
        }
    }
}