// =====================================================================================
// CONFIGURATION ORCHESTRATOR IMPLEMENTATION
// Integrates gRPC Gateway + Scheduler Data Services for Configuration Management
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;
using System.ServiceModel;
using Instrument.Data.Gateways;
using Instrument.Data.Adapters;

// =====================================================================================
// 1. EXECUTION SERVICE gRPC CONTRACTS
// =====================================================================================

namespace Instrument.Data.Execution.Contracts
{
    /// <summary>
    /// gRPC service interface for ExecutionConfigurationService
    /// </summary>
    [ServiceContract]
    public interface IExecutionConfigurationService
    {
        [OperationContract]
        Task<GetCurrentConfigurationResponse> GetCurrentConfigurationAsync(
            GetCurrentConfigurationRequest request, 
            CancellationToken cancellationToken = default);

        [OperationContract]
        Task<GetSequenceConfigurationResponse> GetSequenceConfigurationAsync(
            GetSequenceConfigurationRequest request, 
            CancellationToken cancellationToken = default);

        [OperationContract]
        Task<GetResourceConfigurationResponse> GetResourceConfigurationAsync(
            GetResourceConfigurationRequest request, 
            CancellationToken cancellationToken = default);
    }

    // Request DTOs
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record GetCurrentConfigurationRequest(bool IncludeSequences = true);

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record GetSequenceConfigurationRequest(string Key);

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record GetResourceConfigurationRequest(string Key);

    // Response DTOs (Fixed from your sample - had syntax errors)
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record GetCurrentConfigurationResponse(
        GuidRequestId RequestId,
        ExecutionConfigurationContract? Configuration,
        IReadOnlyCollection<GrpcErrorContract> Errors
    );

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record GetSequenceConfigurationResponse(
        GuidRequestId RequestId,
        ExecutionSequenceContract? Sequence,
        IReadOnlyCollection<GrpcErrorContract> Errors
    );

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record GetResourceConfigurationResponse(
        GuidRequestId RequestId,
        ExecutionResourceContract? Resource,
        IReadOnlyCollection<GrpcErrorContract> Errors
    );

    // Configuration DTOs
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record ExecutionConfigurationContract(
        int StartingPeriod,
        int RolloverPeriod,
        TimeSpan PeriodSpan,
        double PeriodAcceleration,
        IReadOnlyCollection<ExecutionSequenceContract> Sequences
    );

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record ExecutionSequenceContract(
        string Key,
        TimeSpan WorstCaseTime,
        string ExecutionMethod,
        string ScriptKey,
        IReadOnlyCollection<ExecutionResourceContract> Resources,
        IReadOnlyCollection<SequenceParameterTypeContract> Parameters
    );

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record ExecutionResourceContract(
        string Key,
        bool HasScriptingInterface,
        string ScriptingInterface
    );

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record SequenceParameterTypeContract(
        string ParameterName,
        ParameterType ParameterType
    );

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record GuidRequestId(string Lo, string Hi);

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic, SkipConstructor = true)]
    public record GrpcErrorContract(string Message, string Code);

    public enum ParameterType
    {
        StringType,
        IntegerType,
        DecimalType,
        BooleanType,
        ArrayType,
        EnumType
    }
}

// =====================================================================================
// 2. gRPC OPERATIONS FOR EXECUTION SERVICE
// =====================================================================================

namespace Instrument.Data.Execution.Operations
{
    using Instrument.Data.Execution.Contracts;

    /// <summary>
    /// Operation to fetch current configuration from ExecutionConfigurationService
    /// </summary>
    public class GetCurrentConfigurationOperation : GrpcOperation<GetCurrentConfigurationRequest, GetCurrentConfigurationResponse>
    {
        public override string ServiceName => "ExecutionConfigurationService";
        public override string OperationId => "GetCurrentConfiguration";
        public override bool IsCacheable => true;
        public override TimeSpan? Timeout => TimeSpan.FromMinutes(2); // Large payload

