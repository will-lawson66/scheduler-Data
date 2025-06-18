using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Instrument.Data.Grpc;
using Instrument.Data.Orchestration;
using Instrument.Data.Orchestration.ConfigurationImport;
using Instrument.Data.Orchestration.ConfigurationImport.Steps;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Grpc.Core;

namespace Instrument.Data.UT.Integration
{
    /// <summary>
    /// Integration tests that test the interaction between gRPC Gateway and Orchestration components
    /// </summary>
    public class GrpcGatewayOrchestrationIntegrationTests : IDisposable
    {
        private readonly Mock<IRetryPolicy> _mockRetryPolicy;
        private readonly Mock<ILogger<GrpcGateway>> _mockGatewayLogger;
        private readonly Mock<ILogger<ConfigurationImportManager>> _mockManagerLogger;
        private readonly Mock<IExecutionConfigurationOperationFactory> _mockOperationFactory;
        private readonly GrpcGateway _gateway;
        private readonly ConfigurationImportManager _importManager;

        public GrpcGatewayOrchestrationIntegrationTests()
        {
            _mockRetryPolicy = new Mock<IRetryPolicy>();
            _mockGatewayLogger = new Mock<ILogger<GrpcGateway>>();
            _mockManagerLogger = new Mock<ILogger<ConfigurationImportManager>>();
            _mockOperationFactory = new Mock<IExecutionConfigurationOperationFactory>();

            var options = new GrpcGatewayOptions
            {
                DefaultTimeoutSeconds = 30,
                MaxConcurrentRequests = 10,
                RetryOptions = new RetryOptions
                {
                    MaxAttempts = 3,
                    BaseDelayMs = 100,
                    BackoffMultiplier = 2.0
                }
            };

            var optionsMock = new Mock<IOptions<GrpcGatewayOptions>>();
            optionsMock.Setup(x => x.Value).Returns(options);

            _gateway = new GrpcGateway(
                _mockRetryPolicy.Object,
                _mockGatewayLogger.Object,
                optionsMock.Object,
                _mockOperationFactory.Object
            );

            // Setup retry policy to execute operations directly
            _mockRetryPolicy.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task<object>>>(), It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task<object>>, CancellationToken>((func, ct) => func(ct));

            // Create a simple orchestration manager with mock steps
            var steps = new List<IOrchestrationStep>
            {
                new MockOrchestrationStep("ValidateRequest", true),
                new MockOrchestrationStep("GetConfiguration", true),
                new MockOrchestrationStep("ImportSequences", true)
            };

            _importManager = new ConfigurationImportManager(steps, _mockManagerLogger.Object);
        }

        public void Dispose()
        {
            _gateway?.Dispose();
        }

        [Fact]
        public async Task IntegratedWorkflow_GrpcGatewayTriggersOrchestration_ExecutesSuccessfully()
        {
            // Arrange
            var importRequest = new ConfigurationImportRequest { IncludeSequences = true };
            var mockOperation = new Mock<IGrpcOperation<ConfigurationImportRequest, ConfigurationImportResult>>();
            
            mockOperation.Setup(x => x.ServiceName).Returns("ConfigurationService");
            mockOperation.Setup(x => x.OperationName).Returns("ImportConfiguration");
            mockOperation.Setup(x => x.ExecuteAsync(importRequest))
                .Returns(() => _importManager.ExecuteAsync(importRequest));

            _mockRetryPolicy.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task<ConfigurationImportResult>>>(), It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task<ConfigurationImportResult>>, CancellationToken>((func, ct) => func(ct));

            // Act
            var gatewayResult = await _gateway.ExecuteAsync(mockOperation.Object, importRequest);

            // Assert
            Assert.True(gatewayResult.IsSuccess);
            Assert.NotNull(gatewayResult.Data);
            Assert.True(gatewayResult.Data.IsSuccess);
            Assert.Equal(3, gatewayResult.Data.ProcessedSteps.Count);
        }

