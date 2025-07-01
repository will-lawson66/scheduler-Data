using Instrument.Data.DTOs;

namespace Instrument.Data.Services;

public interface ISequenceImportExportService
{
    /// <summary>
    /// Imports Sequences from JSON and persists them to the data store
    /// </summary>
    /// <param name="jsonContent">JSON content containing SequenceDTOs</param>
    /// <param name="replaceExisting">Whether to replace existing sequences with same name</param>
    /// <returns>Import result with details</returns>
    Task<SequenceImportResult> ImportSequencesFromJsonAsync(string jsonContent, bool replaceExisting = false);
    
    /// <summary>
    /// Imports Sequences from JSON file and persists them to the data store
    /// </summary>
    /// <param name="filePath">Path to JSON file</param>
    /// <param name="replaceExisting">Whether to replace existing sequences with same name</param>
    /// <returns>Import result with details</returns>
    Task<SequenceImportResult> ImportSequencesFromFileAsync(string filePath, bool replaceExisting = false);
    
    /// <summary>
    /// Exports Sequences from data store to JSON
    /// </summary>
    /// <param name="name">Optional name filter</param>
    /// <returns>JSON string containing SequenceDTOs</returns>
    Task<string> ExportSequencesToJsonAsync(string? name = null);
    
    /// <summary>
    /// Exports Sequences from data store to JSON file
    /// </summary>
    /// <param name="filePath">Output file path</param>
    /// <param name="name">Optional name filter</param>
    /// <returns>Export result with details</returns>
    Task<SequenceExportResult> ExportSequencesToFileAsync(string filePath, string? name = null);
}

public class SequenceImportResult
{
    public bool Success { get; set; }
    public int SequencesImported { get; set; }
    public int SequencesSkipped { get; set; }
    public int SequencesReplaced { get; set; }
    public int TotalParametersImported { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public TimeSpan Duration { get; set; }
    
    public string Summary => Success 
        ? $"Import successful: {SequencesImported} sequences imported, {SequencesReplaced} replaced, {SequencesSkipped} skipped. {TotalParametersImported} parameters total."
        : $"Import failed: {Errors.Count} errors, {Warnings.Count} warnings.";
}

public class SequenceExportResult
{
    public bool Success { get; set; }
    public int SequencesExported { get; set; }
    public int TotalParametersExported { get; set; }
    public long FileSizeBytes { get; set; }
    public List<string> Errors { get; set; } = [];
    public TimeSpan Duration { get; set; }
    
    public string Summary => Success 
        ? $"Export successful: {SequencesExported} sequences exported with {TotalParametersExported} parameters total. File size: {FileSizeBytes} bytes."
        : $"Export failed: {Errors.Count} errors.";
}