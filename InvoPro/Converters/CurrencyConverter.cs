using System;
using System.Globalization;
using System.Windows.Data;

namespace InvoPro.Converters
{
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                return $"{decimalValue:F2} PLN";
            }
            
            if (value is double doubleValue)
            {
                return $"{doubleValue:F2} PLN";
            }
            
            if (value is float floatValue)
            {
                return $"{floatValue:F2} PLN";
            }
            
            return "0,00 PLN";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                var cleanValue = stringValue.Replace("PLN", "").Replace(" ", "").Trim();
                
                if (decimal.TryParse(cleanValue, NumberStyles.Currency, culture, out decimal result))
                {
                    return result;
                }
            }
            
            return 0m;
        }
    }
}