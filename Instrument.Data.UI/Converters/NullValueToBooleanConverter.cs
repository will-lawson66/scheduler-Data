using System;
using System.Globalization;
using System.Windows.Data;

namespace Instrument.Data.UI.Converters;
/// <summary>
/// Converts a null value to a boolean value.
/// </summary>
public class NullValueToBooleanConverter : IValueConverter
{
    /// <summary>
    /// Converts a null value to a boolean value.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>True if the value is not null; otherwise, false.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null;
    }

    /// <summary>
    /// Converting back is not supported.
    /// </summary>
    /// <exception cref="NotImplementedException">This operation is not supported.</exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}