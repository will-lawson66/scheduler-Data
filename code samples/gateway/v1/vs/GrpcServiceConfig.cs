namespace Instrument.Scheduling.Data.Grpc;

using System.Collections.Generic;

/// <summary>
/// Configuration for a specific gRPC service
/// </summary>
public class GrpcServiceConfig
{
    /// <summary>
    /// Base address of the gRPC service
    /// </summary>
    public string? BaseAddress { get; set; }

    /// <summary>
    /// Service-specific overrides (null = use defaults)
    /// </summary>
    public bool? UseSecureConnection { get; set; }
    public int? TimeoutSeconds { get; set; }
    public int? MaxConnectionsPerService { get; set; }

    /// <summary>
    /// Load balancing endpoints (if multiple instances)
    /// </summary>
    public List<string> Endpoints { get; set; } = [];

    /// <summary>
    /// Service-specific retry policy overrides
    /// </summary>
    public RetryPolicyOptions RetryPolicy { get; set; } = new();

    /// <summary>
    /// Service-specific circuit breaker overrides
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
}
