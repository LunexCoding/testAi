using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace OrderApprovalSystem.Converters
{

    public class MaskedDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date && date != DateTime.MinValue && date != DateTime.MaxValue)
            {
                return date.ToString("dd.MM.yyyy");
            }
            return string.Empty; // Лучше возвращать пустую строку вместо маски
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string input)
            {
                // Если строка пустая - возвращаем null (если свойство nullable) или MinValue
                if (string.IsNullOrWhiteSpace(input))
                    return DependencyProperty.UnsetValue;

                // Удаляем все пробелы
                string cleaned = input.Trim();

                // Если пользователь ввел дату без точек, добавляем их автоматически
                if (cleaned.All(c => char.IsDigit(c) || c == '.'))
                {
                    // Убираем лишние точки
                    while (cleaned.Contains(".."))
                        cleaned = cleaned.Replace("..", ".");

                    // Удаляем точку в начале или конце
                    cleaned = cleaned.Trim('.');

                    // Если строка состоит только из цифр
                    string digitsOnly = new string(cleaned.Where(char.IsDigit).ToArray());

                    if (digitsOnly.Length >= 1 && digitsOnly.Length <= 8)
                    {
                        try
                        {
                            // Автоматически форматируем дату из цифр
                            string formatted = FormatDateString(digitsOnly);

                            if (DateTime.TryParseExact(formatted, "dd.MM.yyyy",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                            {
                                return result;
                            }

                            // Пробуем парсить как есть (если пользователь уже ввел с точками)
                            if (DateTime.TryParse(cleaned, culture, DateTimeStyles.None, out result))
                            {
                                return result;
                            }
                        }
                        catch
                        {
                            return DependencyProperty.UnsetValue;
                        }
                    }
                }
                else
                {
                    // Пробуем парсить обычную дату
                    if (DateTime.TryParse(cleaned, culture, DateTimeStyles.None, out DateTime result))
                    {
                        return result;
                    }
                }
            }

            return DependencyProperty.UnsetValue;
        }

        private string FormatDateString(string digits)
        {
            // Добавляем ведущие нули если нужно
            string padded = digits.PadLeft(8, '0');

            // Берем только первые 8 символов
            if (padded.Length > 8)
                padded = padded.Substring(0, 8);

            // Форматируем как dd.MM.yyyy
            return $"{padded.Substring(0, 2)}.{padded.Substring(2, 2)}.{padded.Substring(4, 4)}";
        }
    }

}
