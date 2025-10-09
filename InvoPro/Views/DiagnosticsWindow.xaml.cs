using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace InvoPro.Views
{
    /// <summary>
    /// Interaction logic for DiagnosticsWindow.xaml
    /// </summary>
    public partial class DiagnosticsWindow : Window
    {
        public DiagnosticsWindow()
        {
            InitializeComponent();
            RunDiagnostics();
        }

        private async void RunDiagnostics()
        {
            try
            {
                LogTextBox.Text = "Rozpoczynanie diagnostyki...\n";
                
                using var context = new InvoPro.Data.InvoiceDbContext();
                
                // SprawdŸ œcie¿kê bazy danych
                var dbPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InvoPro", "invoices.db");
                LogTextBox.Text += $"Œcie¿ka bazy: {dbPath}\n";
                LogTextBox.Text += $"Plik istnieje: {System.IO.File.Exists(dbPath)}\n";
                
                // SprawdŸ po³¹czenie
                var canConnect = await context.Database.CanConnectAsync();
                LogTextBox.Text += $"Mo¿na po³¹czyæ: {canConnect}\n";
                
                if (!canConnect)
                {
                    LogTextBox.Text += "Tworzenie bazy danych...\n";
                    await context.Database.EnsureCreatedAsync();
                    LogTextBox.Text += "Baza utworzona.\n";
                }
                
                // SprawdŸ tabele
                var invoiceCount = await context.Invoices.CountAsync();
                LogTextBox.Text += $"Liczba faktur: {invoiceCount}\n";
                
                var companyCount = await context.CompanyInfo.CountAsync();
                LogTextBox.Text += $"Liczba firm: {companyCount}\n";
                
                LogTextBox.Text += "Diagnostyka zakoñczona.\n";
            }
            catch (Exception ex)
            {
                LogTextBox.Text += $"B£¥D: {ex.Message}\n";
                LogTextBox.Text += $"Szczegó³y: {ex}\n";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RunDiagnostics();
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Czy na pewno chcesz zresetowaæ bazê danych?\n\nTO USUNIE WSZYSTKIE DANE!", 
                "Potwierdzenie resetowania", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    LogTextBox.Text += "\nResetowanie bazy danych...\n";
                    
                    var invoiceService = new InvoPro.Services.InvoiceService();
                    await invoiceService.ResetDatabaseAsync();
                    
                    LogTextBox.Text += "Baza danych zosta³a zresetowana pomyœlnie.\n";
                    
                    // Odœwie¿ diagnostykê
                    RunDiagnostics();
                }
                catch (Exception ex)
                {
                    LogTextBox.Text += $"B£¥D RESETOWANIA: {ex.Message}\n";
                }
            }
        }
    }
}