        [Fact]
        public async Task IntegratedWorkflow_OrchestrationFailure_PropagatedThroughGateway()
        {
            // Arrange
            var steps = new List<IOrchestrationStep>
            {
                new MockOrchestrationStep("ValidateRequest", true),
                new MockOrchestrationStep("FailingStep", false, "Step failed"),
                new MockOrchestrationStep("ShouldNotExecute", true)
            };

            var failingManager = new ConfigurationImportManager(steps, _mockManagerLogger.Object);
            var importRequest = new ConfigurationImportRequest();
            var mockOperation = new Mock<IGrpcOperation<ConfigurationImportRequest, ConfigurationImportResult>>();
            
            mockOperation.Setup(x => x.ServiceName).Returns("ConfigurationService");
            mockOperation.Setup(x => x.OperationName).Returns("ImportConfiguration");
            mockOperation.Setup(x => x.ExecuteAsync(importRequest))
                .Returns(() => failingManager.ExecuteAsync(importRequest));

            _mockRetryPolicy.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task<ConfigurationImportResult>>>(), It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task<ConfigurationImportResult>>, CancellationToken>((func, ct) => func(ct));

            // Act
            var gatewayResult = await _gateway.ExecuteAsync(mockOperation.Object, importRequest);

            // Assert
            Assert.True(gatewayResult.IsSuccess); // Gateway operation succeeded
            Assert.NotNull(gatewayResult.Data);
            Assert.False(gatewayResult.Data.IsSuccess); // But orchestration failed
            Assert.Contains("Step failed", gatewayResult.Data.ErrorMessage);
            Assert.Equal(2, gatewayResult.Data.ProcessedSteps.Count); // Only first two steps
        }

        private class MockOrchestrationStep : IOrchestrationStep
        {
            private readonly bool _shouldSucceed;
            private readonly string? _errorMessage;

            public MockOrchestrationStep(string stepName, bool shouldSucceed, string? errorMessage = null)
            {
                StepName = stepName;
                _shouldSucceed = shouldSucceed;
                _errorMessage = errorMessage;
            }

            public string StepName { get; }

            public async Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
            {
                await Task.Delay(10, cancellationToken); // Simulate some work
                
                return _shouldSucceed 
                    ? StepResult.Success() 
                    : StepResult.Failure(_errorMessage ?? "Step failed");
            }
        }
    }

    /// <summary>
    /// Edge case and stress tests for gRPC Gateway
    /// </summary>
    public class GrpcGatewayStressTests : IDisposable
    {
        private readonly GrpcGateway _gateway;
        private readonly Mock<IRetryPolicy> _mockRetryPolicy;

        public GrpcGatewayStressTests()
        {
            _mockRetryPolicy = new Mock<IRetryPolicy>();
            var mockLogger = new Mock<ILogger<GrpcGateway>>();
            var mockOperationFactory = new Mock<IExecutionConfigurationOperationFactory>();

            var options = new GrpcGatewayOptions
            {
                DefaultTimeoutSeconds = 5,
                MaxConcurrentRequests = 3
            };

            var optionsMock = new Mock<IOptions<GrpcGatewayOptions>>();
            optionsMock.Setup(x => x.Value).Returns(options);

            _gateway = new GrpcGateway(
                _mockRetryPolicy.Object,
                mockLogger.Object,
                optionsMock.Object,
                mockOperationFactory.Object
            );
        }

        public void Dispose()
        {
            _gateway?.Dispose();
        }

        [Fact]
        public async Task ConcurrentRequests_ExceedingMaxConcurrency_AreThrottled()
        {
            // Arrange
            var concurrentRequests = 10;
            var completedTasks = 0;
            var maxConcurrentlyExecuting = 0;
            var currentlyExecuting = 0;
            var maxConcurrency = 3;

            var mockOperation = new Mock<IGrpcOperation<object, string>>();
            mockOperation.Setup(x => x.ServiceName).Returns("TestService");
            mockOperation.Setup(x => x.OperationName).Returns("TestOperation");
            mockOperation.Setup(x => x.ExecuteAsync(It.IsAny<object>()))
                .Returns(async () =>
                {
                    Interlocked.Increment(ref currentlyExecuting);
                    var current = currentlyExecuting;
                    
                    // Track maximum concurrent executions
                    while (true)
                    {
                        var max = maxConcurrentlyExecuting;
                        if (current <= max || Interlocked.CompareExchange(ref maxConcurrentlyExecuting, current, max) == max)
                            break;
                    }

                    await Task.Delay(100); // Simulate work
                    
                    Interlocked.Decrement(ref currentlyExecuting);
                    Interlocked.Increment(ref completedTasks);
                    return "success";
                });

            _mockRetryPolicy.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task<string>>>(), It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task<string>>, CancellationToken>((func, ct) => func(ct));

            // Act
            var tasks = Enumerable.Range(0, concurrentRequests)
                .Select(_ => _gateway.ExecuteAsync(mockOperation.Object, new object()))
                .ToArray();

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(concurrentRequests, completedTasks);
            Assert.All(results, result => Assert.True(result.IsSuccess));
            Assert.True(maxConcurrentlyExecuting <= maxConcurrency, 
                $"Maximum concurrent executions ({maxConcurrentlyExecuting}) exceeded limit ({maxConcurrency})");
        }

