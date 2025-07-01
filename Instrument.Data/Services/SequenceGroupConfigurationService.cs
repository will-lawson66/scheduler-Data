using Instrument.Data.Configuration;
using Instrument.Data.DTOs;
using Instrument.Data.Entities.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Instrument.Data.Services;

public interface ISequenceGroupConfigurationService
{
    /// <summary>
    /// Gets the default sequence group from configuration
    /// </summary>
    SequenceGroupDTO? GetDefaultGroup();
    
    /// <summary>
    /// Gets all configured sequence groups
    /// </summary>
    IEnumerable<SequenceGroupDTO> GetConfiguredGroups();
    
    /// <summary>
    /// Gets configured groups filtered by technology
    /// </summary>
    IEnumerable<SequenceGroupDTO> GetGroupsByTechnology(Technology technology);
    
    /// <summary>
    /// Gets a specific configured group by name
    /// </summary>
    SequenceGroupDTO? GetGroupByName(string name);
    
    /// <summary>
    /// Validates all configured sequence groups
    /// </summary>
    ValidationResult ValidateConfiguration();
}

public class SequenceGroupConfigurationService : ISequenceGroupConfigurationService
{
    private readonly SequenceGroupOptions _options;
    private readonly ILogger<SequenceGroupConfigurationService> _logger;

    public SequenceGroupConfigurationService(
        IOptions<SequenceGroupOptions> options,
        ILogger<SequenceGroupConfigurationService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public SequenceGroupDTO? GetDefaultGroup()
    {
        return _options.DefaultGroup;
    }

    public IEnumerable<SequenceGroupDTO> GetConfiguredGroups()
    {
        return _options.Groups;
    }

    public IEnumerable<SequenceGroupDTO> GetGroupsByTechnology(Technology technology)
    {
        return _options.Groups.Where(g => g.Technology == technology);
    }

    public SequenceGroupDTO? GetGroupByName(string name)
    {
        return _options.Groups.FirstOrDefault(g => 
            string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public ValidationResult ValidateConfiguration()
    {
        var result = new ValidationResult();
        
        // Validate default group
        if (_options.DefaultGroup != null)
        {
            ValidateGroup(_options.DefaultGroup, "DefaultGroup", result);
        }
        
        // Validate all configured groups
        for (int i = 0; i < _options.Groups.Count; i++)
        {
            ValidateGroup(_options.Groups[i], $"Groups[{i}]", result);
        }
        
        // Check for duplicate names
        var duplicateNames = _options.Groups
            .GroupBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
            
        foreach (var duplicateName in duplicateNames)
        {
            result.Errors.Add($"Duplicate sequence group name found: '{duplicateName}'");
        }
        
        if (result.IsValid)
        {
            _logger.LogInformation("SequenceGroup configuration validation successful. Found {GroupCount} groups", 
                _options.Groups.Count);
        }
        else
        {
            _logger.LogWarning("SequenceGroup configuration validation failed with {ErrorCount} errors", 
                result.Errors.Count);
        }
        
        return result;
    }

    private static void ValidateGroup(SequenceGroupDTO group, string path, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(group.Name))
        {
            result.Errors.Add($"{path}: Name is required");
        }
        
        if (!group.Sequences.Any())
        {
            result.Warnings.Add($"{path}: No sequences configured for group '{group.Name}'");
        }
        
        // Validate sequences
        var sequences = group.Sequences.ToList();
        for (int i = 0; i < sequences.Count; i++)
        {
            var sequence = sequences[i];
            var sequencePath = $"{path}.Sequences[{i}]";
            
            if (string.IsNullOrWhiteSpace(sequence.Name))
            {
                result.Errors.Add($"{sequencePath}: Sequence name is required");
            }
            
            if (sequence.WorstCaseTime <= TimeSpan.Zero)
            {
                result.Errors.Add($"{sequencePath}: WorstCaseTime must be greater than zero for sequence '{sequence.Name}'");
            }
            
            if (sequence.WorstCaseTime > TimeSpan.FromHours(24))
            {
                result.Warnings.Add($"{sequencePath}: WorstCaseTime is unusually long ({sequence.WorstCaseTime}) for sequence '{sequence.Name}'");
            }
        }
        
        // Check for duplicate sequence names within the group
        var duplicateSequenceNames = sequences
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
            
        foreach (var duplicateSequenceName in duplicateSequenceNames)
        {
            result.Errors.Add($"{path}: Duplicate sequence name '{duplicateSequenceName}' in group '{group.Name}'");
        }
    }
}

public class ValidationResult
{
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    
    public bool IsValid => !Errors.Any();
    
    public string Summary => IsValid 
        ? $"Validation successful. {Warnings.Count} warnings."
        : $"Validation failed. {Errors.Count} errors, {Warnings.Count} warnings.";
}