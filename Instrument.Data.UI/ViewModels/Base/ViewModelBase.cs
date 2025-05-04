using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Instrument.Data.UI.ViewModels.Base
{
    /// <summary>
    /// Base class for all ViewModels providing common functionality.
    /// </summary>
    public abstract class ViewModelBase : ObservableObject
    {
        private bool _isBusy;
        private string _title;
        private string _errorMessage;
        private bool _hasError;

        /// <summary>
        /// Gets or sets a value indicating whether the ViewModel is currently busy.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// Gets or sets the title of the view.
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                HasError = !string.IsNullOrEmpty(value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the ViewModel has an error.
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        /// <summary>
        /// Clears the error message.
        /// </summary>
        protected void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }
    }
}
