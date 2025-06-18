// =====================================================================================
// CLEAN ABSTRACT FACTORY FOR gRPC OPERATIONS
// Focused on ExecutionConfigurationService operations without unnecessary abstractions
// =====================================================================================

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Instrument.Data.ExecutionService.Contracts;

// =====================================================================================
// 1. CORE OPERATION ABSTRACTION
// =====================================================================================

namespace Instrument.Data.Gateway.Operations
{
    /// <summary>
    /// Base interface for all gRPC operations
    /// </summary>
    public interface IGrpcOperation<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        string ServiceName { get; }
        string OperationName { get; }
        Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Generic bound operation implementation
    /// </summary>
    public class BoundGrpcOperation<TRequest, TResponse> : IGrpcOperation<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        private readonly Func<TRequest, CancellationToken, Task<TResponse>> _executor;
        
        public BoundGrpcOperation(
            string serviceName,
            string operationName,
            Func<TRequest, CancellationToken, Task<TResponse>> executor)
        {
            ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }
        
        public string ServiceName { get; }
        public string OperationName { get; }
        
        public Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken)
            => _executor(request, cancellationToken);
    }
}

// =====================================================================================
// 2. ABSTRACT FACTORY DEFINITION
// =====================================================================================

namespace Instrument.Data.Gateway.Factories
{
    using Instrument.Data.Gateway.Operations;

    /// <summary>
    /// Abstract factory for creating gRPC operations for a specific service
    /// </summary>
    public interface IGrpcOperationFactory
    {
        /// <summary>
        /// The name of the service this factory creates operations for
        /// </summary>
        string ServiceName { get; }
        
        /// <summary>
        /// Create a custom operation with the provided executor function
        /// </summary>
        IGrpcOperation<TRequest, TResponse> CreateOperation<TRequest, TResponse>(
            string operationName,
            Func<TRequest, CancellationToken, Task<TResponse>> executor)
            where TRequest : class
            where TResponse : class;
    }

    /// <summary>
    /// Abstract factory specifically for ExecutionConfigurationService operations
    /// </summary>
    public interface IExecutionConfigurationOperationFactory : IGrpcOperationFactory
    {
        /// <summary>
        /// Create operation to get current configuration
        /// </summary>
        IGrpcOperation<GetCurrentConfigurationRequest, GetCurrentConfigurationResponse> 
            CreateGetCurrentConfigurationOperation();
            
        /// <summary>
        /// Create operation to get sequence configuration
        /// </summary>
        IGrpcOperation<GetSequenceConfigurationRequest, GetSequenceConfigurationResponse> 
            CreateGetSequenceConfigurationOperation();
            
        /// <summary>
        /// Create operation to get resource configuration
        /// </summary>
        IGrpcOperation<GetResourceConfigurationRequest, GetResourceConfigurationResponse> 
            CreateGetResourceConfigurationOperation();
    }
}

// =====================================================================================
// 3. CONCRETE FACTORY IMPLEMENTATION
// =====================================================================================

namespace Instrument.Data.Gateway.Factories
{
    using Instrument.Data.Gateway.Operations;

    /// <summary>
    /// Concrete factory for ExecutionConfigurationService operations
    /// </summary>
    public class ExecutionConfigurationOperationFactory : IExecutionConfigurationOperationFactory
    {
        private readonly IExecutionConfigurationService _service;
        
        public ExecutionConfigurationOperationFactory(IExecutionConfigurationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }
        
        public string ServiceName => "ExecutionConfigurationService";
        
        public IGrpcOperation<TRequest, TResponse> CreateOperation<TRequest, TResponse>(
            string operationName,
            Func<TRequest, CancellationToken, Task<TResponse>> executor)
            where TRequest : class
            where TResponse : class
        {
            return new BoundGrpcOperation<TRequest, TResponse>(ServiceName, operationName, executor);
        }
        
