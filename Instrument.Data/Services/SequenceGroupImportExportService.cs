using System.Diagnostics;
using System.Text.Json;
using Instrument.Data.Configuration;
using Instrument.Data.DTOs;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.Services;

public class SequenceGroupImportExportService : ISequenceGroupImportExportService
{
    private readonly ISequenceGroupService _sequenceGroupService;
    private readonly ISequenceService _sequenceService;
    private readonly ILogger<SequenceGroupImportExportService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SequenceGroupImportExportService(
        ISequenceGroupService sequenceGroupService,
        ISequenceService sequenceService,
        ILogger<SequenceGroupImportExportService> logger)
    {
        _sequenceGroupService = sequenceGroupService;
        _sequenceService = sequenceService;
        _logger = logger;
        
        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters =
            {
                new TechnologyJsonConverter(),
                new TimeSpanJsonConverter()
            }
        };
    }

    public async Task<ImportResult> ImportSequenceGroupsFromJsonAsync(string jsonContent, bool replaceExisting = false)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ImportResult();
        
        try
        {
            _logger.LogInformation("Starting SequenceGroup import from JSON. ReplaceExisting: {ReplaceExisting}", replaceExisting);

            // Parse JSON content
            SequenceGroupDTO[]? sequenceGroupDtos;
            try
            {
                sequenceGroupDtos = JsonSerializer.Deserialize<SequenceGroupDTO[]>(jsonContent, _jsonOptions);
                if (sequenceGroupDtos == null)
                {
                    result.Errors.Add("Failed to deserialize JSON content - result was null");
                    return result;
                }
            }
            catch (JsonException ex)
            {
                result.Errors.Add($"Invalid JSON format: {ex.Message}");
                return result;
            }

            _logger.LogInformation("Parsed {Count} SequenceGroups from JSON", sequenceGroupDtos.Length);

            // Process each sequence group
            foreach (var dto in sequenceGroupDtos)
            {
                try
                {
                    await ProcessSequenceGroupImport(dto, replaceExisting, result);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error processing group '{dto.Name}': {ex.Message}");
                    _logger.LogError(ex, "Error processing SequenceGroup {Name}", dto.Name);
                }
            }

            result.Success = result.Errors.Count == 0;
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation("Import completed: {Summary}", result.Summary);
            return result;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Unexpected error during import: {ex.Message}");
            result.Duration = stopwatch.Elapsed;
            _logger.LogError(ex, "Unexpected error during SequenceGroup import");
            return result;
        }
    }

    public async Task<ImportResult> ImportSequenceGroupsFromFileAsync(string filePath, bool replaceExisting = false)
    {
        var result = new ImportResult();
        
        try
        {
            if (!File.Exists(filePath))
            {
                result.Errors.Add($"File not found: {filePath}");
                return result;
            }

            var jsonContent = await File.ReadAllTextAsync(filePath);
            _logger.LogInformation("Reading SequenceGroups from file: {FilePath}", filePath);
            
            return await ImportSequenceGroupsFromJsonAsync(jsonContent, replaceExisting);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error reading file '{filePath}': {ex.Message}");
            _logger.LogError(ex, "Error reading import file {FilePath}", filePath);
            return result;
        }
    }

    public async Task<string> ExportSequenceGroupsToJsonAsync(string? name = null, Technology? technology = null)
    {
        try
        {
            _logger.LogInformation("Exporting SequenceGroups to JSON. Name: {Name}, Technology: {Technology}", name, technology);

            // Get sequence groups from database (already preserves order)
            var sequenceGroups = await _sequenceGroupService.GetSequenceGroupsAsync(name, technology);
            var sequenceGroupArray = sequenceGroups.ToArray();

            _logger.LogInformation("Retrieved {Count} SequenceGroups for export", sequenceGroupArray.Length);

            // Serialize to JSON (order is preserved)
            var json = JsonSerializer.Serialize(sequenceGroupArray, _jsonOptions);
            
            _logger.LogInformation("Successfully serialized SequenceGroups to JSON ({Length} characters)", json.Length);
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting SequenceGroups to JSON");
            throw;
        }
    }

    public async Task<ExportResult> ExportSequenceGroupsToFileAsync(string filePath, string? name = null, Technology? technology = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ExportResult();
        
        try
        {
            _logger.LogInformation("Exporting SequenceGroups to file: {FilePath}", filePath);

            // Get JSON content
            var json = await ExportSequenceGroupsToJsonAsync(name, technology);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write to file
            await File.WriteAllTextAsync(filePath, json);
            
            // Get file info
            var fileInfo = new FileInfo(filePath);
            result.FileSizeBytes = fileInfo.Length;
            
            // Count exported items
            var sequenceGroups = JsonSerializer.Deserialize<SequenceGroupDTO[]>(json, _jsonOptions);
            if (sequenceGroups != null)
            {
                result.GroupsExported = sequenceGroups.Length;
                result.TotalSequencesExported = sequenceGroups.Sum(g => g.Sequences.Count());
            }

            result.Success = true;
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation("Export completed: {Summary}", result.Summary);
            return result;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error exporting to file '{filePath}': {ex.Message}");
            result.Duration = stopwatch.Elapsed;
            _logger.LogError(ex, "Error exporting SequenceGroups to file {FilePath}", filePath);
            return result;
        }
    }

    private async Task ProcessSequenceGroupImport(SequenceGroupDTO dto, bool replaceExisting, ImportResult result)
    {
        // Validate DTO
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            result.Errors.Add("SequenceGroup name cannot be empty");
            return;
        }

        // Check if group already exists
        var existingGroup = await _sequenceGroupService.GetSequenceGroupAsync(name: dto.Name);
        
        if (existingGroup != null && !replaceExisting)
        {
            result.GroupsSkipped++;
            result.Warnings.Add($"SequenceGroup '{dto.Name}' already exists and replaceExisting is false");
            return;
        }

        // Create or update sequences first
        var sequenceIds = new List<(int Id, int Order)>();
        int order = 1;
        
        foreach (var sequenceDto in dto.Sequences)
        {
            try
            {
                var sequenceId = await EnsureSequenceExists(sequenceDto);
                sequenceIds.Add((sequenceId, order++));
                result.TotalSequencesImported++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error creating sequence '{sequenceDto.Name}' for group '{dto.Name}': {ex.Message}");
                return;
            }
        }

        // Create or update sequence group
        var sequenceGroup = new SequenceGroup
        {
            Name = dto.Name,
            Technology = dto.Technology,
            Description = $"Imported on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"
        };

        try
        {
            if (existingGroup != null)
            {
                // Replace existing - would need to implement update logic
                result.Warnings.Add($"Update functionality not yet implemented for '{dto.Name}' - skipping");
                result.GroupsSkipped++;
                return;
            }
            else
            {
                // Create new group
                var createdGroup = await _sequenceGroupService.CreateSequenceGroupAsync(sequenceGroup);
                
                // Add sequences to group in order
                foreach (var (sequenceId, sequenceOrder) in sequenceIds)
                {
                    await _sequenceGroupService.AddSequenceToSequenceGroupAsync(
                        createdGroup.Id, sequenceId, sequenceOrder);
                }

                result.GroupsImported++;
                _logger.LogInformation("Successfully imported SequenceGroup '{Name}' with {SequenceCount} sequences", 
                    dto.Name, sequenceIds.Count);
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error creating SequenceGroup '{dto.Name}': {ex.Message}");
            _logger.LogError(ex, "Error creating SequenceGroup {Name}", dto.Name);
        }
    }

    private async Task<int> EnsureSequenceExists(Sequence sequenceDto)
    {
        // Check if sequence already exists by name
        var existingSequences = await _sequenceService.GetAllSequencesAsync();
        var existingSequence = existingSequences.FirstOrDefault(s => 
            string.Equals(s.Name, sequenceDto.Name, StringComparison.OrdinalIgnoreCase));

        if (existingSequence != null)
        {
            _logger.LogDebug("Sequence '{Name}' already exists with ID {Id}", sequenceDto.Name, existingSequence.Id);
            return existingSequence.Id;
        }

        // Create new sequence
        var newSequence = new Sequence
        {
            Name = sequenceDto.Name,
            Description = sequenceDto.Description,
            WorstCaseTime = sequenceDto.WorstCaseTime,
            CanBeParallel = sequenceDto.CanBeParallel
        };

        var createdSequence = await _sequenceService.CreateSequenceAsync(newSequence);
        _logger.LogDebug("Created new sequence '{Name}' with ID {Id}", sequenceDto.Name, createdSequence.Id);
        return createdSequence.Id;
    }
}