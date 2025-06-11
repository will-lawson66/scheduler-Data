// =====================================================================================
// FOUR gRPC OPERATION PATTERNS COMPARISON
// Comparing different approaches to avoid service dependencies in operations
// =====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Instrument.Data.ExecutionService.Contracts;
// =====================================================================================
// PATTERN 1: DELEGATE/FUNCTION INJECTION PATTERN
// Operations take functions instead of services
// =====================================================================================
namespace Pattern1_DelegateInjection
{
    // Core operation interface
    public interface IGrpcOperation<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        string ServiceName { get; }
        string OperationName { get; }
        Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken);
    }
    // Operation implementation with function injection
    public class GetCurrentConfigurationOperation : IGrpcOperation<GetCurrentConfigurationRequest, GetCurrentConfigurationResponse>
    {
        private readonly Func<GetCurrentConfigurationRequest, CancellationToken, Task<GetCurrentConfigurationResponse>> _executor;
        
        public GetCurrentConfigurationOperation(
            Func<GetCurrentConfigurationRequest, CancellationToken, Task<GetCurrentConfigurationResponse>> executor)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }
        
        public string ServiceName => "ExecutionConfigurationService";
        public string OperationName => "GetCurrentConfiguration";
        
        public Task<GetCurrentConfigurationResponse> ExecuteAsync(
            GetCurrentConfigurationRequest request, 
            CancellationToken cancellationToken)
        {
            return _executor(request, cancellationToken);
        }
    }
    // Gateway creates operations with bound functions
    public interface IGrpcGateway
    {
        Task<GatewayResult<TResponse>> ExecuteAsync<TRequest, TResponse>(
            IGrpcOperation<TRequest, TResponse> operation,
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class;
            
        // Factory methods for creating operations
        GetCurrentConfigurationOperation CreateGetCurrentConfigurationOperation();
        GetSequenceConfigurationOperation CreateGetSequenceConfigurationOperation();
    }
    public class GrpcGateway : IGrpcGateway
    {
        private readonly IExecutionConfigurationService _executionService;
        private readonly ILogger<GrpcGateway> _logger;
        
        public GrpcGateway(
            IExecutionConfigurationService executionService,
            ILogger<GrpcGateway> logger)
        {
            _executionService = executionService;
            _logger = logger;
        }
        
        public GetCurrentConfigurationOperation CreateGetCurrentConfigurationOperation()
        {
            return new GetCurrentConfigurationOperation(_executionService.GetCurrentConfigurationAsync);
        }
        
        public GetSequenceConfigurationOperation CreateGetSequenceConfigurationOperation()
        {
            return new GetSequenceConfigurationOperation(_executionService.GetSequenceConfigurationAsync);
        }
        
        public async Task<GatewayResult<TResponse>> ExecuteAsync<TRequest, TResponse>(
            IGrpcOperation<TRequest, TResponse> operation,
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.LogDebug("Executing {ServiceName}.{OperationName}", 
                    operation.ServiceName, operation.OperationName);
                
                var result = await operation.ExecuteAsync(request, cancellationToken);
                
                _logger.LogInformation("Successfully executed {ServiceName}.{OperationName} in {Duration}ms",
                    operation.ServiceName, operation.OperationName, stopwatch.ElapsedMilliseconds);
                
                return GatewayResult<TResponse>.Success(result, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {ServiceName}.{OperationName}",
                    operation.ServiceName, operation.OperationName);
                
                return GatewayResult<TResponse>.Failure(ex.Message, stopwatch.Elapsed);
            }
        }
    }
    // Additional operation for completeness
    public class GetSequenceConfigurationOperation : IGrpcOperation<GetSequenceConfigurationRequest, GetSequenceConfigurationResponse>
    {
        private readonly Func<GetSequenceConfigurationRequest, CancellationToken, Task<GetSequenceConfigurationResponse>> _executor;
        
        public GetSequenceConfigurationOperation(
            Func<GetSequenceConfigurationRequest, CancellationToken, Task<GetSequenceConfigurationResponse>> executor)
        {
            _executor = executor;
        }
        
        public string ServiceName => "ExecutionConfigurationService";
        public string OperationName => "GetSequenceConfiguration";
        
        public Task<GetSequenceConfigurationResponse> ExecuteAsync(
            GetSequenceConfigurationRequest request, 
            CancellationToken cancellationToken)
        {
            return _executor(request, cancellationToken);
        }
    }
    // Orchestration step usage
    public class FetchConfigurationStep : IOrchestrationStep
    {
        private readonly IGrpcGateway _gateway;
        
        public FetchConfigurationStep(IGrpcGateway gateway)
        {
            _gateway = gateway;
        }
        
        public string StepName => "FetchConfiguration";
        
        public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            var request = context.GetData<ConfigurationImportRequest>("ImportRequest");
            
            // Create operation with bound function
            var operation = _gateway.CreateGetCurrentConfigurationOperation();
            var grpcRequest = new GetCurrentConfigurationRequest(request.IncludeSequences);
            
            var result = await _gateway.ExecuteAsync(operation, grpcRequest, cancellationToken);
            
            if (!result.IsSuccess)
            {
                return StepResult.Failure($"Failed to fetch configuration: {result.ErrorMessage}");
            }
            
            context.SetData("FetchedConfiguration", result.Data.Configuration);
            return StepResult.Success();
        }
    }
    // DI Registration
    public static class ServiceRegistration
    {
        public static IServiceCollection AddPattern1Services(this IServiceCollection services)
        {
            services.AddScoped<IGrpcGateway, GrpcGateway>();
            services.AddScoped<IOrchestrationStep, FetchConfigurationStep>();
            return services;
        }
    }
    /*
    PROS:
    - Operations are reusable (no service dependencies)
    - Clear separation of concerns
    - Easy to test (inject mock functions)
    - Type-safe
    
    CONS:
    - Gateway interface grows with factory methods
    - Slightly more complex creation patterns
    - Function signatures can become unwieldy
    */
}
// =====================================================================================
// PATTERN 2: MEDIATOR PATTERN
// Operations are pure data, gateway routes based on type
// =====================================================================================
namespace Pattern2_Mediator
{
    // Request marker interface
    public interface IGrpcRequest<TResponse>
    {
        string ServiceName { get; }
        string OperationName { get; }
    }
    // Operations are pure data records
    public record GetCurrentConfigurationOperation(
        GetCurrentConfigurationRequest Request) : IGrpcRequest<GetCurrentConfigurationResponse>
    {
        public string ServiceName => "ExecutionConfigurationService";
        public string OperationName => "GetCurrentConfiguration";
    }
    public record GetSequenceConfigurationOperation(
        GetSequenceConfigurationRequest Request) : IGrpcRequest<GetSequenceConfigurationResponse>
    {
        public string ServiceName => "ExecutionConfigurationService";
        public string OperationName => "GetSequenceConfiguration";
    }
    // Gateway as mediator
    public interface IGrpcGateway
    {
        Task<GatewayResult<TResponse>> ExecuteAsync<TResponse>(
            IGrpcRequest<TResponse> request, 
            CancellationToken cancellationToken = default);
    }
    public class GrpcGateway : IGrpcGateway
    {
        private readonly IExecutionConfigurationService _executionService;
        private readonly ILogger<GrpcGateway> _logger;
        
