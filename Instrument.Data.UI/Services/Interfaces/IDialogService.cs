using System.Threading.Tasks;

namespace Instrument.Data.UI.Services.Interfaces
{
    /// <summary>
    /// Interface for a service that displays dialogs.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Shows a message box with the specified message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="title">The title of the dialog.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ShowMessageAsync(string message, string title = "Information");

        /// <summary>
        /// Shows a confirmation dialog with the specified message.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="title">The title of the dialog.</param>
        /// <returns>True if the user confirmed; otherwise, false.</returns>
        Task<bool> ShowConfirmationAsync(string message, string title = "Confirmation");

        /// <summary>
        /// Shows a dialog that allows the user to input text.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="defaultValue">The default value for the input.</param>
        /// <returns>The text entered by the user, or null if the dialog was canceled.</returns>
        Task<string> ShowInputDialogAsync(string message, string title = "Input", string defaultValue = "");

        /// <summary>
        /// Shows a custom dialog.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the dialog.</typeparam>
        /// <typeparam name="TViewModel">The type of the ViewModel for the dialog.</typeparam>
        /// <param name="parameter">Optional parameter to pass to the dialog.</param>
        /// <returns>The result of the dialog.</returns>
        Task<TResult> ShowCustomDialogAsync<TResult, TViewModel>(object parameter = null);
    }
}
