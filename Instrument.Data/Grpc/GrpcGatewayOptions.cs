namespace Instrument.Data.Grpc;
public class GrpcGatewayOptions
{
    public int DefaultTimeoutSeconds { get; set; } = 30;
    public int MaxConcurrentRequests { get; set; } = 10;
    public RetryOptions RetryOptions { get; set; } = new();
}

public class RetryOptions
{
    public int MaxAttempts { get; set; } = 3;
    public int BaseDelayMs { get; set; } = 1000;
    public double BackoffMultiplier { get; set; } = 2.0;
    public bool UseJitter { get; set; } = true;
}