        public GrpcGateway(
            IExecutionConfigurationService executionService,
            ILogger<GrpcGateway> logger)
        {
            _executionService = executionService;
            _logger = logger;
        }
        
        public async Task<GatewayResult<TResponse>> ExecuteAsync<TResponse>(
            IGrpcRequest<TResponse> request, 
            CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.LogDebug("Executing {ServiceName}.{OperationName}", 
                    request.ServiceName, request.OperationName);
                
                var result = await DispatchRequest(request, cancellationToken);
                
                _logger.LogInformation("Successfully executed {ServiceName}.{OperationName} in {Duration}ms",
                    request.ServiceName, request.OperationName, stopwatch.ElapsedMilliseconds);
                
                return GatewayResult<TResponse>.Success(result, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {ServiceName}.{OperationName}",
                    request.ServiceName, request.OperationName);
                
                return GatewayResult<TResponse>.Failure(ex.Message, stopwatch.Elapsed);
            }
        }
        
        private async Task<TResponse> DispatchRequest<TResponse>(
            IGrpcRequest<TResponse> request, 
            CancellationToken cancellationToken)
        {
            return request switch
            {
                GetCurrentConfigurationOperation op => 
                    (TResponse)(object)await _executionService.GetCurrentConfigurationAsync(op.Request, cancellationToken),
                GetSequenceConfigurationOperation op => 
                    (TResponse)(object)await _executionService.GetSequenceConfigurationAsync(op.Request, cancellationToken),
                _ => throw new NotSupportedException($"Operation {request.GetType().Name} not supported")
            };
        }
    }
    // Orchestration step usage
    public class FetchConfigurationStep : IOrchestrationStep
    {
        private readonly IGrpcGateway _gateway;
        
        public FetchConfigurationStep(IGrpcGateway gateway)
        {
            _gateway = gateway;
        }
        
        public string StepName => "FetchConfiguration";
        
        public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            var request = context.GetData<ConfigurationImportRequest>("ImportRequest");
            
            // Pure data operation - no service dependencies
            var operation = new GetCurrentConfigurationOperation(
                new GetCurrentConfigurationRequest(request.IncludeSequences));
            
            var result = await _gateway.ExecuteAsync(operation, cancellationToken);
            
            if (!result.IsSuccess)
            {
                return StepResult.Failure($"Failed to fetch configuration: {result.ErrorMessage}");
            }
            
            context.SetData("FetchedConfiguration", result.Data.Configuration);
            return StepResult.Success();
        }
    }
    // DI Registration
    public static class ServiceRegistration
    {
        public static IServiceCollection AddPattern2Services(this IServiceCollection services)
        {
            services.AddScoped<IGrpcGateway, GrpcGateway>();
            services.AddScoped<IOrchestrationStep, FetchConfigurationStep>();
            return services;
        }
    }
    /*
    PROS:
    - Operations are pure data (completely reusable)
    - No service dependencies anywhere except gateway
    - Easy to test (operations are just data)
    - Gateway centralizes all service management
    - Type-safe at compile time
    
    CONS:
    - Pattern matching in dispatcher can get large
    - Casting required (though compile-time safe)
    - Gateway must know about all operations
    - Adding new services requires gateway changes
    */
}
// =====================================================================================
// PATTERN 3: TYPED OPERATION FACTORIES
// Factory creates operations with services pre-bound
// =====================================================================================
namespace Pattern3_TypedFactories
{
    // Pure operation interface
    public interface IGrpcOperation<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        string ServiceName { get; }
        string OperationName { get; }
        Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken);
    }
    // Factory interface for creating operations
    public interface IGrpcOperationFactory
    {
        IGrpcOperation<GetCurrentConfigurationRequest, GetCurrentConfigurationResponse> 
            CreateGetCurrentConfigurationOperation();
        IGrpcOperation<GetSequenceConfigurationRequest, GetSequenceConfigurationResponse> 
            CreateGetSequenceConfigurationOperation();
    }
    // Factory implementation
    public class GrpcOperationFactory : IGrpcOperationFactory
    {
        private readonly IExecutionConfigurationService _executionService;
        
        public GrpcOperationFactory(IExecutionConfigurationService executionService)
        {
            _executionService = executionService;
        }
        
        public IGrpcOperation<GetCurrentConfigurationRequest, GetCurrentConfigurationResponse> 
            CreateGetCurrentConfigurationOperation()
        {
            return new BoundOperation<GetCurrentConfigurationRequest, GetCurrentConfigurationResponse>(
                "ExecutionConfigurationService",
                "GetCurrentConfiguration", 
                _executionService.GetCurrentConfigurationAsync);
        }
        
        public IGrpcOperation<GetSequenceConfigurationRequest, GetSequenceConfigurationResponse> 
            CreateGetSequenceConfigurationOperation()
        {
            return new BoundOperation<GetSequenceConfigurationRequest, GetSequenceConfigurationResponse>(
                "ExecutionConfigurationService",
                "GetSequenceConfiguration", 
                _executionService.GetSequenceConfigurationAsync);
        }
    }
    // Generic bound operation implementation
    public class BoundOperation<TRequest, TResponse> : IGrpcOperation<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        private readonly Func<TRequest, CancellationToken, Task<TResponse>> _executor;
        
        public BoundOperation(
            string serviceName, 
            string operationName, 
            Func<TRequest, CancellationToken, Task<TResponse>> executor)
        {
            ServiceName = serviceName;
            OperationName = operationName;
            _executor = executor;
        }
        
        public string ServiceName { get; }
        public string OperationName { get; }
        
        public Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken)
        {
            return _executor(request, cancellationToken);
        }
    }
    // Simple gateway that executes operations
    public interface IGrpcGateway
    {
        Task<GatewayResult<TResponse>> ExecuteAsync<TRequest, TResponse>(
            IGrpcOperation<TRequest, TResponse> operation,
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class;
    }
    public class GrpcGateway : IGrpcGateway
    {
        private readonly ILogger<GrpcGateway> _logger;
        
        public GrpcGateway(ILogger<GrpcGateway> logger)
        {
            _logger = logger;
        }
        
        public async Task<GatewayResult<TResponse>> ExecuteAsync<TRequest, TResponse>(
            IGrpcOperation<TRequest, TResponse> operation,
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.LogDebug("Executing {ServiceName}.{OperationName}", 
                    operation.ServiceName, operation.OperationName);
                
                var result = await operation.ExecuteAsync(request, cancellationToken);
                
                _logger.LogInformation("Successfully executed {ServiceName}.{OperationName} in {Duration}ms",
                    operation.ServiceName, operation.OperationName, stopwatch.ElapsedMilliseconds);
                
                return GatewayResult<TResponse>.Success(result, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {ServiceName}.{OperationName}",
                    operation.ServiceName, operation.OperationName);
                
                return GatewayResult<TResponse>.Failure(ex.Message, stopwatch.Elapsed);
            }
        }
    }
    // Orchestration step usage
    public class FetchConfigurationStep : IOrchestrationStep
    {
        private readonly IGrpcGateway _gateway;
        private readonly IGrpcOperationFactory _operationFactory;
        
        public FetchConfigurationStep(
            IGrpcGateway gateway,
            IGrpcOperationFactory operationFactory)
        {
            _gateway = gateway;
            _operationFactory = operationFactory;
        }
        
        public string StepName => "FetchConfiguration";
        
        public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            var request = context.GetData<ConfigurationImportRequest>("ImportRequest");
            
            // Create operation from factory
            var operation = _operationFactory.CreateGetCurrentConfigurationOperation();
            var grpcRequest = new GetCurrentConfigurationRequest(request.IncludeSequences);
            
            var result = await _gateway.ExecuteAsync(operation, grpcRequest, cancellationToken);
            
            if (!result.IsSuccess)
            {
                return StepResult.Failure($"Failed to fetch configuration: {result.ErrorMessage}");
            }
            
            context.SetData("FetchedConfiguration", result.Data.Configuration);
            return StepResult.Success();
        }
    }
    // DI Registration
    public static class ServiceRegistration
    {
        public static IServiceCollection AddPattern3Services(this IServiceCollection services)
        {
            services.AddScoped<IGrpcOperationFactory, GrpcOperationFactory>();
            services.AddScoped<IGrpcGateway, GrpcGateway>();
            services.AddScoped<IOrchestrationStep, FetchConfigurationStep>();
            return services;
        }
    }
    /*
    PROS:
    - Operations are completely reusable (no dependencies)
    - Clear separation of concerns
    - Factory pattern is familiar
    - Easy to test (mock factory)
    - Gateway stays simple
    
    CONS:
    - Additional factory abstraction layer
    - Factory interface grows with operations
    - Slightly more complex DI setup
    - Two dependencies in steps (gateway + factory)
    */
}
// =====================================================================================
// PATTERN 4: HIGHER-ORDER GATEWAY PATTERN
// Gateway provides execution capabilities, steps provide logic
// =====================================================================================
namespace Pattern4_HigherOrderGateway
{
    // Gateway provides execution infrastructure
    public interface IGrpcGateway
    {
        Task<GatewayResult<TResponse>> ExecuteAsync<TRequest, TResponse>(
            string serviceName,
            string operationName,
            TRequest request,
            Func<TRequest, CancellationToken, Task<TResponse>> executor,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class;
    }
    public class GrpcGateway : IGrpcGateway
    {
        private readonly ILogger<GrpcGateway> _logger;
        
