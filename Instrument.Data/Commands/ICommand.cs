namespace Instrument.Data.Commands;

/// <summary>
/// Interface for command line commands
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Executes the command
    /// </summary>
    /// <param name="args">Command arguments</param>
    Task ExecuteAsync(string[] args);
}
