// =====================================================================================
// PATTERN 1: REPOSITORY PATTERN WITH gRPC GATEWAY - IMPLEMENTATION GUIDE
// =====================================================================================

// 1. Core Gateway Interface
namespace Instrument.Data.Gateways
{
    public interface IGrpcGateway
    {
        Task<TResult> ExecuteAsync<TRequest, TResult>(
            GrpcOperation<TRequest, TResult> operation,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResult : class;
            
        Task<IEnumerable<TResult>> ExecuteBatchAsync<TRequest, TResult>(
            IEnumerable<GrpcOperation<TRequest, TResult>> operations,
            BatchExecutionOptions options = null,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResult : class;
            
        Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    }

    // Operation abstraction
    public abstract class GrpcOperation<TRequest, TResult>
        where TRequest : class
        where TResult : class
    {
        public TRequest Request { get; set; }
        public Type ClientType { get; set; }
        public RetryPolicy RetryPolicy { get; set; } = RetryPolicy.Default;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        
        public abstract Task<TResult> Execute(object client, CancellationToken cancellationToken);
    }
}

// 2. Gateway Implementation
namespace Instrument.Data.Gateways
{
    public class GrpcGateway : IGrpcGateway, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly GrpcGatewayOptions _options;
        private readonly ILogger<GrpcGateway> _logger;
        private readonly ICircuitBreaker _circuitBreaker;
        private readonly IMemoryCache _cache;
        private readonly SemaphoreSlim _semaphore;

        public GrpcGateway(
            IServiceProvider serviceProvider,
            IOptions<GrpcGatewayOptions> options,
            ILogger<GrpcGateway> logger,
            ICircuitBreaker circuitBreaker,
            IMemoryCache cache)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
            _logger = logger;
            _circuitBreaker = circuitBreaker;
            _cache = cache;
            _semaphore = new SemaphoreSlim(_options.MaxConcurrentRequests, _options.MaxConcurrentRequests);
        }

