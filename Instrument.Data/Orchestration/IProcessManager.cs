namespace Instrument.Data.Orchestration;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Interface defining the Process Manager 
/// </summary>
public interface IProcessManager<in TRequest, TResult>
{
    Task<TResult> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}
