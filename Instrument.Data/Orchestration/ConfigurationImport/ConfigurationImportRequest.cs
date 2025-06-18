namespace Instrument.Data.Orchestration.ConfigurationImport;
using System.Collections.Generic;

/// <summary>
/// Request for configuration import process
/// </summary>
public class ConfigurationImportRequest
{
    public bool IncludeSequences { get; set; } = true;
    public bool ClearExistingData { get; set; }
    public List<string> SequenceFilters { get; set; } = [];
    public List<string> ResourceFilters { get; set; } = [];
}
