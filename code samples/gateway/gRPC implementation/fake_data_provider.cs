using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Instrument.Execution.Grpc;
using Instrument.Execution.Grpc.Configuration;
using Instrument.Execution.Parameter;
using Instrument.Grpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Instrument.Execution.Grpc.FakeService;

/// <summary>
/// Manages sample execution configuration data for the fake service.
/// </summary>
public class FakeDataProvider
{
    private readonly FakeServiceOptions _options;
    private readonly ILogger<FakeDataProvider> _logger;
    private readonly ConfigurationBuilder _configurationBuilder;
    private ExecutionConfigurationContract? _cachedConfiguration;
    private readonly object _lock = new();

    public FakeDataProvider(
        IOptions<FakeServiceOptions> options,
        ILogger<FakeDataProvider> logger,
        ConfigurationBuilder configurationBuilder)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configurationBuilder = configurationBuilder ?? throw new ArgumentNullException(nameof(configurationBuilder));
    }

    /// <summary>
    /// Gets the execution configuration with optional sequences.
    /// </summary>
    public async Task<ExecutionConfigurationContract> GetExecutionConfigurationAsync(bool includeSequences)
    {
        await EnsureConfigurationLoadedAsync();

        lock (_lock)
        {
            if (_cachedConfiguration == null)
                throw new InvalidOperationException("Configuration not loaded");

            if (!includeSequences)
            {
                return _cachedConfiguration with { Sequences = Array.Empty<ExecutionSequenceContract>() };
            }

            return _cachedConfiguration;
        }
    }

    /// <summary>
    /// Gets a specific sequence configuration by key.
    /// </summary>
    public async Task<ExecutionSequenceContract?> GetSequenceConfigurationAsync(string sequenceKey)
    {
        await EnsureConfigurationLoadedAsync();

        lock (_lock)
        {
            return _cachedConfiguration?.Sequences?.FirstOrDefault(s => 
                string.Equals(s.Key, sequenceKey, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Gets a specific resource configuration by key.
    /// </summary>
    public async Task<ExecutionResourceContract?> GetResourceConfigurationAsync(string resourceKey)
    {
        await EnsureConfigurationLoadedAsync();

        lock (_lock)
        {
            return _cachedConfiguration?.Sequences?
                .SelectMany(s => s.Resources)
                .FirstOrDefault(r => string.Equals(r.Key, resourceKey, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Simulates reloading the configuration.
    /// </summary>
    public async Task ReloadConfigurationAsync()
    {
        _logger.LogInformation("Reloading configuration data");
        
        lock (_lock)
        {
            _cachedConfiguration = null;
        }

        await EnsureConfigurationLoadedAsync();
        _logger.LogInformation("Configuration reloaded successfully");
    }

    /// <summary>
    /// Ensures the configuration is loaded, loading it if necessary.
    /// </summary>
    private async Task EnsureConfigurationLoadedAsync()
    {
        if (_cachedConfiguration != null)
            return;

        lock (_lock)
        {
            if (_cachedConfiguration != null)
                return;

            _logger.LogInformation("Loading configuration data from {DataSource}", _options.DataSourcePath);

            try
            {
                if (File.Exists(_options.DataSourcePath))
                {
                    _cachedConfiguration = LoadFromJsonFile(_options.DataSourcePath);
                }
                else
                {
                    _logger.LogWarning("Data source file not found, generating default configuration");
                    _cachedConfiguration = _configurationBuilder.BuildDefaultConfiguration();
                }

                _logger.LogInformation("Configuration loaded successfully with {SequenceCount} sequences",
                    _cachedConfiguration.Sequences?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration, using default");
                _cachedConfiguration = _configurationBuilder.BuildDefaultConfiguration();
            }
        }
    }

    /// <summary>
    /// Loads configuration from a JSON file.
    /// </summary>
    private ExecutionConfigurationContract LoadFromJsonFile(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var data = JsonSerializer.Deserialize<ConfigurationData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (data == null)
            throw new InvalidOperationException("Failed to deserialize configuration data");

        return ConvertToContract(data);
    }

    /// <summary>
    /// Converts JSON data to ExecutionConfigurationContract.
    /// </summary>
    private ExecutionConfigurationContract ConvertToContract(ConfigurationData data)
    {
        var sequences = data.Sequences?.Select(s => new ExecutionSequenceContract(
            s.Key,
            TimeSpan.Parse(s.WorstCaseTime),
            s.ExecutionMethod,
            s.ScriptKey ?? string.Empty,
            s.Resources?.Select(r => new ExecutionResourceContract(
                r.Key,
                r.HasScriptingInterface,
                r.ScriptingInterface ?? string.Empty
            )).ToArray() ?? Array.Empty<ExecutionResourceContract>(),
            s.Parameters?.Select(p => new SequenceParameterTypeContract(
                p.ParameterName,
                Enum.Parse<ParameterType>(p.ParameterType)
            )).ToArray() ?? Array.Empty<SequenceParameterTypeContract>()
        )).ToArray() ?? Array.Empty<ExecutionSequenceContract>();

        return new ExecutionConfigurationContract(
            data.StartingPeriod,
            data.RolloverPeriod,
            TimeSpan.Parse(data.PeriodSpan),
            data.PeriodAcceleration,
            sequences
        );
    }

    // JSON data structures for deserialization
    private class ConfigurationData
    {
        public int StartingPeriod { get; set; }
        public int RolloverPeriod { get; set; }
        public string PeriodSpan { get; set; } = "00:00:30";
        public double PeriodAcceleration { get; set; } = 1.0;
        public SequenceData[]? Sequences { get; set; }
    }

    private class SequenceData
    {
        public string Key { get; set; } = string.Empty;
        public string WorstCaseTime { get; set; } = "00:02:00";
        public string ExecutionMethod { get; set; } = "Native";
        public string? ScriptKey { get; set; }
        public ResourceData[]? Resources { get; set; }
        public ParameterData[]? Parameters { get; set; }
    }

    private class ResourceData
    {
        public string Key { get; set; } = string.Empty;
        public bool HasScriptingInterface { get; set; }
        public string? ScriptingInterface { get; set; }
    }

    private class ParameterData
    {
        public string ParameterName { get; set; } = string.Empty;
        public string ParameterType { get; set; } = "StringType";
    }
}

/// <summary>
/// Builds complex execution configurations programmatically.
/// </summary>
public class ConfigurationBuilder
{
    /// <summary>
    /// Builds a default configuration with sample data.
    /// </summary>
    public ExecutionConfigurationContract BuildDefaultConfiguration()
    {
        var sequences = new List<ExecutionSequenceContract>
        {
            BuildSampleSequence("SEQUENCE_001", "Basic Test Sequence"),
            BuildSampleSequence("SEQUENCE_002", "Advanced Test Sequence"),
            BuildComplexSequence("COMPLEX_001", "Complex Multi-Resource Sequence")
        };

        return new ExecutionConfigurationContract(
            StartingPeriod: 1,
            RolloverPeriod: 1000,
            PeriodSpan: TimeSpan.FromSeconds(30),
            PeriodAcceleration: 1.0,
            Sequences: sequences
        );
    }

    /// <summary>
    /// Builds a sample sequence with basic configuration.
    /// </summary>
    private ExecutionSequenceContract BuildSampleSequence(string key, string description)
    {
        var resources = new[]
        {
            new ExecutionResourceContract(
                Key: $"RESOURCE_{key.Split('_')[1]}",
                HasScriptingInterface: true,
                ScriptingInterface: "IInstrumentResource"
            )
        };

        var parameters = new[]
        {
            new SequenceParameterTypeContract("TargetValue", ParameterType.DecimalType),
            new SequenceParameterTypeContract("Timeout", ParameterType.IntegerType),
            new SequenceParameterTypeContract("EnableLogging", ParameterType.BooleanType)
        };

        return new ExecutionSequenceContract(
            Key: key,
            WorstCaseTime: TimeSpan.FromMinutes(2),
            ExecutionMethod: "Native",
            ScriptKey: string.Empty,
            Resources: resources,
            Parameters: parameters
        );
    }

    /// <summary>
    /// Builds a complex sequence with multiple resources and parameters.
    /// </summary>
    private ExecutionSequenceContract BuildComplexSequence(string key, string description)
    {
        var resources = new[]
        {
            new ExecutionResourceContract("PRIMARY_INSTRUMENT", true, "IPrimaryInstrument"),
            new ExecutionResourceContract("SECONDARY_SENSOR", true, "ISecondarySensor"),
            new ExecutionResourceContract("DATA_LOGGER", false, string.Empty),
            new ExecutionResourceContract("POWER_CONTROLLER", true, "IPowerController")
        };

        var parameters = new[]
        {
            new SequenceParameterTypeContract("PrimaryTarget", ParameterType.DecimalType),
            new SequenceParameterTypeContract("SecondaryTarget", ParameterType.DecimalType),
            new SequenceParameterTypeContract("SensorMode", ParameterType.EnumType),
            new SequenceParameterTypeContract("PowerLevel", ParameterType.IntegerType),
            new SequenceParameterTypeContract("DataPoints", ParameterType.ArrayType),
            new SequenceParameterTypeContract("ConfigurationFile", ParameterType.StringType),
            new SequenceParameterTypeContract("AutoCalibrate", ParameterType.BooleanType)
        };

        return new ExecutionSequenceContract(
            Key: key,
            WorstCaseTime: TimeSpan.FromMinutes(5),
            ExecutionMethod: "Scripted",
            ScriptKey: "ComplexSequenceScript.py",
            Resources: resources,
            Parameters: parameters
        );
    }
}

/// <summary>
/// Simulates network delays and failures for testing.
/// </summary>
public class DelaySimulator
{
    private readonly FakeServiceOptions _options;
    private readonly Random _random = new();

    public DelaySimulator(IOptions<FakeServiceOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Simulates a configurable delay.
    /// </summary>
    public async Task SimulateDelayAsync()
    {
        if (_options.ResponseDelay > TimeSpan.Zero)
        {
            await Task.Delay(_options.ResponseDelay);
        }
    }

    /// <summary>
    /// Determines whether to simulate a failure based on configured failure rate.
    /// </summary>
    public Task<bool> ShouldSimulateFailureAsync()
    {
        return Task.FromResult(_random.NextDouble() < _options.FailureRate);
    }
}

/// <summary>
/// Builds error responses for the fake service.
/// </summary>
public class ResponseBuilder
{
    /// <summary>
    /// Creates an error response with the specified details.
    /// </summary>
    public T CreateErrorResponse<T>(Guid requestId, string message, int errorCode) where T : class
    {
        var errors = new[]
        {
            new GrpcErrorContract(
                ErrorCode: errorCode,
                Message: message,
                Details: Array.Empty<string>()
            )
        };

        // Use reflection to create the response with the appropriate constructor
        var constructor = typeof(T).GetConstructors().FirstOrDefault();
        if (constructor == null)
            throw new InvalidOperationException($"No constructor found for type {typeof(T).Name}");

        var parameters = constructor.GetParameters();
        var args = new object[parameters.Length];

        // Set the standard parameters
        args[0] = requestId; // RequestId
        
        // Set data parameter to null (for fetch responses) or skip it (for update responses)
        if (parameters.Length > 2)
            args[1] = null!; // Data parameter
        
        args[^1] = errors; // Errors (last parameter)

        return (T)Activator.CreateInstance(typeof(T), args)!;
    }
}