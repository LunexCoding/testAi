using System;
using System.Globalization;
using System.Windows.Data;

namespace OrderApprovalSystem.Converters
{
    public class IsByMemoToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isByMemo)
            {
                return isByMemo ? "СЗ" : "Тех. заказ";
            }
            return "Тех. заказ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}