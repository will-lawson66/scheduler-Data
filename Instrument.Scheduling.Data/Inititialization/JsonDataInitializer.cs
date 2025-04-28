using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Instrument.Scheduling.Data.Entities;
using Microsoft.Extensions.Logging;
using Range = Instrument.Scheduling.Data.Entities.Range;

namespace Instrument.Scheduling.Data.Initialization
{
    /// <summary>
    /// Handles JSON file initialization for storage
    /// </summary>
    public class JsonFileInitializer : IDataInitializer
    {
        private readonly string _basePath;
        private readonly ILogger<JsonFileInitializer> _logger;

        // File paths for each entity type
        private readonly Dictionary<Type, string> _filePaths;

        public JsonFileInitializer(
            string basePath,
            ILogger<JsonFileInitializer> logger)
        {
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize file paths
            _filePaths = new Dictionary<Type, string>
            {
                { typeof(Sequence), Path.Combine(_basePath, "sequences.json") },
                { typeof(Parameter), Path.Combine(_basePath, "parameters.json") },
                { typeof(SequenceParameter), Path.Combine(_basePath, "sequence_parameters.json") },
                { typeof(Range), Path.Combine(_basePath, "ranges.json") },
                { typeof(RangeValue), Path.Combine(_basePath, "range_values.json") },
                { typeof(Resource), Path.Combine(_basePath, "resources.json") }
            };
        }

        /// <summary>
        /// Checks if the JSON files exist
        /// </summary>
        public Task<bool> ExistsAsync()
        {
            bool exists = Directory.Exists(_basePath) &&
                          _filePaths.Values.Any(path => File.Exists(path));

            _logger.LogInformation("JSON storage {Exists} at {Path}",
                exists ? "exists" : "does not exist", _basePath);

            return Task.FromResult(exists);
        }

        /// <summary>
        /// Initializes all required JSON files
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing JSON storage at {BasePath}", _basePath);

                // Ensure directory exists
                if (!Directory.Exists(_basePath))
                {
                    _logger.LogInformation("Creating base directory {BasePath}", _basePath);
                    Directory.CreateDirectory(_basePath);
                }

                // Initialize each file
                await InitializeFileAsync<Sequence>();
                await InitializeFileAsync<Parameter>();
                await InitializeFileAsync<SequenceParameter>();
                await InitializeFileAsync<Range>();
                await InitializeFileAsync<RangeValue>();
                await InitializeFileAsync<Resource>();

