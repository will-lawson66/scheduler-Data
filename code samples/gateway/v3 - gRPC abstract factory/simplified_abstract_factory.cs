// =====================================================================================
// FIXED ABSTRACT FACTORY FOR gRPC OPERATIONS
// Corrected delegate signature to match actual service interface
// =====================================================================================

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Instrument.Data.ExecutionService.Contracts;

// =====================================================================================
// 1. CORE OPERATION ABSTRACTION - FIXED DELEGATE
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

/*
ABSTRACT FACTORY PATTERN BENEFITS:

✅ EXTENSIBILITY: Easy to add new services by implementing IGrpcOperationFactory
✅ POLYMORPHISM: All factories share common interface, can be treated uniformly  
✅ SCALABLE DI: Gateway constructor grows predictably with new service factories
✅ CONSISTENT API: Same pattern for creating operations across all services
✅ TESTABILITY: Each factory can be mocked independently
✅ SEPARATION OF CONCERNS: Each factory is responsible for one service
✅ TYPE SAFETY: All operations are strongly typed

USAGE PATTERN:
var operation = _gateway.ExecutionConfigurationOperations.CreateXXXOperation();
var result = await _gateway.ExecuteAsync(operation, request);

// OR for different service:
var operation = _gateway.ReportingOperations.CreateXXXOperation();
var result = await _gateway.ExecuteAsync(operation, request);

This provides clean extensibility - each new service just adds one more factory!
*/
        string OperationName { get; }
        Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Operation implementation that wraps service methods correctly
    /// </summary>
    public class GrpcOperation<TRequest, TResponse> : IGrpcOperation<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        private readonly Func<TRequest, Task<TResponse>> _serviceMethod;
        
        public GrpcOperation(
            string serviceName,
            string operationName,
            TimeSpan timeout,
            Func<TRequest, Task<TResponse>> serviceMethod)
        {
            ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            OperationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
            Timeout = timeout;
            _serviceMethod = serviceMethod ?? throw new ArgumentNullException(nameof(serviceMethod));
        }
        
        public string ServiceName { get; }
        public string OperationName { get; }
        public TimeSpan Timeout { get; }
        
        public Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken)
            => _serviceMethod(request);
    }
}

// =====================================================================================
// 2. ABSTRACT FACTORY - USING CORRECT DTOs
// =====================================================================================

namespace Instrument.Data.Gateway.Factories
{
    using Instrument.Data.Gateway.Operations;

    /// <summary>
    /// Abstract factory for ExecutionConfigurationService operations
    /// Using correct Fetch...Request/Response DTOs
    /// </summary>
    public interface IExecutionConfigurationOperationFactory
    {
        string ServiceName { get; }
        
        IGrpcOperation<FetchExecutionConfigurationRequest, FetchExecutionConfigurationResponse> 
            CreateGetCurrentConfigurationOperation();
            
        IGrpcOperation<FetchSequenceConfigurationRequest, FetchSequenceConfigurationResponse> 
            CreateGetSequenceConfigurationOperation();
            
        IGrpcOperation<FetchResourceConfigurationRequest, FetchResourceConfigurationResponse> 
            CreateGetResourceConfigurationOperation();
    }

    /// <summary>
    /// Concrete factory with correct delegate signatures
    /// </summary>
    public class ExecutionConfigurationOperationFactory : IExecutionConfigurationOperationFactory
    {
        private readonly IExecutionConfigurationService _service;
        
        public ExecutionConfigurationOperationFactory(IExecutionConfigurationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }
        
        public string ServiceName => "ExecutionConfigurationService";
        
        public IGrpcOperation<FetchExecutionConfigurationRequest, FetchExecutionConfigurationResponse> 
            CreateGetCurrentConfigurationOperation()
        {
            return new GrpcOperation<FetchExecutionConfigurationRequest, FetchExecutionConfigurationResponse>(
                ServiceName,
                "GetCurrentConfiguration",
                TimeSpan.FromSeconds(30),
                _service.GetCurrentConfigurationAsync);
        }
        
        public IGrpcOperation<FetchSequenceConfigurationRequest, FetchSequenceConfigurationResponse> 
            CreateGetSequenceConfigurationOperation()
        {
            return new GrpcOperation<FetchSequenceConfigurationRequest, FetchSequenceConfigurationResponse>(
                ServiceName,
                "GetSequenceConfiguration",
                TimeSpan.FromSeconds(30),
                _service.GetSequenceConfigurationAsync);
        }
        