        public GrpcGateway(ILogger<GrpcGateway> logger)
        {
            _logger = logger;
        }
        
        public async Task<GatewayResult<TResponse>> ExecuteAsync<TRequest, TResponse>(
            string serviceName,
            string operationName,
            TRequest request,
            Func<TRequest, CancellationToken, Task<TResponse>> executor,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.LogDebug("Executing {ServiceName}.{OperationName}", serviceName, operationName);
                
                var result = await executor(request, cancellationToken);
                
                _logger.LogInformation("Successfully executed {ServiceName}.{OperationName} in {Duration}ms",
                    serviceName, operationName, stopwatch.ElapsedMilliseconds);
                
                return GatewayResult<TResponse>.Success(result, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute {ServiceName}.{OperationName}", serviceName, operationName);
                return GatewayResult<TResponse>.Failure(ex.Message, stopwatch.Elapsed);
            }
        }
    }
    // Optional: Static operation definitions for reusability
    public static class ExecutionConfigurationOperations
    {
        public const string ServiceName = "ExecutionConfigurationService";
        
        public static class OperationNames
        {
            public const string GetCurrentConfiguration = "GetCurrentConfiguration";
            public const string GetSequenceConfiguration = "GetSequenceConfiguration";
        }
    }
    // Orchestration step usage
    public class FetchConfigurationStep : IOrchestrationStep
    {
        private readonly IGrpcGateway _gateway;
        private readonly IExecutionConfigurationService _executionService;
        
