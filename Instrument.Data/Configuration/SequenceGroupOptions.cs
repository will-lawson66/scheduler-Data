using Instrument.Data.DTOs;

namespace Instrument.Data.Configuration;

public class SequenceGroupOptions
{
    public const string SectionName = "SequenceGroups";
    
    public List<SequenceGroupDTO> Groups { get; set; } = [];
    
    // Alternative: Single default group
    public SequenceGroupDTO? DefaultGroup { get; set; }
    
    // Configuration metadata
    public bool EnableCaching { get; set; } = true;
    public int CacheExpirationMinutes { get; set; } = 30;
}