using System.Configuration;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace InvoPro
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Ustaw polską kulturę dla poprawnego formatowania kwot i dat
            var polishCulture = new CultureInfo("pl-PL");
            Thread.CurrentThread.CurrentCulture = polishCulture;
            Thread.CurrentThread.CurrentUICulture = polishCulture;
            
            base.OnStartup(e);
        }
    }
}