        public FetchConfigurationStep(
            IGrpcGateway gateway,
            IExecutionConfigurationService executionService)
        {
            _gateway = gateway;
            _executionService = executionService;
        }
        
        public string StepName => "FetchConfiguration";
        
        public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            var request = context.GetData<ConfigurationImportRequest>("ImportRequest");
            var grpcRequest = new GetCurrentConfigurationRequest(request.IncludeSequences);
            
            // Pass execution function to gateway
            var result = await _gateway.ExecuteAsync(
                ExecutionConfigurationOperations.ServiceName,
                ExecutionConfigurationOperations.OperationNames.GetCurrentConfiguration,
                grpcRequest,
                _executionService.GetCurrentConfigurationAsync, // Pass the function
                cancellationToken);
            
            if (!result.IsSuccess)
            {
                return StepResult.Failure($"Failed to fetch configuration: {result.ErrorMessage}");
            }
            
            context.SetData("FetchedConfiguration", result.Data.Configuration);
            return StepResult.Success();
        }
    }
    // Alternative: Helper extension methods for cleaner syntax
    public static class GrpcGatewayExtensions
    {
        public static Task<GatewayResult<GetCurrentConfigurationResponse>> GetCurrentConfigurationAsync(
            this IGrpcGateway gateway,
            IExecutionConfigurationService service,
            GetCurrentConfigurationRequest request,
            CancellationToken cancellationToken = default)
        {
            return gateway.ExecuteAsync(
                ExecutionConfigurationOperations.ServiceName,
                ExecutionConfigurationOperations.OperationNames.GetCurrentConfiguration,
                request,
                service.GetCurrentConfigurationAsync,
                cancellationToken);
        }
        