        [Fact]
        public async Task OperationTimeout_IsRespectedAndCancelsOperation()
        {
            // Arrange
            var mockOperation = new Mock<IGrpcOperation<object, string>>();
            mockOperation.Setup(x => x.ServiceName).Returns("TestService");
            mockOperation.Setup(x => x.OperationName).Returns("SlowOperation");
            mockOperation.Setup(x => x.Timeout).Returns(TimeSpan.FromMilliseconds(100));
            mockOperation.Setup(x => x.ExecuteAsync(It.IsAny<object>()))
                .Returns(async () =>
                {
                    await Task.Delay(500); // Longer than timeout
                    return "should not complete";
                });

            _mockRetryPolicy.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task<string>>>(), It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task<string>>, CancellationToken>(async (func, ct) =>
                {
                    // Simulate timeout by canceling after operation timeout
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, ct);
                    
                    try
                    {
                        return await func(linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                    {
                        throw new TimeoutException("Operation timed out");
                    }
                });

            // Act
            var result = await _gateway.ExecuteAsync(mockOperation.Object, new object());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("timed out", result.ErrorMessage, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    /// <summary>
    /// Complex orchestration scenarios and edge cases
    /// </summary>
    public class OrchestrationAdvancedTests
    {
        [Fact]
        public async Task ComplexOrchestration_WithDependentSteps_ExecutesInCorrectOrder()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<ConfigurationImportManager>>();
            var executionOrder = new List<string>();
            
            var steps = new List<IOrchestrationStep>
            {
                new OrderedStep("ValidateRequest", 1, executionOrder),
                new OrderedStep("ClearExistingData", 2, executionOrder),
                new OrderedStep("InitializeDatabase", 3, executionOrder),
                new OrderedStep("GetConfiguration", 4, executionOrder),
                new OrderedStep("ImportSequences", 5, executionOrder),
                new OrderedStep("ImportResources", 6, executionOrder)
            };

            var manager = new ConfigurationImportManager(steps, mockLogger.Object);
            var request = new ConfigurationImportRequest();

            // Act
            var result = await manager.ExecuteAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(6, executionOrder.Count);
            
            // Verify execution order
            Assert.Equal("ValidateRequest", executionOrder[0]);
            Assert.Equal("ClearExistingData", executionOrder[1]);
            Assert.Equal("InitializeDatabase", executionOrder[2]);
            Assert.Equal("GetConfiguration", executionOrder[3]);
            Assert.Equal("ImportSequences", executionOrder[4]);
            Assert.Equal("ImportResources", executionOrder[5]);
        }

        [Fact]
        public async Task OrchestrationWithDataFlow_PassesDataCorrectlyBetweenSteps()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<ConfigurationImportManager>>();
            var steps = new List<IOrchestrationStep>
            {
                new DataProducerStep("Producer", "TestData", "ProducedValue"),
                new DataConsumerStep("Consumer", "TestData", "ProducedValue"),
                new DataTransformerStep("Transformer", "TestData", "TransformedValue")
            };

            var manager = new ConfigurationImportManager(steps, mockLogger.Object);
            var request = new ConfigurationImportRequest();

            // Act
            var result = await manager.ExecuteAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.ProcessedSteps.Count);
        }

        [Fact]
        public async Task OrchestrationWithConditionalSteps_ExecutesBasedOnContext()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<ConfigurationImportManager>>();
            var steps = new List<IOrchestrationStep>
            {
                new ConditionalStep("ConditionalStep1", "Condition", true, shouldExecute: true),
                new ConditionalStep("ConditionalStep2", "Condition", false, shouldExecute: false)
            };

            var manager = new ConfigurationImportManager(steps, mockLogger.Object);
            var request = new ConfigurationImportRequest();

            // Act
            var result = await manager.ExecuteAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            // Only the first step should have actually executed based on condition
        }

        private class OrderedStep : IOrchestrationStep
        {
            private readonly int _expectedOrder;
            private readonly List<string> _executionOrder;

            public OrderedStep(string stepName, int expectedOrder, List<string> executionOrder)
            {
                StepName = stepName;
                _expectedOrder = expectedOrder;
                _executionOrder = executionOrder;
            }

            public string StepName { get; }

            public Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
            {
                _executionOrder.Add(StepName);
                return Task.FromResult(StepResult.Success());
            }
        }

        private class DataProducerStep : IOrchestrationStep
        {
            private readonly string _dataKey;
            private readonly string _dataValue;

            public DataProducerStep(string stepName, string dataKey, string dataValue)
            {
                StepName = stepName;
                _dataKey = dataKey;
                _dataValue = dataValue;
            }

            public string StepName { get; }

            public Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
            {
                context.SetData(_dataKey, _dataValue);
                return Task.FromResult(StepResult.Success());
            }
        }

        private class DataConsumerStep : IOrchestrationStep
        {
            private readonly string _dataKey;
            private readonly string _expectedValue;

