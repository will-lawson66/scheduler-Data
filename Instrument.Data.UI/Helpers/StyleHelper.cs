using System;
using System.Windows;

namespace Instrument.Data.UI.Helpers
{
    /// <summary>
    /// Helper class for style and resource management
    /// </summary>
    public static class StyleHelper
    {
        /// <summary>
        /// Tries to find a style resource, and returns a fallback if not found
        /// </summary>
        /// <param name="styleName">Name of the style to find</param>
        /// <param name="fallbackStyleName">Name of the fallback style</param>
        /// <returns>The requested style, or the fallback style if not found</returns>
        public static Style GetStyleWithFallback(string styleName, string fallbackStyleName)
        {
            var style = Application.Current.TryFindResource(styleName) as Style;
            if (style == null)
            {
                // Log that we couldn't find the style
                Console.WriteLine($"Warning: Style '{styleName}' not found, using fallback style '{fallbackStyleName}'");
                style = Application.Current.TryFindResource(fallbackStyleName) as Style;
            }
            return style;
        }

        /// <summary>
        /// Checks if a style exists in the application resources
        /// </summary>
        /// <param name="styleName">Name of the style to check</param>
        /// <returns>True if the style exists, false otherwise</returns>
        public static bool StyleExists(string styleName)
        {
            return Application.Current.TryFindResource(styleName) is Style;
        }
        
        /// <summary>
        /// Gets a resource with a fallback
        /// </summary>
        /// <param name="resourceKey">Key of the resource to find</param>
        /// <param name="fallbackResourceKey">Key of the fallback resource</param>
        /// <returns>The requested resource, or the fallback if not found</returns>
        public static object GetResourceWithFallback(string resourceKey, string fallbackResourceKey)
        {
            var resource = Application.Current.TryFindResource(resourceKey);
            if (resource == null)
            {
                Console.WriteLine($"Warning: Resource '{resourceKey}' not found, using fallback resource '{fallbackResourceKey}'");
                resource = Application.Current.TryFindResource(fallbackResourceKey);
            }
            return resource;
        }
        
        /// <summary>
        /// Checks if a resource exists in the application resources
        /// </summary>
        /// <param name="resourceKey">Key of the resource to check</param>
        /// <returns>True if the resource exists, false otherwise</returns>
        public static bool ResourceExists(string resourceKey)
        {
            return Application.Current.TryFindResource(resourceKey) != null;
        }
    }
}
