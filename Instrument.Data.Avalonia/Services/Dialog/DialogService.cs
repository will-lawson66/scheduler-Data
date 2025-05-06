using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Material.Styles.Controls;
using Material.Styles.Dialogs;
using Material.Styles.Dialogs.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Instrument.Data.Avalonia.Services.Dialog
{
    public class DialogService : IDialogService
    {
        private readonly ILogger<DialogService> _logger;
        
        public DialogService(ILogger<DialogService> logger)
        {
            _logger = logger;
        }
        
        public async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null) return false;
                
                var dialog = DialogHelper.CreateAlertDialog(new AlertDialogBuilderParams
                {
                    ContentHeader = title,
                    SupportingText = message,
                    StartupLocation = WindowStartupLocation.CenterOwner,
                    Width = 400,
                    NegativeButton = "Cancel",
                    PositiveButton = "OK",
                });
                
                var result = await dialog.ShowDialog(mainWindow);
                return result == "OK";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing confirmation dialog");
                return false;
            }
        }
        
        public async Task ShowInformationAsync(string title, string message)
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null) return;
                
                var dialog = DialogHelper.CreateAlertDialog(new AlertDialogBuilderParams
                {
                    ContentHeader = title,
                    SupportingText = message,
                    StartupLocation = WindowStartupLocation.CenterOwner,
                    Width = 400,
                    PositiveButton = "OK",
                });
                
                await dialog.ShowDialog(mainWindow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing information dialog");
            }
        }
        
        public async Task ShowWarningAsync(string title, string message)
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null) return;
                
                var dialog = DialogHelper.CreateAlertDialog(new AlertDialogBuilderParams
                {
                    ContentHeader = title,
                    SupportingText = message,
                    StartupLocation = WindowStartupLocation.CenterOwner,
                    Width = 400,
                    PositiveButton = "OK",
                });
                
                await dialog.ShowDialog(mainWindow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing warning dialog");
            }
        }
        
        public async Task ShowErrorAsync(string title, string message)
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null) return;
                
                var dialog = DialogHelper.CreateAlertDialog(new AlertDialogBuilderParams
                {
                    ContentHeader = title,
                    SupportingText = message,
                    StartupLocation = WindowStartupLocation.CenterOwner,
                    Width = 400,
                    PositiveButton = "OK",
                });
                
                await dialog.ShowDialog(mainWindow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing error dialog");
            }
        }
        
        public async Task<string> ShowOpenFileDialogAsync(string title, string filter)
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null) return string.Empty;
                
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
        
        public async Task<string> ShowSaveFileDialogAsync(string title, string filter, string defaultFileName)
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null) return string.Empty;
                
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
        
        private Window GetMainWindow()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }
            
            return null;
        }
    }
}