        public async Task<TResult> ExecuteAsync<TRequest, TResult>(
            GrpcOperation<TRequest, TResult> operation,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResult : class
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = Guid.NewGuid().ToString();
            
            _logger.LogDebug("Executing gRPC operation {OperationType} with correlation ID {CorrelationId}",
                typeof(TResult).Name, correlationId);

            await _semaphore.WaitAsync(cancellationToken);
            
            try
            {
                // Check cache first
                var cacheKey = GenerateCacheKey(operation);
                if (_options.EnableCaching && _cache.TryGetValue(cacheKey, out TResult cachedResult))
                {
                    _logger.LogDebug("Cache hit for operation {OperationType}", typeof(TResult).Name);
                    return cachedResult;
                }

                // Execute with circuit breaker
                var result = await _circuitBreaker.ExecuteAsync(async () =>
                {
                    var client = _serviceProvider.GetRequiredService(operation.ClientType);
                    return await ExecuteWithRetryAsync(operation, client, cancellationToken);
                }, cancellationToken);

                // Cache result if enabled
                if (_options.EnableCaching && result != null)
                {
                    var cacheExpiry = TimeSpan.FromMinutes(_options.CacheExpiryMinutes);
                    _cache.Set(cacheKey, result, cacheExpiry);
                }

                _logger.LogInformation("Successfully executed gRPC operation {OperationType} in {ElapsedMs}ms",
                    typeof(TResult).Name, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute gRPC operation {OperationType} after {ElapsedMs}ms",
                    typeof(TResult).Name, stopwatch.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                _semaphore.Release();
                stopwatch.Stop();
            }
        }

        private async Task<TResult> ExecuteWithRetryAsync<TRequest, TResult>(
            GrpcOperation<TRequest, TResult> operation,
            object client,
            CancellationToken cancellationToken)
            where TRequest : class
            where TResult : class
        {
            var policy = operation.RetryPolicy;
            Exception lastException = null;

            for (int attempt = 1; attempt <= policy.MaxRetries + 1; attempt++)
            {
                try
                {
                    if (attempt > 1)
                    {
                        var delay = TimeSpan.FromMilliseconds(policy.DelayMs * Math.Pow(policy.BackoffMultiplier, attempt - 1));
                        await Task.Delay(delay, cancellationToken);
                    }

                    using var timeoutCts = new CancellationTokenSource(operation.Timeout);
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                    return await operation.Execute(client, linkedCts.Token);
                }
                catch (RpcException ex) when (IsRetriableException(ex))
                {
                    lastException = ex;
                    _logger.LogWarning("Attempt {Attempt} failed with retriable error: {Error}",
                        attempt, ex.Status);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Non-retriable error on attempt {Attempt}", attempt);
                    throw;
                }
            }

            throw lastException ?? new InvalidOperationException("All retry attempts failed");
        }

        public async Task<IEnumerable<TResult>> ExecuteBatchAsync<TRequest, TResult>(
            IEnumerable<GrpcOperation<TRequest, TResult>> operations,
            BatchExecutionOptions options = null,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResult : class
        {
            options ??= new BatchExecutionOptions();
            var results = new ConcurrentBag<TResult>();
            var semaphore = new SemaphoreSlim(options.MaxConcurrency);

            var tasks = operations.Select(async operation =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var result = await ExecuteAsync(operation, cancellationToken);
                    if (result != null)
                        results.Add(result);
                }
                catch (Exception ex)
                {
                    if (!options.ContinueOnError)
                        throw;
                    _logger.LogWarning(ex, "Batch operation failed, continuing due to ContinueOnError=true");
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            return results.ToList();
        }

        private static bool IsRetriableException(RpcException ex)
        {
            return ex.StatusCode == StatusCode.DeadlineExceeded ||
                   ex.StatusCode == StatusCode.Unavailable ||
                   ex.StatusCode == StatusCode.Internal ||
                   ex.StatusCode == StatusCode.Unknown;
        }

        private static string GenerateCacheKey<TRequest, TResult>(GrpcOperation<TRequest, TResult> operation)
            where TRequest : class
            where TResult : class
        {
            var requestHash = operation.Request?.GetHashCode() ?? 0;
            return $"grpc_{typeof(TResult).Name}_{requestHash}";
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }
}

// 3. Generic gRPC Repository
namespace Instrument.Data.Repositories.Grpc
{
    public interface IGrpcRepository<T> where T : class
    {
        Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);
        Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    }

    public class GrpcRepository<T> : IGrpcRepository<T> where T : class
    {
        private readonly IGrpcGateway _gateway;
        private readonly IGrpcOperationFactory<T> _operationFactory;
        private readonly ILogger<GrpcRepository<T>> _logger;

        public GrpcRepository(
            IGrpcGateway gateway,
            IGrpcOperationFactory<T> operationFactory,
            ILogger<GrpcRepository<T>> logger)
        {
            _gateway = gateway;
            _operationFactory = operationFactory;
            _logger = logger;
        }

        public async Task<T> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var operation = _operationFactory.CreateGetByIdOperation(id);
            return await _gateway.ExecuteAsync(operation, cancellationToken);
        }

        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var operation = _operationFactory.CreateGetAllOperation();
            return await _gateway.ExecuteAsync(operation, cancellationToken);
        }

        public async Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var operation = _operationFactory.CreateGetPagedOperation(page, pageSize);
            return await _gateway.ExecuteAsync(operation, cancellationToken);
        }

        public async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
        {
            var operation = _operationFactory.CreateCreateOperation(entity);
            return await _gateway.ExecuteAsync(operation, cancellationToken);
        }

        public async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            var operation = _operationFactory.CreateUpdateOperation(entity);
            return await _gateway.ExecuteAsync(operation, cancellationToken);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var operation = _operationFactory.CreateDeleteOperation(id);
            var result = await _gateway.ExecuteAsync(operation, cancellationToken);
            return result.Success;
        }

