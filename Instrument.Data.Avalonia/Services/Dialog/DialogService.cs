using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Material.Dialog;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;

namespace Instrument.Data.Avalonia.Services.Dialog
{
    /// <summary>
    /// Implementation of IDialogService that uses Material.Avalonia dialogs and ReactiveUI interactions
    /// </summary>
    public class DialogService : ReactiveObject, IDialogService
    {
        private readonly ILogger<DialogService> _logger;
        
        // ReactiveUI interactions for showing dialogs
        public Interaction<MessageBoxParams, bool> ShowConfirmationInteraction { get; }
        public Interaction<MessageBoxParams, Unit> ShowInformationInteraction { get; }
        public Interaction<MessageBoxParams, Unit> ShowWarningInteraction { get; }
        public Interaction<MessageBoxParams, Unit> ShowErrorInteraction { get; }
        
        public DialogService(ILogger<DialogService> logger)
        {
            _logger = logger;
            
            // Initialize interactions
            ShowConfirmationInteraction = new Interaction<MessageBoxParams, bool>();
            ShowInformationInteraction = new Interaction<MessageBoxParams, Unit>();
            ShowWarningInteraction = new Interaction<MessageBoxParams, Unit>();
            ShowErrorInteraction = new Interaction<MessageBoxParams, Unit>();
        }
        
        /// <summary>
        /// Shows a confirmation dialog with OK and Cancel buttons
        /// </summary>
        public async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null) return false;
                
                // First try ReactiveUI interaction if registered handlers exist
                try
                {
                    var result = await ShowConfirmationInteraction.Handle(new MessageBoxParams
                    {
                        Title = title,
                        Message = message
                    });
                    
                    return result;
                }
                catch (UnhandledInteractionException)
                {
                    // Fall back to Material.Avalonia dialog if no handlers registered
                    var dialog = DialogHelper.CreateAlertDialog(new AlertDialogBuilderParams
                    {
                        ContentHeader = title,
                        SupportingText = message,
                        StartupLocation = WindowStartupLocation.CenterOwner,
                        Width = 400,
                        //NegativeButton = "Cancel",
                        //PositiveButton = "OK",
                    });
                    
                    var result = await dialog.ShowDialog(mainWindow);
                    return result == "OK";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing confirmation dialog");
                return false;
            }
        }
        
        /// <summary>
        /// Shows an information dialog with an OK button
        /// </summary>
        public async Task ShowInformationAsync(string title, string message)
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null) return;
                
                // First try ReactiveUI interaction if registered handlers exist
                try
                {
                    await ShowInformationInteraction.Handle(new MessageBoxParams
                    {
                        Title = title,
                        Message = message
                    });
                }
                catch (UnhandledInteractionException)
                {
                    // Fall back to Material.Avalonia dialog if no handlers registered
                    var dialog = DialogHelper.CreateAlertDialog(new AlertDialogBuilderParams
                    {
                        ContentHeader = title,
                        SupportingText = message,
                        StartupLocation = WindowStartupLocation.CenterOwner,
                        Width = 400,
                        PositiveButton = "OK",
                        DialogIcon = Material.Icons.MaterialIconKind.Information
                    });
                    
                    await dialog.ShowDialog(mainWindow);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing information dialog");
            }
        }
        
        /// <summary>
        /// Shows a warning dialog with an OK button
        /// </summary>
        public async Task ShowWarningAsync(string title, string message)
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null) return;
                
                // First try ReactiveUI interaction if registered handlers exist
                try
                {
                    await ShowWarningInteraction.Handle(new MessageBoxParams
                    {
                        Title = title,
                        Message = message
                    });
                }
                catch (UnhandledInteractionException)
                {
                    // Fall back to Material.Avalonia dialog if no handlers registered
                    var dialog = DialogHelper.CreateAlertDialog(new AlertDialogBuilderParams
                    {
                        ContentHeader = title,
                        SupportingText = message,
                        StartupLocation = WindowStartupLocation.CenterOwner,
                        Width = 400,
                        PositiveButton = "OK",
                        DialogIcon = Material.Icons.MaterialIconKind.AlertCircle
                    });
                    
                    await dialog.ShowDialog(mainWindow);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing warning dialog");
            }
        }
        
        /// <summary>
        /// Shows an error dialog with an OK button
        /// </summary>
        public async Task ShowErrorAsync(string title, string message)
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null) return;
                
                // First try ReactiveUI interaction if registered handlers exist
                try
                {
                    await ShowErrorInteraction.Handle(new MessageBoxParams
                    {
                        Title = title,
                        Message = message
                    });
                }
                catch (UnhandledInteractionException)
                {
                    // Fall back to Material.Avalonia dialog if no handlers registered
                    var dialog = DialogHelper.CreateAlertDialog(new AlertDialogBuilderParams
                    {
                        ContentHeader = title,
                        SupportingText = message,
                        StartupLocation = WindowStartupLocation.CenterOwner,
                        Width = 400,
                        PositiveButton = "OK",
                        DialogIcon = Material.Icons.MaterialIconKind.AlertOctagon
                    });
                    
                    await dialog.ShowDialog(mainWindow);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing error dialog");
            }
        }
        
        /// <summary>
        /// Shows an open file dialog
        /// </summary>
        public async Task<string> ShowOpenFileDialogAsync(string title, string filter)
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null) return string.Empty;
                
                var filters = ParseFileFilters(filter);
                
                var options = new FilePickerOpenOptions
                {
                    Title = title,
                    FileTypeFilter = filters,
                    AllowMultiple = false
                };
                
                var result = await mainWindow.StorageProvider.OpenFilePickerAsync(options);
                
                return result.Count > 0 ? result[0].Path.LocalPath : string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing open file dialog");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Shows a save file dialog
        /// </summary>
        public async Task<string> ShowSaveFileDialogAsync(string title, string filter, string defaultFileName)
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null) return string.Empty;
                
                var filters = ParseFileFilters(filter);
                
                var options = new FilePickerSaveOptions
                {
                    Title = title,
                    FileTypeChoices = filters,
                    SuggestedFileName = defaultFileName
                };
                
                var result = await mainWindow.StorageProvider.SaveFilePickerAsync(options);
                
                return result?.Path.LocalPath ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing save file dialog");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Parses filter string in the format "Name|*.ext|Name2|*.ext2"
        /// </summary>
        private List<FilePickerFileType> ParseFileFilters(string filter)
        {
            var filters = new List<FilePickerFileType>();
            
            // Parse filter string (e.g. "CSV files|*.csv|All files|*.*")
            var filterParts = filter.Split('|');
            for (int i = 0; i < filterParts.Length; i += 2)
            {
                if (i + 1 < filterParts.Length)
                {
                    var name = filterParts[i];
                    var extensions = filterParts[i + 1].Split(';')
                        .Select(e => e.Trim().TrimStart('*'))
                        .ToArray();
                        
                    filters.Add(new FilePickerFileType(name) { Patterns = extensions });
                }
            }
            
            return filters;
        }
        
        /// <summary>
        /// Gets the main window of the application
        /// </summary>
        private Window GetMainWindow()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }
            
            return null;
        }
    }
    
    /// <summary>
    /// Parameters for message box interactions
    /// </summary>
    public class MessageBoxParams
    {
        public string Title { get; set; }
        public string Message { get; set; }
    }
}
