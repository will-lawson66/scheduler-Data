namespace Instrument.Scheduling.Data.Grpc;

using System;
using System.Collections.Generic;

/// <summary>
/// Batch operation for multiple requests of the same type
/// </summary>
public class BatchGrpcOperation<TRequest, TResult>
    where TRequest : class
    where TResult : class
{
    /// <summary>
    /// The operation template to execute
    /// </summary>
    public required GrpcOperation<TRequest, TResult> Operation { get; set; }

    /// <summary>
    /// List of requests to process
    /// </summary>
    public required IEnumerable<TRequest> Requests { get; set; }

    /// <summary>
    /// Maximum concurrent executions
    /// </summary>
    public int MaxConcurrency { get; set; } = 5;

    /// <summary>
    /// Whether to continue processing if one request fails
    /// </summary>
    public bool ContinueOnError { get; set; } = true;

    /// <summary>
    /// Batch identifier for tracking
    /// </summary>
    public string BatchId { get; set; } = Guid.NewGuid().ToString();
}