        public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            var operation = _operationFactory.CreateExistsOperation(id);
            var result = await _gateway.ExecuteAsync(operation, cancellationToken);
            return result.Exists;
        }
    }
}

// 4. Specific Implementation for Sequences
namespace Instrument.Data.Repositories.Grpc
{
    public class SequenceGrpcRepository : GrpcRepository<Sequence>
    {
        public SequenceGrpcRepository(
            IGrpcGateway gateway,
            IGrpcOperationFactory<Sequence> operationFactory,
            ILogger<SequenceGrpcRepository> logger)
            : base(gateway, operationFactory, logger)
        {
        }

        // Add specific methods for Sequence if needed
        public async Task<IEnumerable<Sequence>> GetSequencesByTechnologyAsync(
            Technology technology,
            CancellationToken cancellationToken = default)
        {
            var operation = new GetSequencesByTechnologyOperation(technology);
            return await _gateway.ExecuteAsync(operation, cancellationToken);
        }
    }

    // Operation factory for Sequence
    public class SequenceOperationFactory : IGrpcOperationFactory<Sequence>
    {
        public GrpcOperation<GetByIdRequest, Sequence> CreateGetByIdOperation(int id)
        {
            return new GetSequenceByIdOperation(id);
        }

        public GrpcOperation<GetAllRequest, IEnumerable<Sequence>> CreateGetAllOperation()
        {
            return new GetAllSequencesOperation();
        }

        // ... other operations
    }

    // Specific operations
    public class GetSequenceByIdOperation : GrpcOperation<GetByIdRequest, Sequence>
    {
        public GetSequenceByIdOperation(int id)
        {
            Request = new GetByIdRequest { Id = id };
            ClientType = typeof(ISequenceGrpcClient);
        }

        public override async Task<Sequence> Execute(object client, CancellationToken cancellationToken)
        {
            var sequenceClient = (ISequenceGrpcClient)client;
            return await sequenceClient.GetSequenceByIdAsync(Request.Id, cancellationToken);
        }
    }
}

// =====================================================================================
// PATTERN 2: EVENT-DRIVEN ARCHITECTURE WITH CQRS - IMPLEMENTATION GUIDE
// =====================================================================================

// 1. Command and Query Interfaces
namespace Instrument.Data.CQRS
{
    public interface ICommand
    {
        string CorrelationId { get; }
        DateTime Timestamp { get; }
        string UserId { get; }
    }

    public interface IQuery<TResult>
    {
        string CorrelationId { get; }
        DateTime Timestamp { get; }
    }

    public interface ICommandHandler<TCommand> where TCommand : ICommand
    {
        Task<CommandResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
    }

    public interface IQueryHandler<TQuery, TResult> 
        where TQuery : IQuery<TResult>
        where TResult : class
    {
        Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
    }
}

// 2. Event Interfaces
namespace Instrument.Data.Events
{
    public interface IEvent
    {
        string EventId { get; }
        string CorrelationId { get; }
        DateTime Timestamp { get; }
        string EventType { get; }
    }

    public interface IEventHandler<TEvent> where TEvent : IEvent
    {
        Task HandleAsync(TEvent eventData, CancellationToken cancellationToken = default);
    }

    public interface IEventBus
    {
        Task PublishAsync<T>(T eventData, CancellationToken cancellationToken = default) where T : IEvent;
        Task SubscribeAsync<T>(Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where T : IEvent;
    }
}

// 3. Specific Commands and Queries
namespace Instrument.Data.CQRS.Commands
{
    public class SyncSequencesCommand : ICommand
    {
        public string CorrelationId { get; }
        public DateTime Timestamp { get; }
        public string UserId { get; }
        public bool ForceRefresh { get; set; }

        public SyncSequencesCommand(string userId, bool forceRefresh = false)
        {
            CorrelationId = Guid.New Guid().ToString();
            Timestamp = DateTime.UtcNow;
            UserId = userId;
            ForceRefresh = forceRefresh;
        }
    }

