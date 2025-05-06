using System.Threading.Tasks;

namespace Instrument.Data.Avalonia.Services
{
    public interface IDialogService
    {
        Task<bool> ShowConfirmationAsync(string title, string message);
        Task ShowInformationAsync(string title, string message);
        Task ShowWarningAsync(string title, string message);
        Task ShowErrorAsync(string title, string message);
        Task<string> ShowOpenFileDialogAsync(string title, string filter);
        Task<string> ShowSaveFileDialogAsync(string title, string filter, string defaultFileName);
    }
}
