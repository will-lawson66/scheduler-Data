namespace Instrument.Scheduling.Data.UT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Instrument.Scheduling.Data.Orchestration;
using Instrument.Scheduling.Data.Orchestration.ConfigurationImport;
using Instrument.Scheduling.Data.Orchestration.ConfigurationImport.Steps;
using Microsoft.Extensions.Logging;
using Moq;

public class ConfigurationImportManagerTests
{
    private readonly Mock<ILogger<ConfigurationImportManager>> _mockLogger;
    private readonly List<Mock<IOrchestrationStep>> _mockSteps;
    private readonly ConfigurationImportManager _processManager;

    public ConfigurationImportManagerTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationImportManager>>();
        _mockSteps = new List<Mock<IOrchestrationStep>>();

        // Create mock steps in the order they should execute
        var stepNames = new[] { "ValidateRequest", "ClearExistingData", "InitializeDatabase", "GetConfiguration", "ImportSequences", "ImportResources" };

        foreach (var stepName in stepNames)
        {
            var mockStep = new Mock<IOrchestrationStep>();
            mockStep.Setup(x => x.StepName).Returns(stepName);
            mockStep.Setup(x => x.ExecuteAsync(It.IsAny<OrchestrationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(StepResult.Success());
            _mockSteps.Add(mockStep);
        }

        _processManager = new ConfigurationImportManager(
            _mockSteps.Select(x => x.Object),
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        var request = new ConfigurationImportRequest
        {
            IncludeSequences = true,
            ClearExistingData = false
        };

        // Act
        var result = await _processManager.ExecuteAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Statistics);
        Assert.Equal(6, result.ProcessedSteps.Count);
        Assert.True(result.Duration > TimeSpan.Zero);

        // Verify all steps were executed
        foreach (var mockStep in _mockSteps)
        {
            mockStep.Verify(x => x.ExecuteAsync(It.IsAny<OrchestrationContext>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithFailingStep_ReturnsFailureResult()
    {
        // Arrange
        var request = new ConfigurationImportRequest();
        var failingStepIndex = 2; // InitializeDatabase step
        var errorMessage = "Database initialization failed";

        _mockSteps[failingStepIndex]
            .Setup(x => x.ExecuteAsync(It.IsAny<OrchestrationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StepResult.Failure(errorMessage, shouldContinue: false));

        // Act
        var result = await _processManager.ExecuteAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(errorMessage, result.ErrorMessage);
        Assert.Equal(3, result.ProcessedSteps.Count); // Only first 3 steps should complete

        // Verify steps up to the failing one were executed
        for (int i = 0; i <= failingStepIndex; i++)
        {
            _mockSteps[i].Verify(x => x.ExecuteAsync(It.IsAny<OrchestrationContext>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // Verify subsequent steps were not executed
        for (int i = failingStepIndex + 1; i < _mockSteps.Count; i++)
        {
            _mockSteps[i].Verify(x => x.ExecuteAsync(It.IsAny<OrchestrationContext>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithFailingStepThatContinues_ContinuesExecution()
    {
        // Arrange
        var request = new ConfigurationImportRequest();
        var failingStepIndex = 2;
        var errorMessage = "Non-critical error";

        _mockSteps[failingStepIndex]
            .Setup(x => x.ExecuteAsync(It.IsAny<OrchestrationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StepResult.Failure(errorMessage, shouldContinue: true));

        // Act
        var result = await _processManager.ExecuteAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(errorMessage, result.ErrorMessage);
        Assert.Equal(6, result.ProcessedSteps.Count); // All steps should complete

        // Verify all steps were executed
        foreach (var mockStep in _mockSteps)
        {
            mockStep.Verify(x => x.ExecuteAsync(It.IsAny<OrchestrationContext>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var request = new ConfigurationImportRequest();
        var cts = new CancellationTokenSource();

        _mockSteps[0]
            .Setup(x => x.ExecuteAsync(It.IsAny<OrchestrationContext>(), It.IsAny<CancellationToken>()))
            .Returns<OrchestrationContext, CancellationToken>((ctx, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(StepResult.Success());
            });

        cts.Cancel();

        // Act
        var result = await _processManager.ExecuteAsync(request, cts.Token);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Unexpected error", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnexpectedException_ReturnsFailureResult()
    {
        // Arrange
        var request = new ConfigurationImportRequest();
        var expectedException = new InvalidOperationException("Unexpected error");

        _mockSteps[1]
            .Setup(x => x.ExecuteAsync(It.IsAny<OrchestrationContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _processManager.ExecuteAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Unexpected error", result.ErrorMessage);
        Assert.Contains(expectedException.Message, result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_PassesContextBetweenSteps()
    {
        // Arrange
        var request = new ConfigurationImportRequest();
        OrchestrationContext? capturedContext = null;

        _mockSteps[1]
            .Setup(x => x.ExecuteAsync(It.IsAny<OrchestrationContext>(), It.IsAny<CancellationToken>()))
            .Callback<OrchestrationContext, CancellationToken>((ctx, ct) =>
            {
                capturedContext = ctx;
                ctx.SetData("TestKey", "TestValue");
            })
            .ReturnsAsync(StepResult.Success());

        _mockSteps[2]
            .Setup(x => x.ExecuteAsync(It.IsAny<OrchestrationContext>(), It.IsAny<CancellationToken>()))
            .Callback<OrchestrationContext, CancellationToken>((ctx, ct) =>
            {
                var value = ctx.GetData<string>("TestKey");
                Assert.Equal("TestValue", value);
            })
            .ReturnsAsync(StepResult.Success());

        // Act
        var result = await _processManager.ExecuteAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedContext);
        Assert.Equal(request, capturedContext.GetData<ConfigurationImportRequest>("ImportRequest"));
    }
}

public class OrchestrationContextTests
{
    [Fact]
    public void SetData_AndGetData_WorkCorrectly()
    {
        // Arrange
        var context = new OrchestrationContext();
        var testValue = "test value";
        var testKey = "test key";

        // Act
        context.SetData(testKey, testValue);
        var retrievedValue = context.GetData<string>(testKey);

        // Assert
        Assert.Equal(testValue, retrievedValue);
    }

    [Fact]
    public void GetData_WithNonExistentKey_ReturnsDefault()
    {
        // Arrange
        var context = new OrchestrationContext();

        // Act
        var result = context.GetData<string>("non-existent-key");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetData_WithWrongType_ReturnsDefault()
    {
        // Arrange
        var context = new OrchestrationContext();
        context.SetData("key", "string value");

        // Act
        var result = context.GetData<int>("key");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CompletedSteps_CanBeAddedAndRetrieved()
    {
        // Arrange
        var context = new OrchestrationContext();
        var stepName = "TestStep";

        // Act
        context.CompletedSteps.Add(stepName);

        // Assert
        Assert.Contains(stepName, context.CompletedSteps);
    }

    [Fact]
    public void Errors_CanBeAddedAndRetrieved()
    {
        // Arrange
        var context = new OrchestrationContext();
        var errorMessage = "Test error";

        // Act
        context.Errors.Add(errorMessage);

        // Assert
        Assert.Contains(errorMessage, context.Errors);
    }
}

public class StepResultTests
{
    [Fact]
    public void Success_CreatesSuccessResult()
    {
        // Act
        var result = StepResult.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.ShouldContinue);
    }

    [Fact]
    public void Failure_WithDefaultContinue_CreatesFailureResult()
    {
        // Arrange
        var errorMessage = "Test error";

        // Act
        var result = StepResult.Failure(errorMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(errorMessage, result.ErrorMessage);
        Assert.False(result.ShouldContinue);
    }

    [Fact]
    public void Failure_WithShouldContinueTrue_CreatesFailureResultThatContinues()
    {
        // Arrange
        var errorMessage = "Test error";

        // Act
        var result = StepResult.Failure(errorMessage, shouldContinue: true);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(errorMessage, result.ErrorMessage);
        Assert.True(result.ShouldContinue);
    }
}

public class ValidateRequestStepTests
{
    private readonly Mock<ILogger<ValidateRequestStep>> _mockLogger;
    private readonly ValidateRequestStep _step;

    public ValidateRequestStepTests()
    {
        _mockLogger = new Mock<ILogger<ValidateRequestStep>>();
        _step = new ValidateRequestStep(_mockLogger.Object);
    }

    [Fact]
    public void StepName_ReturnsCorrectName()
    {
        // Assert
        Assert.Equal("ValidateRequest", _step.StepName);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var context = new OrchestrationContext();
        var request = new ConfigurationImportRequest
        {
            IncludeSequences = true,
            ClearExistingData = false
        };
        context.SetData("ImportRequest", request);

        // Act
        var result = await _step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.ShouldContinue);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullRequest_ReturnsFailure()
    {
        // Arrange
        var context = new OrchestrationContext();
        // Don't set the ImportRequest in context

        // Act
        var result = await _step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Import request is null", result.ErrorMessage);
        Assert.False(result.ShouldContinue);
    }
}

public class ConfigurationImportRequestTests
{
    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Act
        var request = new ConfigurationImportRequest();

        // Assert
        Assert.True(request.IncludeSequences);
        Assert.False(request.ClearExistingData);
        Assert.Empty(request.SequenceFilters);
        Assert.Empty(request.ResourceFilters);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var sequenceFilters = new List<string> { "filter1", "filter2" };
        var resourceFilters = new List<string> { "resource1" };

        // Act
        var request = new ConfigurationImportRequest
        {
            IncludeSequences = false,
            ClearExistingData = true,
            SequenceFilters = sequenceFilters,
            ResourceFilters = resourceFilters
        };

        // Assert
        Assert.False(request.IncludeSequences);
        Assert.True(request.ClearExistingData);
        Assert.Equal(sequenceFilters, request.SequenceFilters);
        Assert.Equal(resourceFilters, request.ResourceFilters);
    }
}

public class ConfigurationImportResultTests
{
    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Act
        var result = new ConfigurationImportResult();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(TimeSpan.Zero, result.Duration);
        Assert.NotNull(result.Statistics);
        Assert.Empty(result.ProcessedSteps);
        Assert.Null(result.RequestId);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5);
        var statistics = new ImportStatistics { SequencesProcessed = 10 };
        var processedSteps = new List<string> { "Step1", "Step2" };
        var requestId = Guid.NewGuid().ToString();
        var errorMessage = "Test error";

        // Act
        var result = new ConfigurationImportResult
        {
            IsSuccess = true,
            ErrorMessage = errorMessage,
            Duration = duration,
            Statistics = statistics,
            ProcessedSteps = processedSteps,
            RequestId = requestId
        };

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(errorMessage, result.ErrorMessage);
        Assert.Equal(duration, result.Duration);
        Assert.Equal(statistics, result.Statistics);
        Assert.Equal(processedSteps, result.ProcessedSteps);
        Assert.Equal(requestId, result.RequestId);
    }
}

public class ImportStatisticsTests
{
    [Fact]
    public void DefaultValues_AreZero()
    {
        // Act
        var statistics = new ImportStatistics();

        // Assert
        Assert.Equal(0, statistics.SequencesProcessed);
        Assert.Equal(0, statistics.ResourcesProcessed);
        Assert.Equal(0, statistics.ParametersProcessed);
        Assert.Equal(0, statistics.SequenceParameterLinksCreated);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Act
        var statistics = new ImportStatistics
        {
            SequencesProcessed = 5,
            ResourcesProcessed = 10,
            ParametersProcessed = 15,
            SequenceParameterLinksCreated = 20
        };

        // Assert
        Assert.Equal(5, statistics.SequencesProcessed);
        Assert.Equal(10, statistics.ResourcesProcessed);
        Assert.Equal(15, statistics.ParametersProcessed);
        Assert.Equal(20, statistics.SequenceParameterLinksCreated);
    }
}

// Integration test for the complete orchestration flow
public class OrchestrationIntegrationTests
{
    [Fact]
    public async Task CompleteOrchestrationFlow_WithMockedSteps_ExecutesCorrectly()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ConfigurationImportManager>>();
        var steps = new List<IOrchestrationStep>
        {
            new TestStep("ValidateRequest", shouldSucceed: true),
            new TestStep("ImportSequences", shouldSucceed: true),
            new TestStep("ImportResources", shouldSucceed: true)
        };

        var manager = new ConfigurationImportManager(steps, mockLogger.Object);
        var request = new ConfigurationImportRequest
        {
            IncludeSequences = true,
            SequenceFilters = new List<string> { "Sequence1", "Sequence2" }
        };

        // Act
        var result = await manager.ExecuteAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.ProcessedSteps.Count);
        Assert.Contains("ValidateRequest", result.ProcessedSteps);
        Assert.Contains("ImportSequences", result.ProcessedSteps);
        Assert.Contains("ImportResources", result.ProcessedSteps);
    }

    public class TestStep : IOrchestrationStep
    {
        private readonly bool _shouldSucceed;

        public TestStep(string stepName, bool shouldSucceed = true)
        {
            StepName = stepName;
            _shouldSucceed = shouldSucceed;
        }

        public string StepName { get; }

        public Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
        {
            // Simulate some work and context manipulation
            context.SetData($"{StepName}Executed", true);

            return Task.FromResult(_shouldSucceed
                ? StepResult.Success()
                : StepResult.Failure($"{StepName} failed"));
        }
    }
}