                _logger.LogInformation("JSON storage initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize JSON storage at {BasePath}", _basePath);
                throw;
            }
        }

        /// <summary>
        /// No-op for JSON storage since it doesn't have migrations
        /// </summary>
        public Task<bool> MigrateAsync()
        {
            _logger.LogInformation("Migrations not applicable for JSON storage");
            return Task.FromResult(false);
        }

        /// <summary>
        /// Seeds the JSON files with default data if they're empty
        /// </summary>
        public async Task<bool> SeedDefaultDataAsync()
        {
            try
            {
                // Check if files are empty
                bool isEmpty = true;

                foreach (var filePath in _filePaths.Values)
                {
                    if (File.Exists(filePath) && new FileInfo(filePath).Length > 10)
                    {
                        // If any file has content, consider storage not empty
                        isEmpty = false;
                        break;
                    }
                }

                if (isEmpty)
                {
                    _logger.LogInformation("Seeding default data");

                    // Create default range
                    var defaultRange = new Range
                    {
                        Id = "default-range",
                        Name = "Default Range",
                        Description = "Default range for testing"
                    };

                    // Create default range values
                    var rangeValues = new List<RangeValue>
                    {
                        new RangeValue
                        {
                            Id = "1",
                            RangeId = defaultRange.Id,
                            Value = "Value1",
                            Name = "Default Value 1"
                        },
                        new RangeValue
                        {
                            Id = "2",
                            RangeId = defaultRange.Id,
                            Value = "Value2",
                            Name = "Default Value 2"
                        }
                    };

                    // Create default parameter
                    var defaultParameter = new Parameter
                    {
                        Id = "default-param",
                        Name = "Default Parameter",
                        Type = "string",
                        DefaultValue = "default"
                    };

                    // Create default sequence
                    var defaultSequence = new Sequence
                    {
                        Id = "default-sequence",
                        Name = "Default Sequence",
                        Description = "Default sequence for testing",
                        WorstCaseTime = TimeSpan.FromSeconds(30),
                        CanBeParallel = false
                    };

                    // Create sequence parameter link
                    var sequenceParameter = new SequenceParameter
                    {
                        SequenceId = defaultSequence.Id,
                        ParameterId = defaultParameter.Id,
                        OrderNumber = 1
                    };

                    // Save to files
                    await SaveToJsonFileAsync(new List<Range> { defaultRange }, typeof(Range));
                    await SaveToJsonFileAsync(rangeValues, typeof(RangeValue));
                    await SaveToJsonFileAsync(new List<Parameter> { defaultParameter }, typeof(Parameter));
                    await SaveToJsonFileAsync(new List<Sequence> { defaultSequence }, typeof(Sequence));
                    await SaveToJsonFileAsync(new List<SequenceParameter> { sequenceParameter }, typeof(SequenceParameter));
                    await SaveToJsonFileAsync(new List<Resource>(), typeof(Resource));

                    _logger.LogInformation("Default data seeded successfully");
                    return true;
                }

                _logger.LogInformation("JSON storage already contains data, skipping seed");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed default data");
                throw;
            }
        }

        /// <summary>
        /// Gets a status message about the JSON storage
        /// </summary>
        public async Task<string> GetStatusMessageAsync()
        {
            try
            {
                bool exists = await ExistsAsync();
                if (!exists)
                {
                    return $"JSON storage does not exist at {_basePath}";
                }

                var status = new List<string>();

                foreach (var (type, path) in _filePaths)
                {
                    if (File.Exists(path))
                    {
                        var count = await GetEntityCountAsync(type);
                        status.Add($"{type.Name}: {count}");
                    }
                    else
                    {
                        status.Add($"{type.Name}: file not found");
                    }
                }

                return $"JSON storage at {_basePath} contains: {string.Join(", ", status)}";
            }
            catch (Exception ex)
            {
                return $"Error accessing JSON storage: {ex.Message}";
            }
        }

        /// <summary>
        /// Initializes a specific JSON file if it doesn't exist
        /// </summary>
        private async Task InitializeFileAsync<T>() where T : class
        {
            var type = typeof(T);
            if (!_filePaths.TryGetValue(type, out var filePath))
            {
                _logger.LogWarning("No file path defined for type {Type}", type.Name);
                return;
            }

            if (!File.Exists(filePath))
            {
                _logger.LogInformation("Creating JSON file {FilePath}", filePath);

                // Create empty array as initial content
                var emptyCollection = new List<T>();
                var jsonString = JsonSerializer.Serialize(emptyCollection, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(filePath, jsonString);
            }
            else
            {
                _logger.LogDebug("JSON file already exists: {FilePath}", filePath);

                // Validate file is valid JSON
                try
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    JsonSerializer.Deserialize<List<T>>(content);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "JSON file {FilePath} contains invalid format. Creating backup and initializing new file", filePath);

                    // Backup the invalid file
                    var backupPath = $"{filePath}.bak.{DateTime.Now:yyyyMMddHHmmss}";
                    File.Copy(filePath, backupPath);

                    // Create new empty file
                    var emptyCollection = new List<T>();
                    var jsonString = JsonSerializer.Serialize(emptyCollection, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    await File.WriteAllTextAsync(filePath, jsonString);
                }
            }
        }

        /// <summary>
        /// Gets the number of entities in a JSON file
        /// </summary>
        private async Task<int> GetEntityCountAsync(Type entityType)
        {
            if (!_filePaths.TryGetValue(entityType, out var filePath) || !File.Exists(filePath))
                return 0;

            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                var jsonDocument = JsonDocument.Parse(content);

                // Count root array elements
                if (jsonDocument.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return jsonDocument.RootElement.GetArrayLength();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting entities in {FilePath}", filePath);
            }

            return 0;
        }

        /// <summary>
        /// Saves entities to a JSON file
        /// </summary>
        private async Task SaveToJsonFileAsync<T>(IEnumerable<T> entities, Type entityType)
        {
            if (!_filePaths.TryGetValue(entityType, out var filePath))
                return;

            var jsonString = JsonSerializer.Serialize(entities, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(filePath, jsonString);
        }
    }
}