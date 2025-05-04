using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace Instrument.Data.UI.Helpers
{
    /// <summary>
    /// Validates that styles referenced in the application exist at runtime
    /// </summary>
    public static class StyleValidation
    {
        private static readonly Dictionary<string, bool> _validatedStyles = new Dictionary<string, bool>();
        private static readonly Dictionary<string, string> _fallbackMap = new Dictionary<string, string>
        {
            // Map styles to their fallbacks
            { "HeadingLarge", "MaterialDesignHeadline5TextBlock" },
            { "HeadingMedium", "MaterialDesignHeadline6TextBlock" },
            { "HeadingSmall", "MaterialDesignSubtitle1TextBlock" },
            { "BodyText", "MaterialDesignBody1TextBlock" },
            { "SecondaryText", "MaterialDesignBody2TextBlock" },
            { "PrimaryButton", "MaterialDesignRaisedButton" },
            { "SecondaryButton", "MaterialDesignOutlinedButton" },
            { "FormComboBox", "MaterialDesignComboBox" }
        };

        /// <summary>
        /// Performs validation of all expected styles at application startup
        /// </summary>
        public static void ValidateStyles()
        {
            foreach (var styleEntry in _fallbackMap)
            {
                var styleExists = StyleHelper.StyleExists(styleEntry.Key);
                _validatedStyles[styleEntry.Key] = styleExists;
                
                if (!styleExists)
                {
                    var fallbackExists = StyleHelper.StyleExists(styleEntry.Value);
                    
                    if (!fallbackExists)
                    {
                        // Critical problem - neither style nor fallback exists
                        Debug.WriteLine($"WARNING: Neither style '{styleEntry.Key}' nor fallback '{styleEntry.Value}' exists!");
                    }
                    else
                    {
                        // Fallback exists, we can recover
                        Debug.WriteLine($"Style '{styleEntry.Key}' not found, will use fallback '{styleEntry.Value}'");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the appropriate style name based on validation results
        /// </summary>
        /// <param name="styleName">Original style name</param>
        /// <returns>Either the original style name or a fallback if the original doesn't exist</returns>
        public static string GetValidatedStyleName(string styleName)
        {
            if (_validatedStyles.TryGetValue(styleName, out bool exists) && !exists)
            {
                if (_fallbackMap.TryGetValue(styleName, out string fallback))
                {
                    return fallback;
                }
            }
            
            return styleName;
        }
    }
}
