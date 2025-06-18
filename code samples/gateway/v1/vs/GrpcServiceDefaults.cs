namespace Instrument.Scheduling.Data.Grpc;
/// <summary>
/// Default settings for gRPC services
/// </summary>
public class GrpcServiceDefaults
{
    public bool UseSecureConnection { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxConnectionsPerService { get; set; } = 5;
}
