namespace Instrument.Scheduling.Data.Grpc;

using System.Collections.Generic;

/// <summary>
/// Configuration for the gRPC Gateway supporting multiple services
/// </summary>
public class GrpcGatewayOptions
{
    public const string _sectionName = "GrpcGateway";

    /// <summary>
    /// Default settings applied to all services
    /// </summary>
    public GrpcServiceDefaults Defaults { get; set; } = new();

    /// <summary>
    /// Configuration for individual gRPC services
    /// Key = service name, Value = service configuration
    /// </summary>
    public Dictionary<string, GrpcServiceConfig> Services { get; set; } = [];

    /// <summary>
    /// Global gateway settings
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 20;
    public bool EnableCaching { get; set; }
    public int CacheExpiryMinutes { get; set; } = 15;
    public RetryPolicyOptions RetryPolicy { get; set; } = new();
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
}


