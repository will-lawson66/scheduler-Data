namespace Instrument.Data.Grpc;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents a retry policy
/// </summary>
public interface IRetryPolicy
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}