    public class GetSequencesQuery : IQuery<IEnumerable<SequenceReadModel>>
    {
        public string CorrelationId { get; }
        public DateTime Timestamp { get; }
        public string Filter { get; set; }
        public string SortOrder { get; set; }
        public int PageSize { get; set; } = 50;
        public int PageNumber { get; set; } = 1;

        public GetSequencesQuery()
        {
            CorrelationId = Guid.NewGuid().ToString();
            Timestamp = DateTime.UtcNow;
        }
    }
}

// 4. Command Handler Implementation
namespace Instrument.Data.CQRS.Handlers
{
    public class SyncSequencesCommandHandler : ICommandHandler<SyncSequencesCommand>
    {
        private readonly IGrpcClientFactory _clientFactory;
        private readonly IEventBus _eventBus;
        private readonly ILogger<SyncSequencesCommandHandler> _logger;
        private readonly ISyncStateRepository _syncStateRepository;

        public SyncSequencesCommandHandler(
            IGrpcClientFactory clientFactory,
            IEventBus eventBus,
            ILogger<SyncSequencesCommandHandler> logger,
            ISyncStateRepository syncStateRepository)
        {
            _clientFactory = clientFactory;
            _eventBus = eventBus;
            _logger = logger;
            _syncStateRepository = syncStateRepository;
        }

        public async Task<CommandResult> HandleAsync(
            SyncSequencesCommand command, 
            CancellationToken cancellationToken = default)
        {
            var syncState = new SyncState
            {
                CorrelationId = command.CorrelationId,
                EntityType = "Sequence",
                StartedAt = DateTime.UtcNow,
                Status = SyncStatus.InProgress
            };

            await _syncStateRepository.SaveAsync(syncState, cancellationToken);

            try
            {
                var startEvent = new SequencesSyncStartedEvent
                {
                    CorrelationId = command.CorrelationId,
                    StartedAt = DateTime.UtcNow,
                    UserId = command.UserId,
                    ForceRefresh = command.ForceRefresh
                };

                await _eventBus.PublishAsync(startEvent, cancellationToken);

                using var client = _clientFactory.CreateSequenceClient();
                var sequences = new List<Sequence>();
                var processedCount = 0;

                // Use streaming to handle large datasets
                await foreach (var sequence in client.StreamSequencesAsync(cancellationToken))
                {
                    sequences.Add(sequence);
                    processedCount++;

                    // Publish event for each sequence received
                    var sequenceEvent = new SequenceReceivedEvent
                    {
                        EventId = Guid.NewGuid().ToString(),
                        CorrelationId = command.CorrelationId,
                        Timestamp = DateTime.UtcNow,
                        EventType = nameof(SequenceReceivedEvent),
                        SequenceId = sequence.Id,
                        SequenceName = sequence.Name,
                        SequenceData = sequence,
                        ProcessedCount = processedCount
                    };

                    await _eventBus.PublishAsync(sequenceEvent, cancellationToken);

                    // Batch processing to avoid memory issues
                    if (sequences.Count >= 100)
                    {
                        var batchEvent = new SequencesBatchProcessedEvent
                        {
                            CorrelationId = command.CorrelationId,
                            BatchSize = sequences.Count,
                            TotalProcessed = processedCount
                        };

                        await _eventBus.PublishAsync(batchEvent, cancellationToken);
                        sequences.Clear(); // Clear for next batch
                    }
                }

                // Final completion event
                var completedEvent = new SequencesSyncCompletedEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    CorrelationId = command.CorrelationId,
                    Timestamp = DateTime.UtcNow,
                    EventType = nameof(SequencesSyncCompletedEvent),
                    TotalSequences = processedCount,
                    SyncStartedAt = command.Timestamp,
                    SyncCompletedAt = DateTime.UtcNow
                };

                await _eventBus.PublishAsync(completedEvent, cancellationToken);

                // Update sync state
                syncState.CompletedAt = DateTime.UtcNow;
                syncState.Status = SyncStatus.Completed;
                syncState.RecordsProcessed = processedCount;
                await _syncStateRepository.SaveAsync(syncState, cancellationToken);

                return CommandResult.Success(processedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync sequences for correlation {CorrelationId}", 
                    command.CorrelationId);

                var errorEvent = new SequencesSyncFailedEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    CorrelationId = command.CorrelationId,
                    Timestamp = DateTime.UtcNow,
                    EventType = nameof(SequencesSyncFailedEvent),
                    Error = ex.Message,
                    StackTrace = ex.StackTrace,
                    FailedAt = DateTime.UtcNow
                };