        public static Task<GatewayResult<GetSequenceConfigurationResponse>> GetSequenceConfigurationAsync(
            this IGrpcGateway gateway,
            IExecutionConfigurationService service,
            GetSequenceConfigurationRequest request,
            CancellationToken cancellationToken = default)
        {
            return gateway.ExecuteAsync(
                ExecutionConfigurationOperations.ServiceName,
                ExecutionConfigurationOperations.OperationNames.GetSequenceConfiguration,
                request,
                service.GetSequenceConfigurationAsync,
                cancellationToken);
        }
    }
    // Orchestration step with extension methods
    public class FetchConfigurationStepWithExtensions : IOrchestrationStep
    {
        private readonly IGrpcGateway _gateway;
        private readonly IExecutionConfigurationService _executionService;
        
        public FetchConfigurationStepWithExtensions(
            IGrpcGateway gateway,
            IExecutionConfigurationService executionService)
        {
            _gateway = gateway;
            _executionService = executionService;
        }
        
        public string StepName => "FetchConfiguration";
        
        public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            var request = context.GetData<ConfigurationImportRequest>("ImportRequest");
            var grpcRequest = new GetCurrentConfigurationRequest(request.IncludeSequences);
            
            // Clean syntax with extension method
            var result = await _gateway.GetCurrentConfigurationAsync(_executionService, grpcRequest, cancellationToken);
            
