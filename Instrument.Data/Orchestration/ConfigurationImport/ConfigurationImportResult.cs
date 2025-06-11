namespace Instrument.Data.Orchestration.ConfigurationImport;
using System;
using System.Collections.Generic;

/// <summary>
/// Result of configuration import process
/// </summary>
public class ConfigurationImportResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
    public ImportStatistics Statistics { get; set; } = new();
    public List<string> ProcessedSteps { get; set; } = [];
    public string? RequestId { get; set; }
}