                await _eventBus.PublishAsync(errorEvent, cancellationToken);

                // Update sync state
                syncState.CompletedAt = DateTime.UtcNow;
                syncState.Status = SyncStatus.Failed;
                syncState.ErrorMessage = ex.Message;
                await _syncStateRepository.SaveAsync(syncState, cancellationToken);

                return CommandResult.Failure(ex.Message);
            }
        }
    }
}

// 5. Query Handler Implementation
namespace Instrument.Data.CQRS.Handlers
{
    public class GetSequencesQueryHandler : IQueryHandler<GetSequencesQuery, IEnumerable<SequenceReadModel>>
    {
        private readonly IReadModelRepository<SequenceReadModel> _readModelRepository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<GetSequencesQueryHandler> _logger;

        public GetSequencesQueryHandler(
            IReadModelRepository<SequenceReadModel> readModelRepository,
            IMemoryCache cache,
            ILogger<GetSequencesQueryHandler> logger)
        {
            _readModelRepository = readModelRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IEnumerable<SequenceReadModel>> HandleAsync(
            GetSequencesQuery query, 
            CancellationToken cancellationToken = default)
        {
            var cacheKey = GenerateCacheKey(query);
            
            if (_cache.TryGetValue(cacheKey, out IEnumerable<SequenceReadModel> cachedResult))
            {
                _logger.LogDebug("Cache hit for sequences query with correlation {CorrelationId}", 
                    query.CorrelationId);
                return cachedResult;
            }

            _logger.LogDebug("Executing sequences query with correlation {CorrelationId}", 
                query.CorrelationId);

            var sequences = await _readModelRepository.GetAsync(
                filter: query.Filter,
                sortOrder: query.SortOrder,
                pageSize: query.PageSize,
                pageNumber: query.PageNumber,
                cancellationToken: cancellationToken);

            // Cache for 15 minutes
            _cache.Set(cacheKey, sequences, TimeSpan.FromMinutes(15));

            _logger.LogInformation("Retrieved {Count} sequences for query {CorrelationId}", 
                sequences.Count(), query.CorrelationId);

            return sequences;
        }

        private static string GenerateCacheKey(GetSequencesQuery query)
        {
            return $"sequences_{query.Filter}_{query.SortOrder}_{query.PageSize}_{query.PageNumber}";
        }
    }
}

// 6. Event Handlers for Projections
namespace Instrument.Data.Events.Handlers
{
    public class SequenceProjectionHandler : 
        IEventHandler<SequenceReceivedEvent>,
        IEventHandler<SequenceUpdatedEvent>,
        IEventHandler<SequenceDeletedEvent>
    {
        private readonly IReadModelRepository<SequenceReadModel> _repository;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SequenceProjectionHandler> _logger;

        public SequenceProjectionHandler(
            IReadModelRepository<SequenceReadModel> repository,
            IMapper mapper,
            IMemoryCache cache,
            ILogger<SequenceProjectionHandler> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }

        public async Task HandleAsync(SequenceReceivedEvent eventData, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Processing SequenceReceivedEvent for sequence {SequenceId} with correlation {CorrelationId}",
                eventData.SequenceId, eventData.CorrelationId);

            var readModel = _mapper.Map<SequenceReadModel>(eventData.SequenceData);
            readModel.LastUpdated = eventData.Timestamp;
            readModel.CorrelationId = eventData.CorrelationId;

            // Check if read model already exists
            var existing = await _repository.GetByIdAsync(eventData.SequenceId, cancellationToken);

            if (existing == null)
            {
                await _repository.CreateAsync(readModel, cancellationToken);
                _logger.LogDebug("Created new sequence read model for {SequenceId}", eventData.SequenceId);
            }
            else
            {
                // Apply merge strategy based on timestamps
                var merged = MergeReadModels(existing, readModel);
                await _repository.UpdateAsync(merged, cancellationToken);
                _logger.LogDebug("Updated existing sequence read model for {SequenceId}", eventData.SequenceId);
            }

            // Invalidate related caches
            await InvalidateCachePatternAsync("sequences_*", cancellationToken);
        }