        public IGrpcOperation<GetCurrentConfigurationRequest, GetCurrentConfigurationResponse> 
            CreateGetCurrentConfigurationOperation()
        {
            return CreateOperation<GetCurrentConfigurationRequest, GetCurrentConfigurationResponse>(
                "GetCurrentConfiguration",
                _service.GetCurrentConfigurationAsync);
        }
        
        public IGrpcOperation<GetSequenceConfigurationRequest, GetSequenceConfigurationResponse> 
            CreateGetSequenceConfigurationOperation()
        {
            return CreateOperation<GetSequenceConfigurationRequest, GetSequenceConfigurationResponse>(
                "GetSequenceConfiguration",
                _service.GetSequenceConfigurationAsync);
        }
        
        public IGrpcOperation<GetResourceConfigurationRequest, GetResourceConfigurationResponse> 
            CreateGetResourceConfigurationOperation()
        {
            return CreateOperation<GetResourceConfigurationRequest, GetResourceConfigurationResponse>(
                "GetResourceConfiguration",
                _service.GetResourceConfigurationAsync);
        }
    }
}

// =====================================================================================
// 4. GATEWAY WITH EMBEDDED FACTORY MANAGEMENT
// =====================================================================================

namespace Instrument.Data.Gateway.Core
{
    using Instrument.Data.Gateway.Abstractions;
    using Instrument.Data.Gateway.Operations;
    using Instrument.Data.Gateway.Factories;

    /// <summary>
    /// Gateway interface that exposes operation creation capabilities
    /// </summary>
    public interface IGrpcGateway
    {
        /// <summary>
        /// Execute a gRPC operation
        /// </summary>
        Task<GatewayResult<TResponse>> ExecuteAsync<TRequest, TResponse>(
            IGrpcOperation<TRequest, TResponse> operation,
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class;
            
        /// <summary>
        /// Access to ExecutionConfigurationService operations
        /// </summary>
        IExecutionConfigurationOperationFactory ExecutionConfigurationOperations { get; }
    }

    /// <summary>
    /// Gateway implementation with embedded factory management
    /// </summary>
    public class GrpcGateway : IGrpcGateway, IDisposable
    {
        private readonly ILogger<GrpcGateway> _logger;
        private readonly SemaphoreSlim _semaphore;
        private volatile bool _disposed;

