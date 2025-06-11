namespace Instrument.Data.Grpc;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Thread-safe gateway statistics
/// </summary>
public class GatewayStatistics
{
    private readonly ConcurrentDictionary<string, long> _operationCounts = new();
    private readonly ConcurrentDictionary<string, long> _errorCounts = new();
    private long _totalRequests;
    private long _totalErrors;

    public long TotalRequests => _totalRequests;
    public long TotalErrors => _totalErrors;
    public IReadOnlyDictionary<string, long> OperationCounts => _operationCounts;
    public IReadOnlyDictionary<string, long> ErrorCounts => _errorCounts;

    public void IncrementOperation(string operationName)
    {
        _operationCounts.AddOrUpdate(operationName, 1, (key, value) => value + 1);
        Interlocked.Increment(ref _totalRequests);
    }

    public void IncrementError(string operationName)
    {
        _errorCounts.AddOrUpdate(operationName, 1, (key, value) => value + 1);
        Interlocked.Increment(ref _totalErrors);
    }
}