        public override async Task<GetCurrentConfigurationResponse> ExecuteAsync(
            GrpcChannel channel, 
            GetCurrentConfigurationRequest request, 
            CancellationToken cancellationToken)
        {
            var client = channel.CreateGrpcService<IExecutionConfigurationService>();
            return await client.GetCurrentConfigurationAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Operation to fetch single sequence configuration
    /// </summary>
    public class GetSequenceConfigurationOperation : GrpcOperation<GetSequenceConfigurationRequest, GetSequenceConfigurationResponse>
    {
        public override string ServiceName => "ExecutionConfigurationService";
        public override string OperationId => "GetSequenceConfiguration";
        public override bool IsCacheable => true;

        public override async Task<GetSequenceConfigurationResponse> ExecuteAsync(
            GrpcChannel channel, 
            GetSequenceConfigurationRequest request, 
            CancellationToken cancellationToken)
        {
            var client = channel.CreateGrpcService<IExecutionConfigurationService>();
            return await client.GetSequenceConfigurationAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Operation to fetch single resource configuration
    /// </summary>
    public class GetResourceConfigurationOperation : GrpcOperation<GetResourceConfigurationRequest, GetResourceConfigurationResponse>
    {
        public override string ServiceName => "ExecutionConfigurationService";
        public override string OperationId => "GetResourceConfiguration";
        public override bool IsCacheable => true;

        public override async Task<GetResourceConfigurationResponse> ExecuteAsync(
            GrpcChannel channel, 
            GetResourceConfigurationRequest request, 
            CancellationToken cancellationToken)
        {
            var client = channel.CreateGrpcService<IExecutionConfigurationService>();
            return await client.GetResourceConfigurationAsync(request, cancellationToken);
        }
    }
}

// =====================================================================================
// 3. CONFIGURATION ORCHESTRATOR INTERFACES
// =====================================================================================

namespace Instrument.Data.Orchestration
{
    /// <summary>
    /// Main interface for configuration orchestration
    /// </summary>
    public interface IConfigurationOrchestrator
    {
        /// <summary>
        /// Fetch and import the current configuration from execution service
        /// </summary>
        Task<ConfigurationImportResult> ImportCurrentConfigurationAsync(
            bool includeSequences = true,
            bool clearExistingData = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Fetch and import a specific sequence configuration
        /// </summary>
        Task<ConfigurationImportResult> ImportSequenceConfigurationAsync(
            string sequenceKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Fetch and import a specific resource configuration
        /// </summary>
        Task<ConfigurationImportResult> ImportResourceConfigurationAsync(
            string resourceKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Test connectivity to the execution configuration service
        /// </summary>
        Task<bool> TestExecutionServiceConnectionAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Options for configuration orchestrator
    /// </summary>
    public class ConfigurationOrchestratorOptions
    {
        public const string SectionName = "ConfigurationOrchestrator";

        /// <summary>
        /// Whether to validate data before inserting into database
        /// </summary>
        public bool ValidateDataBeforeInsert { get; set; } = true;

        /// <summary>
        /// Whether to initialize database on startup
        /// </summary>
        public bool InitializeDatabaseOnStartup { get; set; } = true;

        /// <summary>
        /// Maximum number of retry attempts for data import
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Timeout for data import operations
        /// </summary>
        public TimeSpan ImportTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Result of configuration import operation
    /// </summary>
    public class ConfigurationImportResult
    {
        public bool Success { get; set; }
        public int SequencesImported { get; set; }
        public int ResourcesImported { get; set; }
        public int ParametersImported { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public bool DataFromCache { get; set; }
        public string RequestId { get; set; }
    }
}

// =====================================================================================
// 4. CONFIGURATION ORCHESTRATOR IMPLEMENTATION
// =====================================================================================

namespace Instrument.Data.Orchestration
{
    using Instrument.Data.Execution.Contracts;
    using Instrument.Data.Execution.Operations;

    /// <summary>
    /// Orchestrates fetching configuration from gRPC service and importing into data layer
    /// </summary>
    public class ConfigurationOrchestrator : IConfigurationOrchestrator
    {
        private readonly IGrpcGateway _grpcGateway;
        private readonly IJsonDataAdapter _jsonAdapter;
        private readonly IDataInitializer _dataInitializer;
        private readonly ILogger<ConfigurationOrchestrator> _logger;
        private readonly ConfigurationOrchestratorOptions _options;

        // Domain services for direct data manipulation
        private readonly ISequenceService _sequenceService;
        private readonly IResourceService _resourceService;
        private readonly IParameterService _parameterService;

        public ConfigurationOrchestrator(
            IGrpcGateway grpcGateway,
            IJsonDataAdapter jsonAdapter,
            IDataInitializer dataInitializer,
            ISequenceService sequenceService,
            IResourceService resourceService,
            IParameterService parameterService,
            ILogger<ConfigurationOrchestrator> logger,
            IOptions<ConfigurationOrchestratorOptions> options)
        {
            _grpcGateway = grpcGateway;
            _jsonAdapter = jsonAdapter;
            _dataInitializer = dataInitializer;
            _sequenceService = sequenceService;
            _resourceService = resourceService;
            _parameterService = parameterService;
            _logger = logger;
            _options = options.Value;
        }

        /// <summary>
        /// Main method: Fetch current configuration and import into data layer
        /// </summary>
        public async Task<ConfigurationImportResult> ImportCurrentConfigurationAsync(
            bool includeSequences = true,
            bool clearExistingData = false,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new ConfigurationImportResult();

            try
            {
                _logger.LogInformation("Starting configuration import (IncludeSequences: {IncludeSequences}, ClearData: {ClearData})",
                    includeSequences, clearExistingData);

                // Step 1: Fetch configuration from gRPC service
                var grpcResult = await FetchCurrentConfigurationAsync(includeSequences, cancellationToken);

                if (!grpcResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = grpcResult.ErrorMessage;
                    return result;
                }

                var configResponse = grpcResult.Data;
                result.RequestId = $"{configResponse.RequestId.Lo}-{configResponse.RequestId.Hi}";
                result.DataFromCache = grpcResult.FromCache;

                // Step 2: Validate response
                if (configResponse.Configuration == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Configuration is null in response";
                    return result;
                }

                // Step 3: Initialize/Clear database if requested
                if (clearExistingData)
                {
                    await ClearExistingDataAsync(cancellationToken);
                }

                await _dataInitializer.InitializeAsync(cancellationToken);

                // Step 4: Transform and import data
                await ImportConfigurationDataAsync(configResponse.Configuration, result, cancellationToken);

                result.Success = true;
                _logger.LogInformation("Configuration import completed successfully. Sequences: {SeqCount}, Resources: {ResCount}, Parameters: {ParamCount}",
                    result.SequencesImported, result.ResourcesImported, result.ParametersImported);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Configuration import failed");
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                result.ExecutionTime = stopwatch.Elapsed;
            }

            return result;
        }

        /// <summary>
        /// Import a specific sequence configuration
        /// </summary>
        public async Task<ConfigurationImportResult> ImportSequenceConfigurationAsync(
            string sequenceKey,
            CancellationToken cancellationToken = default)
        {
            var result = new ConfigurationImportResult();

            try
            {
                _logger.LogInformation("Importing sequence configuration for key: {SequenceKey}", sequenceKey);

                var grpcResult = await _grpcGateway.ExecuteAsync(
                    new GetSequenceConfigurationOperation(),
                    new GetSequenceConfigurationRequest(sequenceKey),
                    cancellationToken);

                if (!grpcResult.Success || grpcResult.Data?.Sequence == null)
                {
                    result.Success = false;
                    result.ErrorMessage = grpcResult.ErrorMessage ?? "Sequence not found";
                    return result;
                }

                // Import single sequence
                await ImportSingleSequenceAsync(grpcResult.Data.Sequence, cancellationToken);
                
                result.Success = true;
                result.SequencesImported = 1;
                result.DataFromCache = grpcResult.FromCache;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import sequence configuration for key: {SequenceKey}", sequenceKey);
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Import a specific resource configuration
        /// </summary>
        public async Task<ConfigurationImportResult> ImportResourceConfigurationAsync(
            string resourceKey,
            CancellationToken cancellationToken = default)
        {
            var result = new ConfigurationImportResult();

            try
            {
                _logger.LogInformation("Importing resource configuration for key: {ResourceKey}", resourceKey);

                var grpcResult = await _grpcGateway.ExecuteAsync(
                    new GetResourceConfigurationOperation(),
                    new GetResourceConfigurationRequest(resourceKey),
                    cancellationToken);

                if (!grpcResult.Success || grpcResult.Data?.Resource == null)
                {
                    result.Success = false;
                    result.ErrorMessage = grpcResult.ErrorMessage ?? "Resource not found";
                    return result;
                }

                // Import single resource
                await ImportSingleResourceAsync(grpcResult.Data.Resource, cancellationToken);
                
                result.Success = true;
                result.ResourcesImported = 1;
                result.DataFromCache = grpcResult.FromCache;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import resource configuration for key: {ResourceKey}", resourceKey);
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Test connectivity to execution service
        /// </summary>
        public async Task<bool> TestExecutionServiceConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _grpcGateway.TestServiceConnectionAsync("ExecutionConfigurationService", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to test execution service connection");
                return false;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Fetch current configuration from gRPC service
        /// </summary>
        private async Task<GrpcOperationResult<GetCurrentConfigurationResponse>> FetchCurrentConfigurationAsync(
            bool includeSequences, 
            CancellationToken cancellationToken)
        {
            var request = new GetCurrentConfigurationRequest(includeSequences);
            var operation = new GetCurrentConfigurationOperation();

            return await _grpcGateway.ExecuteAsync(operation, request, cancellationToken);
        }

        /// <summary>
        /// Clear existing data from database
        /// </summary>
        private async Task ClearExistingDataAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Clearing existing configuration data");

            // Clear in dependency order (reverse of foreign key relationships)
            await _sequenceService.ClearAllSequencesAsync(cancellationToken);
            await _resourceService.ClearAllResourcesAsync(cancellationToken);
            await _parameterService.ClearAllParametersAsync(cancellationToken);

            _logger.LogInformation("Existing configuration data cleared");
        }

        /// <summary>
        /// Import configuration data into the database
        /// </summary>
        private async Task ImportConfigurationDataAsync(
            ExecutionConfigurationContract config,
            ConfigurationImportResult result,
            CancellationToken cancellationToken)
        {
            // Import sequences and their associated resources/parameters
            foreach (var sequence in config.Sequences)
            {
                await ImportSingleSequenceAsync(sequence, cancellationToken);
                result.SequencesImported++;

                // Count unique resources and parameters
                result.ResourcesImported += sequence.Resources.Count;
                result.ParametersImported += sequence.Parameters.Count;
            }
        }

        /// <summary>
        /// Import a single sequence with its resources and parameters
        /// </summary>
        private async Task ImportSingleSequenceAsync(
            ExecutionSequenceContract sequenceContract,
            CancellationToken cancellationToken)
        {
            // Convert to domain entity
            var sequence = new Sequence
            {
                Name = sequenceContract.Key,
                // Map other properties based on your domain model
                // You'll need to add these mappings based on your Sequence entity
            };

            // Create or update sequence
            var existingSequence = await _sequenceService.GetSequenceByNameAsync(sequenceContract.Key, cancellationToken);
            if (existingSequence == null)
            {
                await _sequenceService.CreateSequenceAsync(sequence, cancellationToken);
            }
            else
            {
                // Update existing sequence properties
                await _sequenceService.UpdateSequenceAsync(sequence, cancellationToken);
            }

            // Import associated resources
            foreach (var resourceContract in sequenceContract.Resources)
            {
                await ImportSingleResourceAsync(resourceContract, cancellationToken);
                // Create sequence-resource relationship
                // You'll need to implement this based on your domain model
            }

            // Import associated parameters
            foreach (var parameterContract in sequenceContract.Parameters)
            {
                await ImportSingleParameterAsync(parameterContract, sequenceContract.Key, cancellationToken);
            }
        }

        /// <summary>
        /// Import a single resource
        /// </summary>
        private async Task ImportSingleResourceAsync(
            ExecutionResourceContract resourceContract,
            CancellationToken cancellationToken)
        {
            var resource = new Resource
            {
                Name = resourceContract.Key,
                // Map other properties based on your domain model
            };

            var existingResource = await _resourceService.GetResourceByNameAsync(resourceContract.Key, cancellationToken);
            if (existingResource == null)
            {
                await _resourceService.CreateResourceAsync(resource, cancellationToken);
            }
            else
            {
                await _resourceService.UpdateResourceAsync(resource, cancellationToken);
            }
        }

        /// <summary>
        /// Import a single parameter
        /// </summary>
        private async Task ImportSingleParameterAsync(
            SequenceParameterTypeContract parameterContract,
            string sequenceKey,
            CancellationToken cancellationToken)
        {
            var parameter = new Parameter
            {
                Name = parameterContract.ParameterName,
                ParameterType = MapParameterType(parameterContract.ParameterType),
                // Map other properties based on your domain model
            };

            var existingParameter = await _parameterService.GetParameterByNameAsync(parameterContract.ParameterName, cancellationToken);
            if (existingParameter == null)
            {
                await _parameterService.CreateParameterAsync(parameter, cancellationToken);
            }
            else
            {
                await _parameterService.UpdateParameterAsync(parameter, cancellationToken);
            }
        }

        /// <summary>
        /// Map gRPC parameter type to domain parameter type
        /// </summary>
        private Entities.Enums.ParameterType MapParameterType(ParameterType grpcParameterType)
        {
            return grpcParameterType switch
            {
                ParameterType.StringType => Entities.Enums.ParameterType.String,
                ParameterType.IntegerType => Entities.Enums.ParameterType.Integer,
                ParameterType.DecimalType => Entities.Enums.ParameterType.Decimal,
                ParameterType.BooleanType => Entities.Enums.ParameterType.Boolean,
                ParameterType.ArrayType => Entities.Enums.ParameterType.Array,
                ParameterType.EnumType => Entities.Enums.ParameterType.Enum,
                _ => Entities.Enums.ParameterType.String
            };
        }

        #endregion
    }
}

// =====================================================================================
// 5. STARTUP INTEGRATION SERVICE
// =====================================================================================

namespace Instrument.Data.Services
{
    /// <summary>
    /// Service that handles configuration import during application startup
    /// </summary>
    public interface IStartupConfigurationService
    {
        /// <summary>
        /// Import configuration during application startup
        /// </summary>
        Task ImportConfigurationOnStartupAsync(CancellationToken cancellationToken = default);
    }

    public class StartupConfigurationService : IStartupConfigurationService
    {
        private readonly IConfigurationOrchestrator _orchestrator;
        private readonly ILogger<StartupConfigurationService> _logger;
        private readonly ConfigurationOrchestratorOptions _options;

        public StartupConfigurationService(
            IConfigurationOrchestrator orchestrator,
            ILogger<StartupConfigurationService> logger,
            IOptions<ConfigurationOrchestratorOptions> options)
        {
            _orchestrator = orchestrator;
            _logger = logger;
            _options = options.Value;
        }

        public async Task ImportConfigurationOnStartupAsync(CancellationToken cancellationToken = default)
        {
            if (!_options.InitializeDatabaseOnStartup)
            {
                _logger.LogInformation("Database initialization on startup is disabled");
                return;
            }

            try
            {
                _logger.LogInformation("Starting configuration import on application startup");

                // Test connection first
                var isConnected = await _orchestrator.TestExecutionServiceConnectionAsync(cancellationToken);
                if (!isConnected)
                {
                    _logger.LogWarning("Execution service is not available during startup. Skipping configuration import.");
                    return;
                }

                // Import current configuration with sequences
                var result = await _orchestrator.ImportCurrentConfigurationAsync(
                    includeSequences: true,
                    clearExistingData: true,
                    cancellationToken: cancellationToken);

                if (result.Success)
                {
                    _logger.LogInformation("Startup configuration import completed successfully. " +
                        "Sequences: {SeqCount}, Resources: {ResCount}, Parameters: {ParamCount}, " +
                        "ExecutionTime: {ExecutionTime}ms, RequestId: {RequestId}",
                        result.SequencesImported, result.ResourcesImported, result.ParametersImported,
                        result.ExecutionTime.TotalMilliseconds, result.RequestId);
                }
                else
                {
                    _logger.LogError("Startup configuration import failed: {ErrorMessage}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import configuration during startup");
                // Don't throw - let the application continue without configuration
            }
        }
    }
}

// =====================================================================================
// 6. DEPENDENCY INJECTION SETUP
// =====================================================================================

namespace Instrument.Data.Extensions
{
    public static class ConfigurationOrchestratorExtensions
    {
        /// <summary>
        /// Register configuration orchestrator services
        /// </summary>
        public static IServiceCollection AddConfigurationOrchestrator(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register options
            services.Configure<ConfigurationOrchestratorOptions>(
                configuration.GetSection(ConfigurationOrchestratorOptions.SectionName));

            // Register orchestrator and startup service
            services.AddScoped<IConfigurationOrchestrator, ConfigurationOrchestrator>();
            services.AddScoped<IStartupConfigurationService, StartupConfigurationService>();

            return services;
        }
    }
}

// =====================================================================================
// 7. CONFIGURATION EXAMPLE (appsettings.json)
// =====================================================================================

/*
{
  "GrpcGateway": {
    "Services": {
      "ExecutionConfigurationService": {
        "BaseAddress": "https://execution-service:5001",
        "TimeoutSeconds": 120,
        "UseSecureConnection": true
      }
    }
  },
  "ConfigurationOrchestrator": {
    "ValidateDataBeforeInsert": true,
    "InitializeDatabaseOnStartup": true,
    "MaxRetryAttempts": 3,
    "ImportTimeout": "00:05:00"
  }
}
*/

// =====================================================================================
// 8. USAGE IN PROGRAM.CS / STARTUP
// =====================================================================================

/*
// In Program.cs or Startup.cs

// Register all services
services.AddGrpcGateway(configuration);
services.AddConfigurationOrchestrator(configuration);
services.AddSchedulerDataServices(configuration); // Your existing services

// In Main method or during application startup
public static async Task Main(string[] args)
{
    var host = CreateHostBuilder(args).Build();
    
    // Import configuration on startup
    using (var scope = host.Services.CreateScope())
    {
        var startupService = scope.ServiceProvider.GetRequiredService<IStartupConfigurationService>();
        await startupService.ImportConfigurationOnStartupAsync();
    }
    
    await host.RunAsync();
}

// Or in a hosted service for more control
public class ConfigurationImportHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    
    public ConfigurationImportHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var startupService = scope.ServiceProvider.GetRequiredService<IStartupConfigurationService>();
        await startupService.ImportConfigurationOnStartupAsync(cancellationToken);
    }
    
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
*/