            if (!result.IsSuccess)
            {
                return StepResult.Failure($"Failed to fetch configuration: {result.ErrorMessage}");
            }
            
            context.SetData("FetchedConfiguration", result.Data.Configuration);
            return StepResult.Success();
        }
    }
    // DI Registration
    public static class ServiceRegistration
    {
        public static IServiceCollection AddPattern4Services(this IServiceCollection services)
        {
            services.AddScoped<IGrpcGateway, GrpcGateway>();
            services.AddScoped<IOrchestrationStep, FetchConfigurationStep>();
            // Or: services.AddScoped<IOrchestrationStep, FetchConfigurationStepWithExtensions>();
            return services;
        }
    }
    /*
    PROS:
    - Simplest gateway implementation
    - No abstraction layers
    - Direct function passing
    - Extension methods provide clean syntax
    - Very flexible
    
    CONS:
    - Steps have service dependencies again
    - Function signatures in step code
    - Less reusable operations
    - Gateway provides less value-add
    */
}
// =====================================================================================
// SHARED TYPES AND INTERFACES FOR ALL PATTERNS
// =====================================================================================
// Common result type
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
// Orchestration interfaces (shared across patterns)
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
// Sample request type
public class ConfigurationImportRequest
{
    public bool IncludeSequences { get; set; } = true;
    public bool ClearExistingData { get; set; } = false;
}
// =====================================================================================
// PATTERN COMPARISON SUMMARY
// =====================================================================================
/*
PATTERN COMPARISON:
1. DELEGATE/FUNCTION INJECTION:
   ✅ Operations are reusable (no service dependencies)
   ✅ Clear separation of concerns
   ✅ Easy to test
   ❌ Gateway interface grows
   ❌ More complex creation patterns

2. MEDIATOR:
   ✅ Operations are pure data (completely reusable)
   ✅ No service dependencies outside gateway
   ✅ Easy to test
   ✅ Type-safe
   ❌ Pattern matching dispatcher
   ❌ Gateway must know all operations

3. TYPED OPERATION FACTORIES:
   ✅ Operations are completely reusable
   ✅ Clear separation of concerns
   ✅ Familiar factory pattern
   ❌ Additional abstraction layer
   ❌ Two dependencies in steps
   ❌ Factory interface grows

4. HIGHER-ORDER GATEWAY:
   ✅ Simplest gateway implementation
   ✅ No abstraction layers
   ✅ Very flexible
   ✅ Extension methods provide clean syntax
   ❌ Steps have service dependencies again
   ❌ Less reusable operations
   ❌ Gateway provides less value

RECOMMENDATION PRIORITY:
1. Mediator Pattern - Best balance of reusability and simplicity
2. Typed Operation Factories - Good for complex scenarios
3. Delegate/Function Injection - Good middle ground
4. Higher-Order Gateway - Best for simple scenarios where gateway value is questioned
*/
Made with
Artifacts are user-generated and may contain unverified or potentially unsafe content.
Customize Artifact
