using Avalonia.Data.Converters;
using Avalonia;
using System;
using System.Globalization;

namespace Instrument.Data.Avalonia.Converters
{
    /// <summary>
    /// Converts a Boolean value to a Visibility value, optionally inverting the logic.
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets a value indicating whether the converter should invert the input value.
        /// </summary>
        public bool Invert { get; set; }

        /// <summary>
        /// Converts a Boolean value to a Visibility value.
        /// </summary>
        /// <param name="value">The Boolean value to convert.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">Optional parameter that, if provided and equal to "Invert", will invert the logic.</param>
        /// <param name="culture">The culture information.</param>
        /// <returns>A Visibility value based on the Boolean input.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool shouldInvert = Invert;
            
            // Check if parameter is provided and is "Invert"
            if (parameter is string paramString && paramString.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            {
                shouldInvert = !shouldInvert;
            }
            
            bool boolValue = value is bool b ? b : false;
            
            if (shouldInvert)
            {
                boolValue = !boolValue;
            }
            
            return boolValue ? Avalonia.Controls.Avalonia.VisualTree.IsVisible : Avalonia.Controls.Avalonia.VisualTree.IsCollapsed;
        }

        /// <summary>
        /// Converts a Visibility value back to a Boolean value.
        /// </summary>
        /// <param name="value">The Visibility value to convert.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">Optional parameter that, if provided and equal to "Invert", will invert the logic.</param>
        /// <param name="culture">The culture information.</param>
        /// <returns>A Boolean value based on the Visibility input.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool shouldInvert = Invert;
            
            // Check if parameter is provided and is "Invert"
            if (parameter is string paramString && paramString.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            {
                shouldInvert = !shouldInvert;
            }
            
            bool result = value is bool visibility && visibility == Avalonia.Controls.Avalonia.VisualTree.IsVisible;
            
            if (shouldInvert)
            {
                result = !result;
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// Converter that inverts a Boolean value and converts it to a Visibility value.
    /// </summary>
    public class InverseBooleanToVisibilityConverter : BooleanToVisibilityConverter
    {
        /// <summary>
        /// Initializes a new instance of the InverseBooleanToVisibilityConverter class.
        /// </summary>
        public InverseBooleanToVisibilityConverter()
        {
            Invert = true;
        }
    }
}
