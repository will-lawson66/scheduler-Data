namespace Instrument.Scheduling.Data.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Configuration options for gRPC gateway retry policy
/// </summary>
public class RetryPolicyOptions
{
    public int MaxRetries { get; set; } = 3;
    public int InitialDelayMs { get; set; } = 1000;
    public double BackoffMultiplier { get; set; } = 2.0;
    public int MaxDelayMs { get; set; } = 30000;
    public bool UseJitter { get; set; } = true;
}