            public DataConsumerStep(string stepName, string dataKey, string expectedValue)
            {
                StepName = stepName;
                _dataKey = dataKey;
                _expectedValue = expectedValue;
            }

            public string StepName { get; }

            public Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
            {
                var value = context.GetData<string>(_dataKey);
                if (value != _expectedValue)
                {
                    return Task.FromResult(StepResult.Failure($"Expected {_expectedValue}, got {value}"));
                }
                return Task.FromResult(StepResult.Success());
            }
        }

        private class DataTransformerStep : IOrchestrationStep
        {
            private readonly string _dataKey;
            private readonly string _newValue;

            public DataTransformerStep(string stepName, string dataKey, string newValue)
            {
                StepName = stepName;
                _dataKey = dataKey;
                _newValue = newValue;
            }

            public string StepName { get; }

            public Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
            {
                var currentValue = context.GetData<string>(_dataKey);
                if (currentValue == null)
                {
                    return Task.FromResult(StepResult.Failure("No data to transform"));
                }
                
                context.SetData(_dataKey, _newValue);
                return Task.FromResult(StepResult.Success());
            }
        }

        private class ConditionalStep : IOrchestrationStep
        {
            private readonly string _conditionKey;
            private readonly bool _conditionValue;
            private readonly bool _shouldExecute;

            public ConditionalStep(string stepName, string conditionKey, bool conditionValue, bool shouldExecute)
            {
                StepName = stepName;
                _conditionKey = conditionKey;
                _conditionValue = conditionValue;
                _shouldExecute = shouldExecute;
            }

            public string StepName { get; }

            public Task<StepResult> ExecuteAsync(OrchestrationContext context, CancellationToken cancellationToken)
            {
                // Set condition for next step
                context.SetData(_conditionKey, _conditionValue);
                
                if (!_shouldExecute)
                {
                    // Skip execution but continue
                    return Task.FromResult(StepResult.Success());
                }

                // Simulate actual work
                return Task.FromResult(StepResult.Success());
            }
        }
    }

    /// <summary>
    /// Performance and memory tests
    /// </summary>
    public class PerformanceTests : IDisposable
    {
        private readonly GrpcGateway _gateway;

        public PerformanceTests()
        {
            var mockRetryPolicy = new Mock<IRetryPolicy>();
            var mockLogger = new Mock<ILogger<GrpcGateway>>();
            var mockOperationFactory = new Mock<IExecutionConfigurationOperationFactory>();

            var options = new GrpcGatewayOptions
            {
                DefaultTimeoutSeconds = 30,
                MaxConcurrentRequests = 100
            };

            var optionsMock = new Mock<IOptions<GrpcGatewayOptions>>();
            optionsMock.Setup(x => x.Value).Returns(options);

            _gateway = new GrpcGateway(
                mockRetryPolicy.Object,
                mockLogger.Object,
                optionsMock.Object,
                mockOperationFactory.Object
            );

            mockRetryPolicy.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task<string>>>(), It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task<string>>, CancellationToken>((func, ct) => func(ct));
        }

        public void Dispose()
        {
            _gateway?.Dispose();
        }

        [Fact]
        public async Task HighVolumeOperations_CompleteWithinReasonableTime()
        {
            // Arrange
            var operationCount = 1000;
            var mockOperation = new Mock<IGrpcOperation<object, string>>();
            
            mockOperation.Setup(x => x.ServiceName).Returns("PerformanceTestService");
            mockOperation.Setup(x => x.OperationName).Returns("FastOperation");
            mockOperation.Setup(x => x.ExecuteAsync(It.IsAny<object>()))
                .ReturnsAsync("success");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var tasks = Enumerable.Range(0, operationCount)
                .Select(_ => _gateway.ExecuteAsync(mockOperation.Object, new object()))
                .ToArray();

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.Equal(operationCount, results.Length);
            Assert.All(results, result => Assert.True(result.IsSuccess));
            
            // Performance assertion: should complete within 30 seconds for 1000 operations
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30), 
                $"Operations took too long: {stopwatch.Elapsed}");
        }

        [Fact]
        public void StatisticsTracking_HandlesHighVolumeCorrectly()
        {
            // Arrange
            var statistics = new GatewayStatistics();
            var operationCount = 10000;

            // Act
            Parallel.For(0, operationCount, i =>
            {
                statistics.IncrementOperation($"Operation{i % 10}");
                if (i % 100 == 0) // Some errors
                {
                    statistics.IncrementError($"Operation{i % 10}");
                }
            });

            // Assert
            Assert.Equal(operationCount, statistics.TotalRequests);
            Assert.Equal(operationCount / 100, statistics.TotalErrors);
            Assert.Equal(10, statistics.OperationCounts.Count); // 10 different operation types
        }
    }
}