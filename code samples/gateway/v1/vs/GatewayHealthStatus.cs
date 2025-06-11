namespace Instrument.Scheduling.Data.Grpc;
using System;
using System.Collections.Generic;

/// <summary>
/// Overall gateway health status
/// </summary>
public class GatewayHealthStatus
{
    public DateTime Timestamp { get; set; }
    public string OverallStatus { get; set; }
    public Dictionary<string, ServiceHealthStatus> ServiceStatuses { get; set; } = new();
    public bool CacheEnabled { get; set; }
    public int AvailableConnections { get; set; }
    public int MaxConcurrentConnections { get; set; }
}
