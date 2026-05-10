using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DocuMind.UI.Converters // <--- Standart Namespace
{
    public class RoleToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string role = value as string ?? string.Empty;
            string targetRole = parameter as string ?? string.Empty;
            if (role == targetRole) return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
