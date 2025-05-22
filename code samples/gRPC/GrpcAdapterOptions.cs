namespace Instrument.Data.Adapters;

/// <summary>
/// Configuration options for gRPC data adapter
/// </summary>
public class GrpcAdapterOptions
{
    /// <summary>
    /// Base address for gRPC services
    /// </summary>
    public string BaseAddress { get; set; } = "https://localhost:5001";
    
    /// <summary>
    /// Timeout for gRPC requests in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Whether to use secure connection (TLS/SSL)
    /// </summary>
    public bool UseSecureConnection { get; set; } = true;
    
    /// <summary>
    /// Whether to clear existing data before importing
    /// </summary>
    public bool ClearExistingDataBeforeImport { get; set; } = false;
    
    /// <summary>
    /// Maximum batch size for storing entities
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;
    
    /// <summary>
    /// Number of retry attempts for failed requests
    /// </summary>
    public int RetryCount { get; set; } = 3;
    
    /// <summary>
    /// Delay between retries in milliseconds
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;
}