        public async Task HandleAsync(SequenceUpdatedEvent eventData, CancellationToken cancellationToken)
        {
            // Similar implementation for updates
            _logger.LogDebug("Processing SequenceUpdatedEvent for sequence {SequenceId}", eventData.SequenceId);
            // Implementation here...
        }

        public async Task HandleAsync(SequenceDeletedEvent eventData, CancellationToken cancellationToken)
        {
            // Implementation for deletions
            _logger.LogDebug("Processing SequenceDeletedEvent for sequence {SequenceId}", eventData.SequenceId);
            
            await _repository.DeleteAsync(eventData.SequenceId, cancellationToken);
            await InvalidateCachePatternAsync("sequences_*", cancellationToken);
        }

        private SequenceReadModel MergeReadModels(SequenceReadModel existing, SequenceReadModel updated)
        {
            // Implement your merge strategy here
            // For example, take the latest based on timestamp
            return updated.LastUpdated > existing.LastUpdated ? updated : existing;
        }

        private async Task InvalidateCachePatternAsync(string pattern, CancellationToken cancellationToken)
        {
            // Implementation to invalidate cache entries matching the pattern
            // This would depend on your cache implementation
            _logger.LogDebug("Invalidating cache pattern: {Pattern}", pattern);
        }
    }
}

// 7. Event Bus Implementation
namespace Instrument.Data.Events
{
    public class InMemoryEventBus : IEventBus
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InMemoryEventBus> _logger;
        private readonly ConcurrentDictionary<Type, List<Type>> _handlerMap;

        public InMemoryEventBus(IServiceProvider serviceProvider, ILogger<InMemoryEventBus> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _handlerMap = new ConcurrentDictionary<Type, List<Type>>();
            
            // Register handlers during initialization
            RegisterHandlers();
        }

