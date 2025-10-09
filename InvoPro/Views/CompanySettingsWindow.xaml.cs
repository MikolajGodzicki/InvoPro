using System.ComponentModel;
using System.Windows;
using InvoPro.ViewModels;

namespace InvoPro.Views
{
    /// <summary>
    /// Interaction logic for CompanySettingsWindow.xaml
    /// </summary>
    public partial class CompanySettingsWindow : Window
    {
        public CompanySettingsWindow()
        {
            InitializeComponent();
            DataContext = new CompanySettingsViewModel();
            
            // Nas³uchuj zmiany DialogResult w ViewModelu
            if (DataContext is CompanySettingsViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CompanySettingsViewModel.DialogResult))
            {
                var viewModel = (CompanySettingsViewModel)sender!;
                DialogResult = viewModel.DialogResult;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Jeœli okno jest zamykane bez ustawienia DialogResult, ustaw na false (Anuluj)
            if (DialogResult == null)
            {
                DialogResult = false;
            }
            
            base.OnClosing(e);
        }
    }
}