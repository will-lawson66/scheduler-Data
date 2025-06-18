using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Instrument.Execution.Grpc.Configuration;
using Instrument.Execution.Grpc.FakeService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Instrument.Execution.Grpc.FakeService.Tests;

public class FakeExecutionConfigurationServiceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;
    private readonly IExecutionConfigurationService _service;

    public FakeExecutionConfigurationServiceTests(ITestOutputHelper output)
    {
        _output = output;

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddFakeExecutionConfigurationServiceForTesting();

        _serviceProvider = services.BuildServiceProvider();
        _service = _serviceProvider.GetRequiredService<IExecutionConfigurationService>();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    [Fact]
    public async Task GetCurrentConfigurationAsync_WithoutSequences_ReturnsEmptySequences()
    {
        // Arrange
        var request = new FetchExecutionConfigurationRequest(IncludeSequences: false);

        // Act
        var response = await _service.GetCurrentConfigurationAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, response.RequestId);
        Assert.NotNull(response.Configuration);
        Assert.Empty(response.Configuration.Sequences);
        Assert.Empty(response.Errors);
        Assert.Equal(1, response.Configuration.StartingPeriod);
        Assert.Equal(1000, response.Configuration.RolloverPeriod);
    }

    [Fact]
    public async Task GetCurrentConfigurationAsync_WithSequences_ReturnsAllSequences()
    {
        // Arrange
        var request = new FetchExecutionConfigurationRequest(IncludeSequences: true);

        // Act
        var response = await _service.GetCurrentConfigurationAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, response.RequestId);
        Assert.NotNull(response.Configuration);
        Assert.NotEmpty(response.Configuration.Sequences);
        Assert.Empty(response.Errors);
        
        _output.WriteLine($"Returned {response.Configuration.Sequences.Count} sequences");
        foreach (var sequence in response.Configuration.Sequences)
        {
            _output.WriteLine($"- {sequence.Key}: {sequence.ExecutionMethod}, {sequence.Resources.Count} resources");
        }
    }

    [Fact]
    public async Task GetSequenceConfigurationAsync_WithValidKey_ReturnsSequence()
    {
        // Arrange
        var request = new FetchSequenceConfigurationRequest("SEQUENCE_001");

        // Act
        var response = await _service.GetSequenceConfigurationAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, response.RequestId);
        Assert.NotNull(response.Sequence);
        Assert.Equal("SEQUENCE_001", response.Sequence.Key);
        Assert.Empty(response.Errors);
        Assert.NotEmpty(response.Sequence.Resources);
        Assert.NotEmpty(response.Sequence.Parameters);
        
        _output.WriteLine($"Sequence: {response.Sequence.Key}");
        _output.WriteLine($"Worst case time: {response.Sequence.WorstCaseTime}");
        _output.WriteLine($"Execution method: {response.Sequence.ExecutionMethod}");
        _output.WriteLine($"Resources: {response.Sequence.Resources.Count}");
        _output.WriteLine($"Parameters: {response.Sequence.Parameters.Count}");
    }

    [Fact]
    public async Task GetSequenceConfigurationAsync_WithInvalidKey_ReturnsNotFound()
    {
        // Arrange
        var request = new FetchSequenceConfigurationRequest("INVALID_SEQUENCE");

        // Act
        var response = await _service.GetSequenceConfigurationAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, response.RequestId);
        Assert.Null(response.Sequence);
        Assert.NotEmpty(response.Errors);
        Assert.Equal(404, response.Errors.First().ErrorCode);
        Assert.Contains("not found", response.Errors.First().Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSequenceConfigurationAsync_WithEmptyKey_ReturnsBadRequest()
    {
        // Arrange
        var request = new FetchSequenceConfigurationRequest("");

        // Act
        var response = await _service.GetSequenceConfigurationAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, response.RequestId);
        Assert.Null(response.Sequence);
        Assert.NotEmpty(response.Errors);
        Assert.Equal(400, response.Errors.First().ErrorCode);
        Assert.Contains("cannot be empty", response.Errors.First().Message);
    }

    [Fact]
    public async Task GetResourceConfigurationAsync_WithValidKey_ReturnsResource()
    {
        // Arrange
        var request = new FetchResourceConfigurationRequest("RESOURCE_001");

        // Act
        var response = await _service.GetResourceConfigurationAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, response.RequestId);
        Assert.NotNull(response.Resource);
        Assert.Equal("RESOURCE_001", response.Resource.Key);
        Assert.Empty(response.Errors);
        Assert.True(response.Resource.HasScriptingInterface);
        Assert.NotEmpty(response.Resource.ScriptingInterface);
        
        _output.WriteLine($"Resource: {response.Resource.Key}");
        _output.WriteLine($"Has scripting interface: {response.Resource.HasScriptingInterface}");
        _output.WriteLine($"Scripting interface: {response.Resource.ScriptingInterface}");
    }

    [Fact]
    public async Task GetResourceConfigurationAsync_WithInvalidKey_ReturnsNotFound()
    {
        // Arrange
        var request = new FetchResourceConfigurationRequest("INVALID_RESOURCE");

        // Act
        var response = await _service.GetResourceConfigurationAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, response.RequestId);
        Assert.Null(response.Resource);
        Assert.NotEmpty(response.Errors);
        Assert.Equal(404, response.Errors.First().ErrorCode);
    }

    [Fact]
    public async Task ReloadExecutionConfigurationAsync_Always_ReturnsSuccess()
    {
        // Act
        var response = await _service.ReloadExecutionConfigurationAsync();

        // Assert
        Assert.NotEqual(Guid.Empty, response.RequestId);
        Assert.Empty(response.Errors);
        
        _output.WriteLine($"Reload completed with request ID: {response.RequestId}");
    }

    [Fact]
    public async Task ServiceOperations_WithDelay_RespectConfiguredDelay()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddFakeExecutionConfigurationServiceForTesting(
            responseDelay: TimeSpan.FromMilliseconds(100));

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IExecutionConfigurationService>();
        var request = new FetchExecutionConfigurationRequest(IncludeSequences: true);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var response = await service.GetCurrentConfigurationAsync(request);
        stopwatch.Stop();

        // Assert
        Assert.NotNull(response.Configuration);
        Assert.True(stopwatch.ElapsedMilliseconds >= 90); // Allow some tolerance
        
        _output.WriteLine($"Operation took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task ConcurrentRequests_AreHandledCorrectly()
    {
        // Arrange
        var request = new FetchExecutionConfigurationRequest(IncludeSequences: true);
        var tasks = new Task<FetchExecutionConfigurationResponse>[5];

        // Act
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = _service.GetCurrentConfigurationAsync(request);
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.All(responses, response =>
        {
            Assert.NotEqual(Guid.Empty, response.RequestId);
            Assert.NotNull(response.Configuration);
            Assert.Empty(response.Errors);
        });

        // Verify unique request IDs
        var requestIds = responses.Select(r => r.RequestId).ToArray();
        Assert.Equal(requestIds.Length, requestIds.Distinct().Count());
        
        _output.WriteLine($"Successfully handled {responses.Length} concurrent requests");
    }
}

public class FakeServiceWithErrorsTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IExecutionConfigurationService _service;

    public FakeServiceWithErrorsTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddFakeExecutionConfigurationServiceWithErrors(failureRate: 1.0); // Always fail

        _serviceProvider = services.BuildServiceProvider();
        _service = _serviceProvider.GetRequiredService<IExecutionConfigurationService>();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    [Fact]
    public async Task GetCurrentConfigurationAsync_WithHighFailureRate_ReturnsErrors()
    {
        // Arrange
        var request = new FetchExecutionConfigurationRequest(IncludeSequences: true);

        // Act
        var response = await _service.GetCurrentConfigurationAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, response.RequestId);
        Assert.Null(response.Configuration);
        Assert.NotEmpty(response.Errors);
        Assert.Equal(500, response.Errors.First().ErrorCode);
        Assert.Contains("Simulated service failure", response.Errors.First().Message);
    }
}

public class FakeDataProviderTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly FakeDataProvider _dataProvider;

    public FakeDataProviderTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.Configure<FakeServiceOptions>(options =>
        {
            options.DataSourcePath = "nonexistent.json"; // Force default data generation
        });
        services.AddSingleton<ConfigurationBuilder>();
        services.AddSingleton<FakeDataProvider>();

        _serviceProvider = services.BuildServiceProvider();
        _dataProvider = _serviceProvider.GetRequiredService<FakeDataProvider>();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    [Fact]
    public async Task GetExecutionConfigurationAsync_WithDefaultData_ReturnsValidConfiguration()
    {
        // Act
        var config = await _dataProvider.GetExecutionConfigurationAsync(includeSequences: true);

        // Assert
        Assert.NotNull(config);
        Assert.True(config.StartingPeriod > 0);
        Assert.True(config.RolloverPeriod > 0);
        Assert.True(config.PeriodSpan > TimeSpan.Zero);
        Assert.NotEmpty(config.Sequences);
    }

    [Fact]
    public async Task GetSequenceConfigurationAsync_WithValidKey_ReturnsSequence()
    {
        // Act
        var sequence = await _dataProvider.GetSequenceConfigurationAsync("SEQUENCE_001");

        // Assert
        Assert.NotNull(sequence);
        Assert.Equal("SEQUENCE_001", sequence.Key);
        Assert.NotEmpty(sequence.Resources);
        Assert.NotEmpty(sequence.Parameters);
    }

    [Fact]
    public async Task ReloadConfigurationAsync_Always_CompletesSuccessfully()
    {
        // Act & Assert
        await _dataProvider.ReloadConfigurationAsync(); // Should not throw
    }
}

public class ConfigurationBuilderTests
{
    private readonly ConfigurationBuilder _builder = new();

    [Fact]
    public void BuildDefaultConfiguration_Always_ReturnsValidConfiguration()
    {
        // Act
        var config = _builder.BuildDefaultConfiguration();

        // Assert
        Assert.NotNull(config);
        Assert.True(config.StartingPeriod > 0);
        Assert.True(config.RolloverPeriod > 0);
        Assert.True(config.PeriodSpan > TimeSpan.Zero);
        Assert.True(config.PeriodAcceleration > 0);
        Assert.NotEmpty(config.Sequences);

        // Verify sequences have required data
        Assert.All(config.Sequences, sequence =>
        {
            Assert.NotEmpty(sequence.Key);
            Assert.True(sequence.WorstCaseTime > TimeSpan.Zero);
            Assert.NotEmpty(sequence.ExecutionMethod);
            Assert.NotEmpty(sequence.Resources);
            Assert.NotEmpty(sequence.Parameters);
        });
    }
}