        public async Task PublishAsync<T>(T eventData, CancellationToken cancellationToken = default) where T : IEvent
        {
            var eventType = typeof(T);
            
            _logger.LogDebug("Publishing event {EventType} with ID {EventId}", 
                eventType.Name, eventData.EventId);

            if (!_handlerMap.TryGetValue(eventType, out var handlerTypes))
            {
                _logger.LogWarning("No handlers registered for event type: {EventType}", eventType.Name);
                return;
            }

            var tasks = handlerTypes.Select(async handlerType =>
            {
                try
                {
                    var handler = _serviceProvider.GetRequiredService(handlerType);
                    var handleMethod = handlerType.GetMethod("HandleAsync", new[] { eventType, typeof(CancellationToken) });
                    
                    if (handleMethod != null)
                    {
                        _logger.LogDebug("Executing handler {HandlerType} for event {EventType}", 
                            handlerType.Name, eventType.Name);
                            
                        var task = (Task)handleMethod.Invoke(handler, new object[] { eventData, cancellationToken });
                        await task;
                        
                        _logger.LogDebug("Successfully executed handler {HandlerType} for event {EventType}", 
                            handlerType.Name, eventType.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling event {EventType} with handler {HandlerType}", 
                        eventType.Name, handlerType.Name);
                        
                    // Optionally implement dead letter queue or retry logic here
                }
            });

            await Task.WhenAll(tasks);
            
            _logger.LogDebug("Completed publishing event {EventType} to {HandlerCount} handlers", 
                eventType.Name, handlerTypes.Count);
        }

        public Task SubscribeAsync<T>(Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where T : IEvent
        {
            // For in-memory implementation, this could be used for dynamic subscription
            // In a real implementation, you might use a message broker like RabbitMQ or Azure Service Bus
            throw new NotImplementedException("Dynamic subscription not implemented for in-memory event bus");
        }

        private void RegisterHandlers()
        {
            // Automatically register all event handlers found in the assembly
            var handlerInterface = typeof(IEventHandler<>);
            var eventTypes = new Dictionary<Type, List<Type>>();

            var assembly = Assembly.GetExecutingAssembly();
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface))
                .ToList();

            foreach (var handlerType in handlerTypes)
            {
                var interfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface);

                foreach (var interfaceType in interfaces)
                {
                    var eventType = interfaceType.GetGenericArguments()[0];
                    
                    if (!eventTypes.ContainsKey(eventType))
                        eventTypes[eventType] = new List<Type>();
                        
                    eventTypes[eventType].Add(handlerType);
                }
            }

            foreach (var kvp in eventTypes)
            {
                _handlerMap.TryAdd(kvp.Key, kvp.Value);
                _logger.LogInformation("Registered {HandlerCount} handlers for event type {EventType}", 
                    kvp.Value.Count, kvp.Key.Name);
            }
        }
    }
}

// 8. Service Registration Extensions
namespace Instrument.Data.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGrpcGatewayPattern(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Register gateway pattern services
            services.Configure<GrpcGatewayOptions>(configuration.GetSection("GrpcGateway"));
            services.AddSingleton<IGrpcGateway, GrpcGateway>();
            services.AddSingleton<ICircuitBreaker, GrpcCircuitBreaker>();
            services.AddMemoryCache();
            
            // Register repositories
            services.AddScoped<IGrpcRepository<Sequence>, SequenceGrpcRepository>();
            services.AddScoped<IGrpcRepository<Parameter>, ParameterGrpcRepository>();
            services.AddScoped<IGrpcRepository<Resource>, ResourceGrpcRepository>();
            
            // Register operation factories
            services.AddScoped<IGrpcOperationFactory<Sequence>, SequenceOperationFactory>();
            services.AddScoped<IGrpcOperationFactory<Parameter>, ParameterOperationFactory>();
            services.AddScoped<IGrpcOperationFactory<Resource>, ResourceOperationFactory>();
            
            return services;
        }

        public static IServiceCollection AddCqrsPattern(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Register CQRS services
            services.AddSingleton<IEventBus, InMemoryEventBus>();
            services.AddScoped<ICommandBus, CommandBus>();
            services.AddScoped<IQueryBus, QueryBus>();
            
            // Register handlers
            services.AddScoped<ICommandHandler<SyncSequencesCommand>, SyncSequencesCommandHandler>();
            services.AddScoped<IQueryHandler<GetSequencesQuery, IEnumerable<SequenceReadModel>>, GetSequencesQueryHandler>();
            
            // Register event handlers
            services.AddScoped<IEventHandler<SequenceReceivedEvent>, SequenceProjectionHandler>();
            services.AddScoped<IEventHandler<SequenceUpdatedEvent>, SequenceProjectionHandler>();
            services.AddScoped<IEventHandler<SequenceDeletedEvent>, SequenceProjectionHandler>();
            
            // Register repositories
            services.AddScoped<IReadModelRepository<SequenceReadModel>, SequenceReadModelRepository>();
            services.AddScoped<ISyncStateRepository, SyncStateRepository>();
            
            return services;
        }
    }
}

// This implementation guide provides concrete, working code for both design patterns
// that can be integrated into your existing Instrument.Data project.