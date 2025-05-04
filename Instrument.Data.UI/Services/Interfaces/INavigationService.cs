using System.Threading.Tasks;

namespace Instrument.Data.UI.Services.Interfaces
{
    /// <summary>
    /// Interface for navigation service.
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Navigates to a view associated with the specified ViewModel.
        /// </summary>
        /// <typeparam name="TViewModel">The type of the ViewModel to navigate to.</typeparam>
        /// <param name="parameter">Optional parameter to pass to the ViewModel.</param>
        void NavigateTo<TViewModel>(object parameter = null);

        /// <summary>
        /// Navigates back to the previous view.
        /// </summary>
        /// <returns>True if navigation was successful; otherwise, false.</returns>
        bool GoBack();

        /// <summary>
        /// Shows a dialog associated with the specified ViewModel.
        /// </summary>
        /// <typeparam name="TViewModel">The type of the ViewModel for the dialog.</typeparam>
        /// <param name="parameter">Optional parameter to pass to the ViewModel.</param>
        /// <returns>The result of the dialog.</returns>
        Task<object> ShowDialogAsync<TViewModel>(object parameter = null);
    }
}
