using System.Diagnostics;
using System.Text.Json;
using Instrument.Data.Configuration;
using Instrument.Data.DTOs;
using Instrument.Data.Entities;
using Microsoft.Extensions.Logging;

namespace Instrument.Data.Services;

public class SequenceImportExportService : ISequenceImportExportService
{
    private readonly ISequenceService _sequenceService;
    private readonly IParameterService _parameterService;
    private readonly ILogger<SequenceImportExportService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SequenceImportExportService(
        ISequenceService sequenceService,
        IParameterService parameterService,
        ILogger<SequenceImportExportService> logger)
    {
        _sequenceService = sequenceService;
        _parameterService = parameterService;
        _logger = logger;
        
        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters =
            {
                new TimeSpanJsonConverter()
            }
        };
    }

    public async Task<SequenceImportResult> ImportSequencesFromJsonAsync(string jsonContent, bool replaceExisting = false)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new SequenceImportResult();
        
        try
        {
            _logger.LogInformation("Starting Sequence import from JSON. ReplaceExisting: {ReplaceExisting}", replaceExisting);

            // Parse JSON content
            SequenceDTO[]? sequenceDtos;
            try
            {
                sequenceDtos = JsonSerializer.Deserialize<SequenceDTO[]>(jsonContent, _jsonOptions);
                if (sequenceDtos == null)
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

            _logger.LogInformation("Parsed {Count} Sequences from JSON", sequenceDtos.Length);

            // Process each sequence
            foreach (var dto in sequenceDtos)
            {
                try
                {
                    await ProcessSequenceImport(dto, replaceExisting, result);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error processing sequence '{dto.Name}': {ex.Message}");
                    _logger.LogError(ex, "Error processing Sequence {Name}", dto.Name);
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
            _logger.LogError(ex, "Unexpected error during Sequence import");
            return result;
        }
    }

    public async Task<SequenceImportResult> ImportSequencesFromFileAsync(string filePath, bool replaceExisting = false)
    {
        var result = new SequenceImportResult();
        
        try
        {
            if (!File.Exists(filePath))
            {
                result.Errors.Add($"File not found: {filePath}");
                return result;
            }

            var jsonContent = await File.ReadAllTextAsync(filePath);
            _logger.LogInformation("Reading Sequences from file: {FilePath}", filePath);
            
            return await ImportSequencesFromJsonAsync(jsonContent, replaceExisting);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error reading file '{filePath}': {ex.Message}");
            _logger.LogError(ex, "Error reading import file {FilePath}", filePath);
            return result;
        }
    }

    public async Task<string> ExportSequencesToJsonAsync(string? name = null)
    {
        try
        {
            _logger.LogInformation("Exporting Sequences to JSON. Name: {Name}", name);

            // Get sequences from database (already preserves parameter order)
            var sequences = await _sequenceService.GetSequencesAsync(name);
            var sequenceArray = sequences.ToArray();

            _logger.LogInformation("Retrieved {Count} Sequences for export", sequenceArray.Length);

            // Serialize to JSON (parameter order is preserved)
            var json = JsonSerializer.Serialize(sequenceArray, _jsonOptions);
            
            _logger.LogInformation("Successfully serialized Sequences to JSON ({Length} characters)", json.Length);
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting Sequences to JSON");
            throw;
        }
    }

    public async Task<SequenceExportResult> ExportSequencesToFileAsync(string filePath, string? name = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new SequenceExportResult();
        
        try
        {
            _logger.LogInformation("Exporting Sequences to file: {FilePath}", filePath);

            // Get JSON content
            var json = await ExportSequencesToJsonAsync(name);
            
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
            var sequences = JsonSerializer.Deserialize<SequenceDTO[]>(json, _jsonOptions);
            if (sequences != null)
            {
                result.SequencesExported = sequences.Length;
                result.TotalParametersExported = sequences.Sum(s => s.Parameters.Count());
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
            _logger.LogError(ex, "Error exporting Sequences to file {FilePath}", filePath);
            return result;
        }
    }

    private async Task ProcessSequenceImport(SequenceDTO dto, bool replaceExisting, SequenceImportResult result)
    {
        // Validate DTO
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            result.Errors.Add("Sequence name cannot be empty");
            return;
        }

        // Check if sequence already exists
        var existingSequence = await _sequenceService.GetSequenceAsync(name: dto.Name);
        
        if (existingSequence != null && !replaceExisting)
        {
            result.SequencesSkipped++;
            result.Warnings.Add($"Sequence '{dto.Name}' already exists and replaceExisting is false");
            return;
        }

        // Create or update parameters first
        var parameterIds = new List<(int Id, int Order)>();
        int order = 1;
        
        foreach (var parameterDto in dto.Parameters)
        {
            try
            {
                var parameterId = await EnsureParameterExists(parameterDto);
                parameterIds.Add((parameterId, order++));
                result.TotalParametersImported++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error creating parameter '{parameterDto.Name}' for sequence '{dto.Name}': {ex.Message}");
                return;
            }
        }

        // Create sequence entity
        var sequence = new Sequence
        {
            Name = dto.Name,
            Description = $"Imported on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            WorstCaseTime = TimeSpan.FromMinutes(1), // Default time since SequenceDTO doesn't include this
            CanBeParallel = false // Default value since SequenceDTO doesn't include this
        };

        try
        {
            if (existingSequence != null)
            {
                // Replace existing - would need to implement update logic
                result.Warnings.Add($"Update functionality not yet implemented for '{dto.Name}' - skipping");
                result.SequencesSkipped++;
                return;
            }
            else
            {
                // Create new sequence
                var createdSequence = await _sequenceService.CreateSequenceAsync(sequence);
                
                // Add parameters to sequence in order
                foreach (var (parameterId, parameterOrder) in parameterIds)
                {
                    await _sequenceService.AddParameterToSequenceAsync(parameterId, createdSequence.Id, parameterOrder);
                }

                result.SequencesImported++;
                _logger.LogInformation("Successfully imported Sequence '{Name}' with {ParameterCount} parameters", 
                    dto.Name, parameterIds.Count);
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error creating Sequence '{dto.Name}': {ex.Message}");
            _logger.LogError(ex, "Error creating Sequence {Name}", dto.Name);
        }
    }

    private async Task<int> EnsureParameterExists(Parameter parameterDto)
    {
        // Check if parameter already exists by name
        var existingParameters = await _parameterService.GetAllParametersAsync();
        var existingParameter = existingParameters.FirstOrDefault(p => 
            string.Equals(p.Name, parameterDto.Name, StringComparison.OrdinalIgnoreCase));

        if (existingParameter != null)
        {
            _logger.LogDebug("Parameter '{Name}' already exists with ID {Id}", parameterDto.Name, existingParameter.Id);
            return existingParameter.Id;
        }

        // Create new parameter
        var newParameter = new Parameter
        {
            Name = parameterDto.Name,
            Type = parameterDto.Type,
            Min = parameterDto.Min,
            Max = parameterDto.Max,
            DefaultValue = parameterDto.DefaultValue,
            Format = parameterDto.Format
        };

        var createdParameter = await _parameterService.CreateParameterAsync(newParameter);
        _logger.LogDebug("Created new parameter '{Name}' with ID {Id}", parameterDto.Name, createdParameter.Id);
        return createdParameter.Id;
    }
}