using Instrument.Data.DTOs;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;

namespace Instrument.Data.Services;

public interface ISequenceGroupImportExportService
{
    /// <summary>
    /// Imports SequenceGroups from JSON and persists them to the data store
    /// </summary>
    /// <param name="jsonContent">JSON content containing SequenceGroups</param>
    /// <param name="replaceExisting">Whether to replace existing groups with same name</param>
    /// <returns>Import result with details</returns>
    Task<ImportResult> ImportSequenceGroupsFromJsonAsync(string jsonContent, bool replaceExisting = false);
    
    /// <summary>
    /// Imports SequenceGroups from JSON file and persists them to the data store
    /// </summary>
    /// <param name="filePath">Path to JSON file</param>
    /// <param name="replaceExisting">Whether to replace existing groups with same name</param>
    /// <returns>Import result with details</returns>
    Task<ImportResult> ImportSequenceGroupsFromFileAsync(string filePath, bool replaceExisting = false);
    
    /// <summary>
    /// Exports SequenceGroups from data store to JSON
    /// </summary>
    /// <param name="name">Optional name filter</param>
    /// <param name="technology">Optional technology filter</param>
    /// <returns>JSON string containing SequenceGroups</returns>
    Task<string> ExportSequenceGroupsToJsonAsync(string? name = null, Technology? technology = null);
    
    /// <summary>
    /// Exports SequenceGroups from data store to JSON file
    /// </summary>
    /// <param name="filePath">Output file path</param>
    /// <param name="name">Optional name filter</param>
    /// <param name="technology">Optional technology filter</param>
    /// <returns>Export result with details</returns>
    Task<ExportResult> ExportSequenceGroupsToFileAsync(string filePath, string? name = null, Technology? technology = null);
}

public class ImportResult
{
    public bool Success { get; set; }
    public int GroupsImported { get; set; }
    public int GroupsSkipped { get; set; }
    public int GroupsReplaced { get; set; }
    public int TotalSequencesImported { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public TimeSpan Duration { get; set; }
    
    public string Summary => Success 
        ? $"Import successful: {GroupsImported} groups imported, {GroupsReplaced} replaced, {GroupsSkipped} skipped. {TotalSequencesImported} sequences total."
        : $"Import failed: {Errors.Count} errors, {Warnings.Count} warnings.";
}

public class ExportResult
{
    public bool Success { get; set; }
    public int GroupsExported { get; set; }
    public int TotalSequencesExported { get; set; }
    public long FileSizeBytes { get; set; }
    public List<string> Errors { get; set; } = [];
    public TimeSpan Duration { get; set; }
    
    public string Summary => Success 
        ? $"Export successful: {GroupsExported} groups exported with {TotalSequencesExported} sequences total. File size: {FileSizeBytes} bytes."
        : $"Export failed: {Errors.Count} errors.";
}