        public IGrpcOperation<FetchResourceConfigurationRequest, FetchResourceConfigurationResponse> 
            CreateGetResourceConfigurationOperation()
        {
            return new GrpcOperation<FetchResourceConfigurationRequest, FetchResourceConfigurationResponse>(
                ServiceName,
                "GetResourceConfiguration",
                TimeSpan.FromSeconds(30),
                _service.GetResourceConfigurationAsync);
        }
        public IGrpcOperation<FetchSequenceConfigurationRequest, FetchSequenceConfigurationResponse> 
            CreateGetSequenceConfigurationOperation()
        {
            return new GrpcOperation<FetchSequenceConfigurationRequest, FetchSequenceConfigurationResponse>(
                ServiceName,
                "GetSequenceConfiguration",
                TimeSpan.FromSeconds(30),
                request => _service.GetSequenceConfigurationAsync(request));
        }
        
        public IGrpcOperation<FetchResourceConfigurationRequest, FetchResourceConfigurationResponse> 
            CreateGetResourceConfigurationOperation()
        {
            return new GrpcOperation<FetchResourceConfigurationRequest, FetchResourceConfigurationResponse>(
                ServiceName,
                "GetResourceConfiguration",
                TimeSpan.FromSeconds(30),
                request => _service.GetResourceConfigurationAsync(request));
        }
    }
}

// =====================================================================================
// 3. GATEWAY - UNCHANGED
// =====================================================================================

namespace Instrument.Data.Gateway.Core
{
    using Instrument.Data.Gateway.Operations;
    using Instrument.Data.Gateway.Factories;

    public interface IGrpcGateway
    {
        Task<GatewayResult<TResponse>> ExecuteAsync<TRequest, TResponse>(
            IGrpcOperation<TRequest, TResponse> operation,
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class;
            
        IExecutionConfigurationOperationFactory ExecutionConfigurationOperations { get; }
    }

    public class GrpcGateway : IGrpcGateway, IDisposable
    {
        private readonly ILogger<GrpcGateway> _logger;
        private readonly SemaphoreSlim _semaphore;
        private volatile bool _disposed;

        public GrpcGateway(
            IExecutionConfigurationOperationFactory executionConfigFactory,
            ILogger<GrpcGateway> logger,
            GrpcGatewayOptions options)
        {
            ExecutionConfigurationOperations = executionConfigFactory ?? throw new ArgumentNullException(nameof(executionConfigFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var optionsValue = options ?? throw new ArgumentNullException(nameof(options));
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
// 4. USAGE EXAMPLE - WITH CORRECT DTOs
// =====================================================================================

namespace Instrument.Data.Orchestration.Steps
{
    using Instrument.Data.Gateway.Core;

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
            try
            {
                var operation = _gateway.ExecutionConfigurationOperations
                    .CreateGetCurrentConfigurationOperation();
                
                // Using correct DTO names
                var grpcRequest = new FetchExecutionConfigurationRequest(/* constructor params */);
                
                var result = await _gateway.ExecuteAsync(operation, grpcRequest, cancellationToken);

                if (!result.IsSuccess)
                {
                    return StepResult.Failure($"Failed to fetch configuration: {result.ErrorMessage}");
                }

                context.SetData("FetchedConfiguration", result.Data);
                
                _logger.LogInformation("Configuration fetched successfully");
                
                return StepResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch configuration");
                return StepResult.Failure($"Failed to fetch configuration: {ex.Message}");
            }
        }
    }
}

// =====================================================================================
// 5. SUPPORTING TYPES
// =====================================================================================

public class GrpcGatewayOptions
{
    public int MaxConcurrentRequests { get; set; } = 10;
}

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

public interface IOrchestrationStep
{
    string StepName { get; }
    Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken);
}

public class StepResult
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    
    public static StepResult Success() => new() { IsSuccess = true };
    public static StepResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

public class OrchestrationContext
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> _data = new();
    
    public T GetData<T>(string key) => _data.TryGetValue(key, out var value) ? (T)value : default;
    public void SetData<T>(string key, T value) => _data.AddOrUpdate(key, value, (k, v) => value);
}

// Service interfaces (would normally be in separate contracts assembly)
public interface IExecutionConfigurationService
{
    Task<FetchExecutionConfigurationResponse> GetCurrentConfigurationAsync(FetchExecutionConfigurationRequest request);
    Task<FetchSequenceConfigurationResponse> GetSequenceConfigurationAsync(FetchSequenceConfigurationRequest request);
    Task<FetchResourceConfigurationResponse> GetResourceConfigurationAsync(FetchResourceConfigurationRequest request);
}

public interface IReportingService  
{
    Task<FetchReportResponse> GetReportAsync(FetchReportRequest request);
    Task<GenerateReportResponse> GenerateReportAsync(GenerateReportRequest request);
}

// Request/Response DTOs (would normally be in separate contracts assembly)
public record FetchExecutionConfigurationRequest();
public record FetchExecutionConfigurationResponse();
public record FetchSequenceConfigurationRequest();
public record FetchSequenceConfigurationResponse();
public record FetchResourceConfigurationRequest();
public record FetchResourceConfigurationResponse();
public record FetchReportRequest();
public record FetchReportResponse();
public record GenerateReportRequest();
public record GenerateReportResponse();