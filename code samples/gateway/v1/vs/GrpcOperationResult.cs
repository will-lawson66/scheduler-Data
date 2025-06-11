namespace Instrument.Scheduling.Data.Grpc;

using System;

/// <summary>
/// Result wrapper for gRPC operations
/// </summary>
public class GrpcOperationResult<TResult> where TResult : class
{
    public bool Success { get; set; }
    public required TResult Data { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public bool FromCache { get; set; }
    public string? ServiceName { get; set; }
    public string? OperationId { get; set; }
}
