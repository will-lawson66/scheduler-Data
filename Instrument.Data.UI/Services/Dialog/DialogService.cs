using System;
using System.Threading.Tasks;
using System.Windows;
using Instrument.Data.UI.Services.Interfaces;
using Instrument.Data.UI.Services.Navigation;

namespace Instrument.Data.UI.Services.Dialog
{
    /// <summary>
    /// Implements the dialog service for WPF applications.
    /// </summary>
    public class DialogService : IDialogService
    {
        private readonly INavigationService _navigationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogService"/> class.
        /// </summary>
        /// <param name="navigationService">The navigation service to use for custom dialogs.</param>
        public DialogService(INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        }

        /// <inheritdoc/>
        public Task ShowMessageAsync(string message, string title = "Information")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> ShowConfirmationAsync(string message, string title = "Confirmation")
        {
            var result = MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return Task.FromResult(result == MessageBoxResult.Yes);
        }

        /// <inheritdoc/>
        public async Task<string> ShowInputDialogAsync(string message, string title = "Input", string defaultValue = "")
        {
            // For a real implementation, this would use a custom dialog
            // This is a simple placeholder implementation
            var result = await ShowConfirmationAsync($"{message}\n\nUse default value: {defaultValue}?", title);
            return result ? defaultValue : null;
        }

        /// <inheritdoc/>
        public async Task<TResult> ShowCustomDialogAsync<TResult, TViewModel>(object parameter = null)
        {
            var result = await _navigationService.ShowDialogAsync<TViewModel>(parameter);
            
            if (result is TResult typedResult)
            {
                return typedResult;
            }

            return default;
        }
    }
}
