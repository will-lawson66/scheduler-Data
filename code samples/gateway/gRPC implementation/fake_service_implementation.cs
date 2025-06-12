using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Instrument.Execution.Grpc.Configuration;
using Instrument.Grpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Instrument.Execution.Grpc.FakeService;

/// <summary>
/// Fake implementation of IExecutionConfigurationService for testing purposes.
/// Returns realistic test data in JSON format as if from gRPC gateway.
/// </summary>
public class FakeExecutionConfigurationService : IExecutionConfigurationService
{
    private readonly FakeDataProvider _dataProvider;
    private readonly FakeServiceOptions _options;
    private readonly ILogger<FakeExecutionConfigurationService> _logger;
    private readonly DelaySimulator _delaySimulator;
    private readonly ResponseBuilder _responseBuilder;

    public FakeExecutionConfigurationService(
        FakeDataProvider dataProvider,
        IOptions<FakeServiceOptions> options,
        ILogger<FakeExecutionConfigurationService> logger,
        DelaySimulator delaySimulator,
        ResponseBuilder responseBuilder)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _delaySimulator = delaySimulator ?? throw new ArgumentNullException(nameof(delaySimulator));
        _responseBuilder = responseBuilder ?? throw new ArgumentNullException(nameof(responseBuilder));
    }

    /// <summary>
    /// Fetches the current configuration with optional sequences included.
    /// </summary>
    public async Task<FetchExecutionConfigurationResponse> GetCurrentConfigurationAsync(
        FetchExecutionConfigurationRequest request)
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("Processing GetCurrentConfiguration request {RequestId}, IncludeSequences: {IncludeSequences}",
            requestId, request.IncludeSequences);

        try
        {
            // Simulate processing delay
            await _delaySimulator.SimulateDelayAsync();

            // Check for simulated failures
            if (await _delaySimulator.ShouldSimulateFailureAsync())
            {
                _logger.LogWarning("Simulating failure for request {RequestId}", requestId);
                return _responseBuilder.CreateErrorResponse<FetchExecutionConfigurationResponse>(
                    requestId, "Simulated service failure", 500);
            }

            // Get configuration data
            var configuration = await _dataProvider.GetExecutionConfigurationAsync(request.IncludeSequences);

            _logger.LogInformation("Successfully retrieved configuration for request {RequestId}", requestId);

            return new FetchExecutionConfigurationResponse(
                requestId,
                configuration,
                Array.Empty<GrpcErrorContract>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetCurrentConfiguration request {RequestId}", requestId);
            return _responseBuilder.CreateErrorResponse<FetchExecutionConfigurationResponse>(
                requestId, $"Internal error: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Fetches a specific sequence configuration by its key.
    /// </summary>
    public async Task<FetchSequenceConfigurationResponse> GetSequenceConfigurationAsync(
        FetchSequenceConfigurationRequest request)
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("Processing GetSequenceConfiguration request {RequestId}, SequenceKey: {SequenceKey}",
            requestId, request.SequenceKey);

        try
        {
            // Simulate processing delay
            await _delaySimulator.SimulateDelayAsync();

            // Check for simulated failures
            if (await _delaySimulator.ShouldSimulateFailureAsync())
            {
                _logger.LogWarning("Simulating failure for request {RequestId}", requestId);
                return _responseBuilder.CreateErrorResponse<FetchSequenceConfigurationResponse>(
                    requestId, "Simulated service failure", 500);
            }

            // Validate request
            if (string.IsNullOrWhiteSpace(request.SequenceKey))
            {
                _logger.LogWarning("Invalid sequence key provided for request {RequestId}", requestId);
                return _responseBuilder.CreateErrorResponse<FetchSequenceConfigurationResponse>(
                    requestId, "Sequence key cannot be empty", 400);
            }

            // Get sequence data
            var sequence = await _dataProvider.GetSequenceConfigurationAsync(request.SequenceKey);

            if (sequence == null)
            {
                _logger.LogWarning("Sequence {SequenceKey} not found for request {RequestId}", 
                    request.SequenceKey, requestId);
                return _responseBuilder.CreateErrorResponse<FetchSequenceConfigurationResponse>(
                    requestId, $"Sequence '{request.SequenceKey}' not found", 404);
            }

            _logger.LogInformation("Successfully retrieved sequence {SequenceKey} for request {RequestId}",
                request.SequenceKey, requestId);

            return new FetchSequenceConfigurationResponse(
                requestId,
                sequence,
                Array.Empty<GrpcErrorContract>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetSequenceConfiguration request {RequestId}", requestId);
            return _responseBuilder.CreateErrorResponse<FetchSequenceConfigurationResponse>(
                requestId, $"Internal error: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Fetches a specific resource configuration by its key.
    /// </summary>
    public async Task<FetchResourceConfigurationResponse> GetResourceConfigurationAsync(
        FetchResourceConfigurationRequest request)
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("Processing GetResourceConfiguration request {RequestId}, ResourceKey: {ResourceKey}",
            requestId, request.ResourceKey);

        try
        {
            // Simulate processing delay
            await _delaySimulator.SimulateDelayAsync();

            // Check for simulated failures
            if (await _delaySimulator.ShouldSimulateFailureAsync())
            {
                _logger.LogWarning("Simulating failure for request {RequestId}", requestId);
                return _responseBuilder.CreateErrorResponse<FetchResourceConfigurationResponse>(
                    requestId, "Simulated service failure", 500);
            }

            // Validate request
            if (string.IsNullOrWhiteSpace(request.ResourceKey))
            {
                _logger.LogWarning("Invalid resource key provided for request {RequestId}", requestId);
                return _responseBuilder.CreateErrorResponse<FetchResourceConfigurationResponse>(
                    requestId, "Resource key cannot be empty", 400);
            }

            // Get resource data
            var resource = await _dataProvider.GetResourceConfigurationAsync(request.ResourceKey);

            if (resource == null)
            {
                _logger.LogWarning("Resource {ResourceKey} not found for request {RequestId}", 
                    request.ResourceKey, requestId);
                return _responseBuilder.CreateErrorResponse<FetchResourceConfigurationResponse>(
                    requestId, $"Resource '{request.ResourceKey}' not found", 404);
            }

            _logger.LogInformation("Successfully retrieved resource {ResourceKey} for request {RequestId}",
                request.ResourceKey, requestId);

            return new FetchResourceConfigurationResponse(
                requestId,
                resource,
                Array.Empty<GrpcErrorContract>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GetResourceConfiguration request {RequestId}", requestId);
            return _responseBuilder.CreateErrorResponse<FetchResourceConfigurationResponse>(
                requestId, $"Internal error: {ex.Message}", 500);
        }
    }

    /// <summary>
    /// Simulates reloading the execution configuration.
    /// </summary>
    public async Task<UpdateExecutionConfigurationResponse> ReloadExecutionConfigurationAsync()
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("Processing ReloadExecutionConfiguration request {RequestId}", requestId);

        try
        {
            // Simulate processing delay
            await _delaySimulator.SimulateDelayAsync();

            // Check for simulated failures
            if (await _delaySimulator.ShouldSimulateFailureAsync())
            {
                _logger.LogWarning("Simulating failure for request {RequestId}", requestId);
                return _responseBuilder.CreateErrorResponse<UpdateExecutionConfigurationResponse>(
                    requestId, "Simulated service failure", 500);
            }

            // Simulate reload operation
            await _dataProvider.ReloadConfigurationAsync();

            _logger.LogInformation("Successfully reloaded configuration for request {RequestId}", requestId);

            return new UpdateExecutionConfigurationResponse(
                requestId,
                Array.Empty<GrpcErrorContract>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ReloadExecutionConfiguration request {RequestId}", requestId);
            return _responseBuilder.CreateErrorResponse<UpdateExecutionConfigurationResponse>(
                requestId, $"Internal error: {ex.Message}", 500);
        }
    }
}