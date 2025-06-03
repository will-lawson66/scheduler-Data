// =====================================================================================
// EVENT STREAM PROCESSING EXTENSIONS
// Building on the existing gateway and orchestrator design for real-time event processing
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Instrument.Data.Entities;
using System.Runtime.CompilerServices;

// =====================================================================================
// 1. EVENT STREAM ABSTRACTIONS - EXTENDING THE GATEWAY PATTERN
// =====================================================================================

namespace Instrument.Data.Gateway.Streaming
{
    /// <summary>
    /// Represents a streaming gRPC operation (extends the existing pattern)
    /// </summary>
    public interface IStreamingGrpcOperation<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        string ServiceName { get; }
        string OperationName { get; }
        TimeSpan? Timeout { get; }
        
        /// <summary>
        /// Execute a server streaming operation (one request, many responses)
        /// </summary>
        IAsyncEnumerable<TResponse> ExecuteServerStreamAsync(TRequest request, CancellationToken cancellationToken);
        
        /// <summary>
        /// Execute a client streaming operation (many requests, one response)
        /// </summary>
        Task<TResponse> ExecuteClientStreamAsync(IAsyncEnumerable<TRequest> requests, CancellationToken cancellationToken);
        
        /// <summary>
        /// Execute a bidirectional streaming operation (many requests, many responses)
        /// </summary>
        IAsyncEnumerable<TResponse> ExecuteBidirectionalStreamAsync(IAsyncEnumerable<TRequest> requests, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Event-driven gateway extending the existing gateway interface
    /// </summary>
    public interface IEventStreamGateway : IGrpcGateway
    {
        /// <summary>
        /// Execute a streaming operation
        /// </summary>
        IAsyncEnumerable<TResponse> ExecuteStreamAsync<TRequest, TResponse>(
            IStreamingGrpcOperation<TRequest, TResponse> operation,
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class;

        /// <summary>
        /// Execute a bidirectional streaming operation
        /// </summary>
        IAsyncEnumerable<TResponse> ExecuteBidirectionalStreamAsync<TRequest, TResponse>(
            IStreamingGrpcOperation<TRequest, TResponse> operation,
            IAsyncEnumerable<TRequest> requests,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class;
    }

    /// <summary>
    /// Stream result wrapper for monitoring and observability
    /// </summary>
    public class StreamResult<T>
    {
        public bool IsSuccess { get; set; }
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public long SequenceNumber { get; set; }
        public string StreamId { get; set; }
        
        public static StreamResult<T> Success(T data, long sequenceNumber, string streamId) =>
            new() { IsSuccess = true, Data = data, SequenceNumber = sequenceNumber, StreamId = streamId };
            
        public static StreamResult<T> Failure(string error, long sequenceNumber, string streamId) =>
            new() { IsSuccess = false, ErrorMessage = error, SequenceNumber = sequenceNumber, StreamId = streamId };
    }
}

// =====================================================================================
// 2. EVENT STREAM GATEWAY IMPLEMENTATION
// =====================================================================================

namespace Instrument.Data.Gateway.Streaming.Core
{
    using Instrument.Data.Gateway.Abstractions;
    using Instrument.Data.Gateway.Configuration;
    using Instrument.Data.Gateway.Resilience;
    using Instrument.Data.Gateway.Streaming;

    /// <summary>
    /// Event stream gateway implementation extending the existing gateway
    /// </summary>
    public class EventStreamGateway : GrpcGateway, IEventStreamGateway
    {
        private readonly ILogger<EventStreamGateway> _streamLogger;

        public EventStreamGateway(
            IGrpcServiceRegistry serviceRegistry,
            IRetryPolicy retryPolicy,
            ILogger<EventStreamGateway> logger,
            IOptions<GrpcGatewayOptions> options)
            : base(serviceRegistry, retryPolicy, logger, options)
        {
            _streamLogger = logger;
        }

        public async IAsyncEnumerable<TResponse> ExecuteStreamAsync<TRequest, TResponse>(
            IStreamingGrpcOperation<TRequest, TResponse> operation,
            TRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            _streamLogger.LogInformation("Starting stream {ServiceName}.{OperationName}",
                operation.ServiceName, operation.OperationName);

            var streamId = Guid.NewGuid().ToString();
            long sequenceNumber = 0;

            try
            {
                await foreach (var response in operation.ExecuteServerStreamAsync(request, cancellationToken))
                {
                    yield return response;
                    sequenceNumber++;
                    
                    if (sequenceNumber % 1000 == 0)
                    {
                        _streamLogger.LogDebug("Processed {Count} events in stream {StreamId}",
                            sequenceNumber, streamId);
                    }
                }

                _streamLogger.LogInformation("Completed stream {ServiceName}.{OperationName} with {Count} events",
                    operation.ServiceName, operation.OperationName, sequenceNumber);
            }
            catch (Exception ex)
            {
                _streamLogger.LogError(ex, "Stream {ServiceName}.{OperationName} failed at event {SequenceNumber}",
                    operation.ServiceName, operation.OperationName, sequenceNumber);
                throw;
            }
        }

        public async IAsyncEnumerable<TResponse> ExecuteBidirectionalStreamAsync<TRequest, TResponse>(
            IStreamingGrpcOperation<TRequest, TResponse> operation,
            IAsyncEnumerable<TRequest> requests,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            _streamLogger.LogInformation("Starting bidirectional stream {ServiceName}.{OperationName}",
                operation.ServiceName, operation.OperationName);

            await foreach (var response in operation.ExecuteBidirectionalStreamAsync(requests, cancellationToken))
            {
                yield return response;
            }
        }
    }
}

// =====================================================================================
// 3. EVENT-DRIVEN PROCESS MANAGER PATTERN
// =====================================================================================

namespace Instrument.Data.Events.Abstractions
{
    /// <summary>
    /// Base interface for domain events
    /// </summary>
    public interface IDomainEvent
    {
        string EventId { get; }
        string EventType { get; }
        DateTime Timestamp { get; }
        string SourceServiceName { get; }
        object Data { get; }
    }

    /// <summary>
    /// Event handler interface
    /// </summary>
    public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
    {
        Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Event stream processor - extends orchestration pattern for continuous processing
    /// </summary>
    public interface IEventStreamProcessor
    {
        /// <summary>
        /// Process a stream of events through the configured pipeline
        /// </summary>
        IAsyncEnumerable<ProcessingResult> ProcessStreamAsync(
            IAsyncEnumerable<IDomainEvent> eventStream,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Add a processing stage to the pipeline
        /// </summary>
        IEventStreamProcessor AddStage(IEventProcessingStage stage);
    }

    /// <summary>
    /// Event processing stage (similar to orchestration step)
    /// </summary>
    public interface IEventProcessingStage
    {
        string StageName { get; }
        
        /// <summary>
        /// Process an event and optionally emit new events
        /// </summary>
        Task<StageResult> ProcessAsync(IDomainEvent inputEvent, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Result of event processing stage
    /// </summary>
    public class StageResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<IDomainEvent> OutputEvents { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        
        public static StageResult Success(params IDomainEvent[] outputEvents) =>
            new() { IsSuccess = true, OutputEvents = outputEvents.ToList() };
            
        public static StageResult Failure(string error) =>
            new() { IsSuccess = false, ErrorMessage = error };
    }

    /// <summary>
    /// Result of processing a single event through the pipeline
    /// </summary>
    public class ProcessingResult
    {
        public string EventId { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<IDomainEvent> GeneratedEvents { get; set; } = new();
        public TimeSpan ProcessingTime { get; set; }
        public List<string> ProcessedStages { get; set; } = new();
    }
}

// =====================================================================================
// 4. EVENT STREAM PROCESSOR IMPLEMENTATION
// =====================================================================================

namespace Instrument.Data.Events.Core
{
    using Instrument.Data.Events.Abstractions;

    /// <summary>
    /// Event stream processor implementing pipeline pattern
    /// </summary>
    public class EventStreamProcessor : IEventStreamProcessor
    {
        private readonly List<IEventProcessingStage> _stages = new();
        private readonly ILogger<EventStreamProcessor> _logger;

        public EventStreamProcessor(ILogger<EventStreamProcessor> logger)
        {
            _logger = logger;
        }

        public IEventStreamProcessor AddStage(IEventProcessingStage stage)
        {
            _stages.Add(stage);
            _logger.LogDebug("Added processing stage: {StageName}", stage.StageName);
            return this;
        }

        public async IAsyncEnumerable<ProcessingResult> ProcessStreamAsync(
            IAsyncEnumerable<IDomainEvent> eventStream,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting event stream processing with {StageCount} stages", _stages.Count);

            await foreach (var inputEvent in eventStream.WithCancellation(cancellationToken))
            {
                var result = await ProcessSingleEventAsync(inputEvent, cancellationToken);
                yield return result;
            }
        }

        private async Task<ProcessingResult> ProcessSingleEventAsync(IDomainEvent inputEvent, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new ProcessingResult
            {
                EventId = inputEvent.EventId
            };

            var currentEvents = new List<IDomainEvent> { inputEvent };

            try
            {
                foreach (var stage in _stages)
                {
                    var newEvents = new List<IDomainEvent>();

                    foreach (var eventToProcess in currentEvents)
                    {
                        var stageResult = await stage.ProcessAsync(eventToProcess, cancellationToken);
                        
                        if (!stageResult.IsSuccess)
                        {
                            result.IsSuccess = false;
                            result.ErrorMessage = $"Stage '{stage.StageName}' failed: {stageResult.ErrorMessage}";
                            return result;
                        }

                        newEvents.AddRange(stageResult.OutputEvents);
                        result.ProcessedStages.Add(stage.StageName);
                    }

                    currentEvents = newEvents;
                }

                result.IsSuccess = true;
                result.GeneratedEvents = currentEvents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process event {EventId}", inputEvent.EventId);
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                result.ProcessingTime = stopwatch.Elapsed;
            }

            return result;
        }
    }
}

// =====================================================================================
// 5. CONCRETE EVENT TYPES FOR SCHEDULER-DATA
// =====================================================================================

namespace Instrument.Data.Events.SchedulerEvents
{
    using Instrument.Data.Events.Abstractions;

    /// <summary>
    /// Configuration change event from ExecutionService
    /// </summary>
    public record ConfigurationChangedEvent : IDomainEvent
    {
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public string EventType { get; init; } = nameof(ConfigurationChangedEvent);
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string SourceServiceName { get; init; } = "ExecutionConfigurationService";
        public object Data { get; init; }

        // Specific properties
        public string ConfigurationKey { get; init; }
        public string ChangeType { get; init; } // "Created", "Updated", "Deleted"
        public Dictionary<string, object> Changes { get; init; } = new();
    }

    /// <summary>
    /// Sequence execution status event
    /// </summary>
    public record SequenceExecutionEvent : IDomainEvent
    {
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public string EventType { get; init; } = nameof(SequenceExecutionEvent);
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string SourceServiceName { get; init; } = "ExecutionService";
        public object Data { get; init; }

        // Specific properties
        public string SequenceKey { get; init; }
        public string Status { get; init; } // "Started", "Completed", "Failed", "Cancelled"
        public TimeSpan? Duration { get; init; }
        public Dictionary<string, object> Parameters { get; init; } = new();
        public string ErrorMessage { get; init; }
    }

    /// <summary>
    /// Resource status change event
    /// </summary>
    public record ResourceStatusEvent : IDomainEvent
    {
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public string EventType { get; init; } = nameof(ResourceStatusEvent);
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string SourceServiceName { get; init; } = "ResourceService";
        public object Data { get; init; }

        // Specific properties
        public string ResourceKey { get; init; }
        public string Status { get; init; } // "Available", "Busy", "Locked", "Error"
        public Dictionary<string, object> StatusData { get; init; } = new();
    }
}

// =====================================================================================
// 6. CONCRETE PROCESSING STAGES FOR SCHEDULER-DATA
// =====================================================================================

namespace Instrument.Data.Events.Stages
{
    using Instrument.Data.Events.Abstractions;
    using Instrument.Data.Events.SchedulerEvents;

    /// <summary>
    /// Stage to validate incoming events
    /// </summary>
    public class EventValidationStage : IEventProcessingStage
    {
        private readonly ILogger<EventValidationStage> _logger;

        public EventValidationStage(ILogger<EventValidationStage> logger)
        {
            _logger = logger;
        }

        public string StageName => "EventValidation";

        public Task<StageResult> ProcessAsync(IDomainEvent inputEvent, CancellationToken cancellationToken)
        {
            try
            {
                // Validate event structure
                if (string.IsNullOrEmpty(inputEvent.EventId))
                {
                    return Task.FromResult(StageResult.Failure("Event ID is required"));
                }

                if (string.IsNullOrEmpty(inputEvent.EventType))
                {
                    return Task.FromResult(StageResult.Failure("Event type is required"));
                }

                _logger.LogDebug("Validated event {EventId} of type {EventType}", 
                    inputEvent.EventId, inputEvent.EventType);

                // Pass through the original event
                return Task.FromResult(StageResult.Success(inputEvent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate event {EventId}", inputEvent.EventId);
                return Task.FromResult(StageResult.Failure($"Validation failed: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// Stage to enrich events with additional data from scheduler-Data
    /// </summary>
    public class EventEnrichmentStage : IEventProcessingStage
    {
        private readonly ISequenceService _sequenceService;
        private readonly IResourceService _resourceService;
        private readonly ILogger<EventEnrichmentStage> _logger;

        public EventEnrichmentStage(
            ISequenceService sequenceService,
            IResourceService resourceService,
            ILogger<EventEnrichmentStage> logger)
        {
            _sequenceService = sequenceService;
            _resourceService = resourceService;
            _logger = logger;
        }

        public string StageName => "EventEnrichment";

        public async Task<StageResult> ProcessAsync(IDomainEvent inputEvent, CancellationToken cancellationToken)
        {
            try
            {
                var enrichedEvent = await EnrichEventAsync(inputEvent, cancellationToken);
                
                _logger.LogDebug("Enriched event {EventId} of type {EventType}", 
                    inputEvent.EventId, inputEvent.EventType);

                return StageResult.Success(enrichedEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enrich event {EventId}", inputEvent.EventId);
                // Continue with original event on enrichment failure
                return StageResult.Success(inputEvent);
            }
        }

        private async Task<IDomainEvent> EnrichEventAsync(IDomainEvent inputEvent, CancellationToken cancellationToken)
        {
            return inputEvent switch
            {
                SequenceExecutionEvent seqEvent => await EnrichSequenceEventAsync(seqEvent, cancellationToken),
                ResourceStatusEvent resEvent => await EnrichResourceEventAsync(resEvent, cancellationToken),
                _ => inputEvent // Pass through unknown events
            };
        }

        private async Task<SequenceExecutionEvent> EnrichSequenceEventAsync(SequenceExecutionEvent seqEvent, CancellationToken cancellationToken)
        {
            // Look up sequence details from scheduler-Data
            var sequences = await _sequenceService.GetAllSequencesAsync();
            var sequence = sequences.FirstOrDefault(s => s.Name == seqEvent.SequenceKey);

            if (sequence != null)
            {
                var enrichedData = new Dictionary<string, object>(seqEvent.Parameters)
                {
                    ["SequenceId"] = sequence.Id,
                    ["WorstCaseTime"] = sequence.WorstCaseTime,
                    ["CanBeParallel"] = sequence.CanBeParallel,
                    ["Description"] = sequence.Description
                };

                return seqEvent with { Data = enrichedData };
            }

            return seqEvent;
        }

        private async Task<ResourceStatusEvent> EnrichResourceEventAsync(ResourceStatusEvent resEvent, CancellationToken cancellationToken)
        {
            // Look up resource details from scheduler-Data
            var resource = await _resourceService.GetByCodeAsync(resEvent.ResourceKey);

            if (resource != null)
            {
                var enrichedData = new Dictionary<string, object>(resEvent.StatusData)
                {
                    ["ResourceId"] = resource.Id,
                    ["ResourceName"] = resource.Name,
                    ["IsLocked"] = resource.Locked
                };

                return resEvent with { Data = enrichedData };
            }

            return resEvent;
        }
    }

    /// <summary>
    /// Stage to trigger configuration imports based on events
    /// </summary>
    public class ConfigurationSyncStage : IEventProcessingStage
    {
        private readonly IProcessManager<ConfigurationImportRequest, ConfigurationImportResult> _importManager;
        private readonly ILogger<ConfigurationSyncStage> _logger;

        public ConfigurationSyncStage(
            IProcessManager<ConfigurationImportRequest, ConfigurationImportResult> importManager,
            ILogger<ConfigurationSyncStage> logger)
        {
            _importManager = importManager;
            _logger = logger;
        }

        public string StageName => "ConfigurationSync";

        public async Task<StageResult> ProcessAsync(IDomainEvent inputEvent, CancellationToken cancellationToken)
        {
            try
            {
                if (inputEvent is ConfigurationChangedEvent configEvent)
                {
                    _logger.LogInformation("Configuration change detected for {ConfigKey}, triggering sync",
                        configEvent.ConfigurationKey);

                    // Trigger selective import based on change type
                    var importRequest = CreateImportRequest(configEvent);
                    
                    // Execute import asynchronously (fire-and-forget for real-time processing)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var result = await _importManager.ExecuteAsync(importRequest, cancellationToken);
                            _logger.LogInformation("Configuration sync completed for {ConfigKey}: {Success}",
                                configEvent.ConfigurationKey, result.IsSuccess);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Configuration sync failed for {ConfigKey}",
                                configEvent.ConfigurationKey);
                        }
                    }, cancellationToken);

                    // Pass through the original event
                    return StageResult.Success(inputEvent);
                }

                // Pass through non-configuration events
                return StageResult.Success(inputEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process configuration sync for event {EventId}", inputEvent.EventId);
                return StageResult.Failure($"Configuration sync failed: {ex.Message}");
            }
        }

        private ConfigurationImportRequest CreateImportRequest(ConfigurationChangedEvent configEvent)
        {
            return new ConfigurationImportRequest
            {
                IncludeSequences = configEvent.ChangeType != "Deleted",
                ClearExistingData = false,
                SequenceFilters = configEvent.ConfigurationKey.Contains("Sequence") 
                    ? new List<string> { configEvent.ConfigurationKey }
                    : new List<string>(),
                ResourceFilters = configEvent.ConfigurationKey.Contains("Resource")
                    ? new List<string> { configEvent.ConfigurationKey }
                    : new List<string>()
            };
        }
    }
}

// =====================================================================================
// 7. EVENT SOURCING EXTENSION (FUTURE CAPABILITY)
// =====================================================================================

namespace Instrument.Data.Events.Sourcing
{
    using Instrument.Data.Events.Abstractions;

    /// <summary>
    /// Event store interface for storing and retrieving events
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// Append events to the store
        /// </summary>
        Task AppendEventsAsync(string streamId, IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);

        /// <summary>
        /// Read events from a stream
        /// </summary>
        IAsyncEnumerable<IDomainEvent> ReadEventsAsync(string streamId, long fromVersion = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribe to new events as they arrive
        /// </summary>
        IAsyncEnumerable<IDomainEvent> SubscribeToStreamAsync(string streamId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Event store implementation using scheduler-Data storage
    /// </summary>
    public class SchedulerDataEventStore : IEventStore
    {
        private readonly IJsonDataAdapter _jsonAdapter;
        private readonly ILogger<SchedulerDataEventStore> _logger;

        public SchedulerDataEventStore(
            IJsonDataAdapter jsonAdapter,
            ILogger<SchedulerDataEventStore> logger)
        {
            _jsonAdapter = jsonAdapter;
            _logger = logger;
        }

        public async Task AppendEventsAsync(string streamId, IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
        {
            // Implementation would store events in scheduler-Data database
            // This is a simplified example
            foreach (var domainEvent in events)
            {
                _logger.LogDebug("Storing event {EventId} to stream {StreamId}", domainEvent.EventId, streamId);
                // Store using existing data adapters
            }
        }

        public async IAsyncEnumerable<IDomainEvent> ReadEventsAsync(string streamId, long fromVersion = 0, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Implementation would read events from scheduler-Data database
            // This is a placeholder
            yield break;
        }

        public async IAsyncEnumerable<IDomainEvent> SubscribeToStreamAsync(string streamId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Implementation would provide real-time subscription
            // This is a placeholder
            yield break;
        }
    }
}

// =====================================================================================
// 8. STREAMING OPERATIONS FOR EXECUTION SERVICE
// =====================================================================================

namespace Instrument.Data.Gateway.Operations.Streaming
{
    using Instrument.Data.Gateway.Abstractions;
    using Instrument.Data.Gateway.Streaming;
    using Instrument.Data.ExecutionService.Contracts;
    using Instrument.Data.Events.SchedulerEvents;

    /// <summary>
    /// Streaming operation to receive real-time configuration changes
    /// </summary>
    public class ConfigurationChangeStreamOperation : IStreamingGrpcOperation<ConfigurationSubscriptionRequest, ConfigurationChangedEvent>
    {
        private readonly IGrpcServiceRegistry _serviceRegistry;

        public ConfigurationChangeStreamOperation(IGrpcServiceRegistry serviceRegistry)
        {
            _serviceRegistry = serviceRegistry;
        }

        public string ServiceName => "ExecutionConfigurationService";
        public string OperationName => "SubscribeToConfigurationChanges";
        public TimeSpan? Timeout => null; // Long-running stream

        public async IAsyncEnumerable<ConfigurationChangedEvent> ExecuteServerStreamAsync(
            ConfigurationSubscriptionRequest request, 
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var client = _serviceRegistry.GetService<IExecutionConfigurationStreamingService>();
            
            await foreach (var change in client.SubscribeToConfigurationChangesAsync(request, cancellationToken))
            {
                yield return new ConfigurationChangedEvent
                {
                    ConfigurationKey = change.Key,
                    ChangeType = change.ChangeType,
                    Changes = change.Changes?.ToDictionary(kv => kv.Key, kv => (object)kv.Value) ?? new(),
                    Data = change
                };
            }
        }

        public Task<ConfigurationChangedEvent> ExecuteClientStreamAsync(IAsyncEnumerable<ConfigurationSubscriptionRequest> requests, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Client streaming not supported for configuration changes");
        }

        public IAsyncEnumerable<ConfigurationChangedEvent> ExecuteBidirectionalStreamAsync(IAsyncEnumerable<ConfigurationSubscriptionRequest> requests, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Bidirectional streaming not supported for configuration changes");
        }
    }

    /// <summary>
    /// Example streaming service interface (would be defined based on your .proto)
    /// </summary>
    public interface IExecutionConfigurationStreamingService
    {
        IAsyncEnumerable<ConfigurationChangeDto> SubscribeToConfigurationChangesAsync(
            ConfigurationSubscriptionRequest request, 
            CancellationToken cancellationToken);
    }

    // Supporting DTOs
    public record ConfigurationSubscriptionRequest(
        string[] SubscriptionFilters = null,
        bool IncludeInitialState = true);

    public record ConfigurationChangeDto(
        string Key,
        string ChangeType,
        Dictionary<string, string> Changes);
}

// =====================================================================================
// 9. COMPLETE PIPELINE SETUP AND DI REGISTRATION
// =====================================================================================

namespace Instrument.Data.Extensions
{
    using Instrument.Data.Events.Abstractions;
    using Instrument.Data.Events.Core;
    using Instrument.Data.Events.Stages;
    using Instrument.Data.Events.Sourcing;
    using Instrument.Data.Gateway.Streaming;
    using Instrument.Data.Gateway.Streaming.Core;

    public static class EventStreamExtensions
    {
        /// <summary>
        /// Add event stream processing capabilities to scheduler-Data
        /// </summary>
        public static IServiceCollection AddEventStreamProcessing(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Core event processing services
            services.AddScoped<IEventStreamProcessor, EventStreamProcessor>();
            services.AddScoped<IEventStore, SchedulerDataEventStore>();

            // Upgrade gateway to support streaming
            services.AddScoped<IEventStreamGateway, EventStreamGateway>();

            // Processing stages
            services.AddScoped<IEventProcessingStage, EventValidationStage>();
            services.AddScoped<IEventProcessingStage, EventEnrichmentStage>();
            services.AddScoped<IEventProcessingStage, ConfigurationSyncStage>();

            // Event handlers can be registered here
            // services.AddScoped<IEventHandler<ConfigurationChangedEvent>, ConfigurationChangedHandler>();

            return services;
        }

        /// <summary>
        /// Create a configured event processing pipeline
        /// </summary>
        public static IEventStreamProcessor CreateConfigurationSyncPipeline(IServiceProvider serviceProvider)
        {
            var processor = serviceProvider.GetRequiredService<IEventStreamProcessor>();
            
            return processor
                .AddStage(serviceProvider.GetRequiredService<EventValidationStage>())
                .AddStage(serviceProvider.GetRequiredService<EventEnrichmentStage>())
                .AddStage(serviceProvider.GetRequiredService<ConfigurationSyncStage>());
        }
    }
}

// =====================================================================================
// 10. USAGE EXAMPLE - REAL-TIME CONFIGURATION SYNC
// =====================================================================================

namespace Instrument.Data.Examples
{
    using Instrument.Data.Events.Abstractions;
    using Instrument.Data.Gateway.Streaming;
    using Instrument.Data.Gateway.Operations.Streaming;

    public class RealTimeConfigurationSync
    {
        private readonly IEventStreamGateway _streamGateway;
        private readonly IEventStreamProcessor _eventProcessor;
        private readonly IGrpcServiceRegistry _serviceRegistry;
        private readonly ILogger<RealTimeConfigurationSync> _logger;

        public RealTimeConfigurationSync(
            IEventStreamGateway streamGateway,
            IEventStreamProcessor eventProcessor,
            IGrpcServiceRegistry serviceRegistry,
            ILogger<RealTimeConfigurationSync> logger)
        {
            _streamGateway = streamGateway;
            _eventProcessor = eventProcessor;
            _serviceRegistry = serviceRegistry;
            _logger = logger;
        }

        /// <summary>
        /// Start real-time configuration synchronization
        /// </summary>
        public async Task StartRealTimeSyncAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting real-time configuration synchronization");

            try
            {
                // Set up the streaming operation
                var streamOperation = new ConfigurationChangeStreamOperation(_serviceRegistry);
                var subscriptionRequest = new ConfigurationSubscriptionRequest(
                    SubscriptionFilters: new[] { "Sequence.*", "Resource.*" },
                    IncludeInitialState: false
                );

                // Get the event stream from ExecutionService
                var eventStream = _streamGateway.ExecuteStreamAsync(streamOperation, subscriptionRequest, cancellationToken);

                // Process events through the pipeline
                await foreach (var result in _eventProcessor.ProcessStreamAsync(eventStream, cancellationToken))
                {
                    if (result.IsSuccess)
                    {
                        _logger.LogDebug("Successfully processed event {EventId} through {StageCount} stages in {Duration}ms",
                            result.EventId, result.ProcessedStages.Count, result.ProcessingTime.TotalMilliseconds);
                    }
                    else
                    {
                        _logger.LogError("Failed to process event {EventId}: {Error}",
                            result.EventId, result.ErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Real-time configuration sync failed");
                throw;
            }
        }

        /// <summary>
        /// Example of processing historical events
        /// </summary>
        public async Task ProcessHistoricalEventsAsync(DateTime fromTimestamp, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing historical events from {FromTimestamp}", fromTimestamp);

            // This would use the event store to replay events
            // var eventStore = serviceProvider.GetRequiredService<IEventStore>();
            // var historicalEvents = eventStore.ReadEventsAsync("configuration-stream", fromVersion: 0, cancellationToken);
            
            // await foreach (var result in _eventProcessor.ProcessStreamAsync(historicalEvents, cancellationToken))
            // {
            //     // Process historical events
            // }
        }
    }
}

// =====================================================================================
// 11. CONFIGURATION FOR STREAMING
// =====================================================================================

/*
{
  "GrpcGateway": {
    "DefaultTimeoutSeconds": 30,
    "MaxConcurrentRequests": 10,
    "Retry": {
      "MaxAttempts": 3,
      "BaseDelayMs": 1000,
      "BackoffMultiplier": 2.0
    },
    "Services": {
      "ExecutionConfigurationService": {
        "TimeoutSeconds": null,  // null = no timeout for streaming
        "BaseAddress": "https://execution-service:5001"
      }
    }
  },
  "EventStreamProcessing": {
    "EnableEventSourcing": true,
    "EventStoreConnectionString": "Data Source=events.db",
    "ProcessingParallelism": 4,
    "EnableRealTimeSync": true
  }
}
*/