        public GrpcGateway(
            IExecutionConfigurationOperationFactory executionConfigFactory,
            ILogger<GrpcGateway> logger,
            IOptions<GrpcGatewayOptions> options)
        {
            ExecutionConfigurationOperations = executionConfigFactory ?? throw new ArgumentNullException(nameof(executionConfigFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var optionsValue = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _semaphore = new SemaphoreSlim(optionsValue.MaxConcurrentRequests, optionsValue.MaxConcurrentRequests);
        }

        public IExecutionConfigurationOperationFactory ExecutionConfigurationOperations { get; }

        public async Task<GatewayResult<TResponse>> ExecuteAsync<TRequest, TResponse>(
            IGrpcOperation<TRequest, TResponse> operation,
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            ThrowIfDisposed();
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var operationKey = $"{operation.ServiceName}.{operation.OperationName}";
            
            _logger.LogDebug("Executing {OperationKey}", operationKey);

            await _semaphore.WaitAsync(cancellationToken);
            
            try
            {
                var result = await operation.ExecuteAsync(request, cancellationToken);
                
                _logger.LogInformation("Successfully executed {OperationKey} in {Duration}ms",
                    operationKey, stopwatch.ElapsedMilliseconds);

                return GatewayResult<TResponse>.Success(result, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {OperationKey}", operationKey);
                return GatewayResult<TResponse>.Failure(ex.Message, stopwatch.Elapsed);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GrpcGateway));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _semaphore?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}

// =====================================================================================
// 5. ORCHESTRATION STEP IMPLEMENTATIONS
// =====================================================================================

namespace Instrument.Data.Orchestration.Steps
{
    using Instrument.Data.Gateway.Core;
    using Instrument.Data.Orchestration.Abstractions;

    /// <summary>
    /// Step to fetch configuration from ExecutionConfigurationService
    /// Clean implementation with single gateway dependency
    /// </summary>
    public class FetchConfigurationStep : IOrchestrationStep
    {
        private readonly IGrpcGateway _gateway;
        private readonly ILogger<FetchConfigurationStep> _logger;

        public FetchConfigurationStep(
            IGrpcGateway gateway,
            ILogger<FetchConfigurationStep> logger)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string StepName => "FetchConfiguration";

        public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            var request = context.GetData<ConfigurationImportRequest>("ImportRequest");

            try
            {
                // Create operation using the factory through gateway - no dependency drilling!
                var operation = _gateway.ExecutionConfigurationOperations
                    .CreateGetCurrentConfigurationOperation();
                
                var grpcRequest = new GetCurrentConfigurationRequest(request.IncludeSequences);
                
                var result = await _gateway.ExecuteAsync(operation, grpcRequest, cancellationToken);

                if (!result.IsSuccess)
                {
                    return StepResult.Failure($"Failed to fetch configuration: {result.ErrorMessage}");
                }

                if (result.Data?.Configuration == null)
                {
                    return StepResult.Failure("Configuration is null in response");
                }

                // Store the fetched configuration and metadata in context
                context.SetData("FetchedConfiguration", result.Data.Configuration);
                context.SetData("RequestId", $"{result.Data.RequestId.Lo}-{result.Data.RequestId.Hi}");
                
                _logger.LogInformation("Configuration fetched successfully. Sequences: {SequenceCount}", 
                    result.Data.Configuration.Sequences.Count);
                
                return StepResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch configuration from ExecutionConfigurationService");
                return StepResult.Failure($"Failed to fetch configuration: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Step to fetch specific sequence configuration
    /// Demonstrates reusable operation pattern
    /// </summary>
    public class FetchSequenceStep : IOrchestrationStep
    {
        private readonly IGrpcGateway _gateway;
        private readonly ILogger<FetchSequenceStep> _logger;

        public FetchSequenceStep(
            IGrpcGateway gateway,
            ILogger<FetchSequenceStep> logger)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string StepName => "FetchSequence";

        public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            var sequenceKey = context.GetData<string>("SequenceKey");
            if (string.IsNullOrEmpty(sequenceKey))
            {
                return StepResult.Failure("SequenceKey not found in context");
            }

            try
            {
                // Same pattern - use factory through gateway
                var operation = _gateway.ExecutionConfigurationOperations
                    .CreateGetSequenceConfigurationOperation();
                
                var grpcRequest = new GetSequenceConfigurationRequest(sequenceKey);
                
                var result = await _gateway.ExecuteAsync(operation, grpcRequest, cancellationToken);

                if (!result.IsSuccess)
                {
                    return StepResult.Failure($"Failed to fetch sequence {sequenceKey}: {result.ErrorMessage}");
                }

                context.SetData($"FetchedSequence_{sequenceKey}", result.Data.Sequence);
                
                _logger.LogInformation("Sequence {SequenceKey} fetched successfully", sequenceKey);
                
                return StepResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch sequence {SequenceKey}", sequenceKey);
                return StepResult.Failure($"Failed to fetch sequence {sequenceKey}: {ex.Message}");
            }
        }
    }
}

// =====================================================================================
// 6. DEPENDENCY INJECTION SETUP
// =====================================================================================

namespace Instrument.Data.Extensions
{
    using Instrument.Data.Gateway.Core;
    using Instrument.Data.Gateway.Factories;
    using Instrument.Data.Orchestration.Steps;

    public static class AbstractFactoryGatewayExtensions
    {
        /// <summary>
        /// Register Abstract Factory pattern services for gRPC gateway
        /// </summary>
        public static IServiceCollection AddAbstractFactoryGrpcGateway(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Gateway configuration
            services.Configure<GrpcGatewayOptions>(
                configuration.GetSection(GrpcGatewayOptions.SectionName));

            // Register concrete factories
            services.AddScoped<IExecutionConfigurationOperationFactory, ExecutionConfigurationOperationFactory>();

            // Register gateway with factory management
            services.AddScoped<IGrpcGateway, GrpcGateway>();

            // Register orchestration steps
            services.AddScoped<IOrchestrationStep, FetchConfigurationStep>();
            services.AddScoped<IOrchestrationStep, FetchSequenceStep>();

            return services;
        }

        /// <summary>
        /// Register ExecutionConfigurationService gRPC client
        /// </summary>
        public static IServiceCollection AddExecutionConfigurationServiceClient(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register your actual gRPC client here
            // Example for ProtoBuf.Grpc:
            /*
            services.AddCodeFirstGrpcClient<IExecutionConfigurationService>(options =>
            {
                options.Address = new Uri(configuration["ExecutionService:BaseAddress"]);
            });
            */

            // For demonstration, register a mock service
            services.AddScoped<IExecutionConfigurationService, MockExecutionConfigurationService>();

            return services;
        }
    }

    /// <summary>
    /// Mock service for demonstration purposes
    /// Replace with your actual gRPC client registration
    /// </summary>
    internal class MockExecutionConfigurationService : IExecutionConfigurationService
    {
        public Task<GetCurrentConfigurationResponse> GetCurrentConfigurationAsync(
            GetCurrentConfigurationRequest request, 
            CancellationToken cancellationToken = default)
        {
            var mockResponse = new GetCurrentConfigurationResponse(
                RequestId: new GuidRequestId("12345", "67890"),
                Configuration: new ExecutionConfigurationContract(
                    StartingPeriod: 1,
                    RolloverPeriod: 100,
                    PeriodSpan: TimeSpan.FromMinutes(1),
                    PeriodAcceleration: 1.0,
                    Sequences: new List<ExecutionSequenceContract>
                    {
                        new("TestSequence1", TimeSpan.FromSeconds(30), "Standard", "script1", 
                            new List<ExecutionResourceContract>
                            {
                                new("Resource1", true, "StandardInterface")
                            },
                            new List<SequenceParameterTypeContract>
                            {
                                new("Param1", ParameterType.StringType)
                            })
                    }
                ),
                Errors: new List<GrpcErrorContract>()
            );

            return Task.FromResult(mockResponse);
        }

        public Task<GetSequenceConfigurationResponse> GetSequenceConfigurationAsync(
            GetSequenceConfigurationRequest request, 
            CancellationToken cancellationToken = default)
        {
            var mockResponse = new GetSequenceConfigurationResponse(
                RequestId: new GuidRequestId("54321", "09876"),
                Sequence: new ExecutionSequenceContract(
                    request.Key, 
                    TimeSpan.FromSeconds(45), 
                    "Advanced", 
                    "script2",
                    new List<ExecutionResourceContract>(),
                    new List<SequenceParameterTypeContract>()),
                Errors: new List<GrpcErrorContract>()
            );

            return Task.FromResult(mockResponse);
        }

        public Task<GetResourceConfigurationResponse> GetResourceConfigurationAsync(
            GetResourceConfigurationRequest request, 
            CancellationToken cancellationToken = default)
        {
            var mockResponse = new GetResourceConfigurationResponse(
                RequestId: new GuidRequestId("11111", "22222"),
                Resource: new ExecutionResourceContract(request.Key, true, "MockInterface"),
                Errors: new List<GrpcErrorContract>()
            );

            return Task.FromResult(mockResponse);
        }
    }
}

// =====================================================================================
// 7. USAGE EXAMPLE
// =====================================================================================

namespace Instrument.Data.Examples
{
    using Instrument.Data.Gateway.Core;

    public class AbstractFactoryUsageExample
    {
        private readonly IGrpcGateway _gateway;

        public AbstractFactoryUsageExample(IGrpcGateway gateway)
        {
            _gateway = gateway;
        }

        /// <summary>
        /// Demonstrate clean operation creation and execution
        /// </summary>
        public async Task FetchConfigurationDataAsync()
        {
            // Create operations using the factory - clean and reusable
            var getCurrentConfigOp = _gateway.ExecutionConfigurationOperations
                .CreateGetCurrentConfigurationOperation();
                
            var getSequenceOp = _gateway.ExecutionConfigurationOperations
                .CreateGetSequenceConfigurationOperation();

            // Execute operations through the gateway
            var configResult = await _gateway.ExecuteAsync(
                getCurrentConfigOp, 
                new GetCurrentConfigurationRequest(true));

            if (configResult.IsSuccess)
            {
                Console.WriteLine($"Fetched configuration with {configResult.Data.Configuration.Sequences.Count} sequences");

                // Fetch details for each sequence
                foreach (var sequence in configResult.Data.Configuration.Sequences)
                {
                    var sequenceResult = await _gateway.ExecuteAsync(
                        getSequenceOp,
                        new GetSequenceConfigurationRequest(sequence.Key));

                    if (sequenceResult.IsSuccess)
                    {
                        Console.WriteLine($"Fetched details for sequence: {sequence.Key}");
                    }
                }
            }
        }
    }
}

// =====================================================================================
// 8. SUPPORTING TYPES
// =====================================================================================

// Gateway configuration
public class GrpcGatewayOptions
{
    public const string SectionName = "GrpcGateway";
    public int MaxConcurrentRequests { get; set; } = 10;
}

// Gateway result
public class GatewayResult<T>
{
    public bool IsSuccess { get; set; }
    public T Data { get; set; }
    public string ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
    
    public static GatewayResult<T> Success(T data, TimeSpan duration) =>
        new() { IsSuccess = true, Data = data, Duration = duration };
        
    public static GatewayResult<T> Failure(string error, TimeSpan duration) =>
        new() { IsSuccess = false, ErrorMessage = error, Duration = duration };
}

// Orchestration types
public interface IOrchestrationStep
{
    string StepName { get; }
    Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken);
}

public class StepResult
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public bool ShouldContinue { get; set; } = true;
    
    public static StepResult Success() => new() { IsSuccess = true };
    public static StepResult Failure(string error, bool shouldContinue = false) => 
        new() { IsSuccess = false, ErrorMessage = error, ShouldContinue = shouldContinue };
}

public class OrchestrationContext
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> _data = new();
    
    public T GetData<T>(string key) => _data.TryGetValue(key, out var value) ? (T)value : default;
    public void SetData<T>(string key, T value) => _data.AddOrUpdate(key, value, (k, v) => value);
}

public class ConfigurationImportRequest
{
    public bool IncludeSequences { get; set; } = true;
    public bool ClearExistingData { get; set; } = false;
}

/*
ABSTRACT FACTORY BENEFITS:

✅ CLEAN DEPENDENCIES: Steps only depend on IGrpcGateway
✅ NO DEPENDENCY DRILLING: Factory is accessed through gateway property
✅ REUSABLE OPERATIONS: Operations are created consistently and can be reused
✅ TYPE SAFETY: All operations are strongly typed
✅ EXTENSIBLE: Easy to add new services by implementing IGrpcOperationFactory
✅ TESTABLE: Factory can be mocked, operations can be tested independently
✅ SINGLE RESPONSIBILITY: Gateway manages execution, factory manages creation

USAGE PATTERN:
var operation = _gateway.ExecutionConfigurationOperations.CreateXXXOperation();
var result = await _gateway.ExecuteAsync(operation, request);

This provides the cleanest separation of concerns while maintaining reusability!
*/