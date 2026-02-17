using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OrderApprovalSystem.Converters
{
    /// <summary>
    /// Converts tree node level to left margin for visual indentation.
    /// Each level adds 20 pixels of indentation, plus 18 pixels for the expander button area.
    /// </summary>
    public class LevelToIndentConverter : IValueConverter
    {
        private const double IndentSize = 20.0;
        private const double ExpanderOffset = 18.0;
        private const double RightMargin = 2.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int level)
            {
                // Add base offset for the expander button area, plus indentation per level
                double leftMargin = ExpanderOffset + (level * IndentSize);
                return new Thickness(leftMargin, 0, RightMargin, 0);
            }
            return new Thickness(ExpanderOffset, 0, RightMargin, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}