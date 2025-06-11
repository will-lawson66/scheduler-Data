namespace Instrument.Scheduling.Data.Grpc;
using System;
using System.Collections.Generic;

/// <summary>
/// Result wrapper for batch operations
/// </summary>
public class BatchGrpcOperationResult<TResult> where TResult : class
{
    public string? BatchId { get; set; }
    public string? ServiceName { get; set; }
    public string? OperationId { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public List<GrpcOperationResult<TResult>> Results { get; set; } = new List<GrpcOperationResult<TResult>>();
    public TimeSpan TotalExecutionTime { get; set; }
}
