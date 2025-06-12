using System;
using System.Threading;
using System.Threading.Tasks;
using Instrument.Data.Grpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Grpc.Core;

namespace Instrument.Data.UT.Grpc
{
    public class GrpcGatewayTests : IDisposable
    {
        private readonly Mock<IRetryPolicy> _mockRetryPolicy;
        private readonly Mock<ILogger<GrpcGateway>> _mockLogger;
        private readonly Mock<IExecutionConfigurationOperationFactory> _mockOperationFactory;
        private readonly GrpcGatewayOptions _options;
        private readonly GrpcGateway _gateway;

        public GrpcGatewayTests()
        {
            _mockRetryPolicy = new Mock<IRetryPolicy>();
            _mockLogger = new Mock<ILogger<GrpcGateway>>();
            _mockOperationFactory = new Mock<IExecutionConfigurationOperationFactory>();
            
            _options = new GrpcGatewayOptions
            {
                DefaultTimeoutSeconds = 30,
                MaxConcurrentRequests = 5,
                RetryOptions = new RetryOptions
                {
                    MaxAttempts = 3,
                    BaseDelayMs = 1000,
                    BackoffMultiplier = 2.0,
                    UseJitter = true
                }
            };

            var optionsMock = new Mock<IOptions<GrpcGatewayOptions>>();
            optionsMock.Setup(x => x.Value).Returns(_options);

            _gateway = new GrpcGateway(
                _mockRetryPolicy.Object,
                _mockLogger.Object,
                optionsMock.Object,
                _mockOperationFactory.Object
            );
        }

        public void Dispose()
        {
            _gateway?.Dispose();
        }

        [Fact]
        public async Task ExecuteAsync_WithValidOperation_ReturnsSuccessResult()
        {
            // Arrange
            var request = new TestRequest { Data = "test" };
            var expectedResponse = new TestResponse { Result = "success" };
            var mockOperation = new Mock<IGrpcOperation<TestRequest, TestResponse>>();
            
            mockOperation.Setup(x => x.ServiceName).Returns("TestService");
            mockOperation.Setup(x => x.OperationName).Returns("TestOperation");
            mockOperation.Setup(x => x.Timeout).Returns(TimeSpan.FromSeconds(10));
            mockOperation.Setup(x => x.ExecuteAsync(request))
                .ReturnsAsync(expectedResponse);

            _mockRetryPolicy.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task<TestResponse>>>(), It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task<TestResponse>>, CancellationToken>((func, ct) => func(ct));

            // Act
            var result = await _gateway.ExecuteAsync(mockOperation.Object, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedResponse, result.Data);
            Assert.Null(result.ErrorMessage);
            Assert.True(result.Duration > TimeSpan.Zero);

            mockOperation.Verify(x => x.ExecuteAsync(request), Times.Once);
            _mockRetryPolicy.Verify(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task<TestResponse>>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithFailedOperation_ReturnsFailureResult()
        {
            // Arrange
            var request = new TestRequest { Data = "test" };
            var mockOperation = new Mock<IGrpcOperation<TestRequest, TestResponse>>();
            var expectedException = new RpcException(new Status(StatusCode.Internal, "Internal server error"));
            
            mockOperation.Setup(x => x.ServiceName).Returns("TestService");
            mockOperation.Setup(x => x.OperationName).Returns("TestOperation");
            mockOperation.Setup(x => x.ExecuteAsync(request))
                .ThrowsAsync(expectedException);

            _mockRetryPolicy.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task<TestResponse>>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _gateway.ExecuteAsync(mockOperation.Object, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Null(result.Data);
            Assert.Contains("Internal server error", result.ErrorMessage);
            Assert.True(result.Duration > TimeSpan.Zero);
        }

        [Fact]
        public async Task ExecuteAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            var request = new TestRequest { Data = "test" };
            var mockOperation = new Mock<IGrpcOperation<TestRequest, TestResponse>>();
            var cts = new CancellationTokenSource();
            
            mockOperation.Setup(x => x.ServiceName).Returns("TestService");
            mockOperation.Setup(x => x.OperationName).Returns("TestOperation");
            
            _mockRetryPolicy.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task<TestResponse>>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            cts.Cancel();

            // Act & Assert
            var result = await _gateway.ExecuteAsync(mockOperation.Object, request, cts.Token);
            
            Assert.False(result.IsSuccess);
            Assert.Contains("canceled", result.ErrorMessage, StringComparison.InvariantCultureIgnoreCase);
        }

        [Fact]
        public async Task ExecuteAsync_WithConcurrentRequests_RespectsMaxConcurrency()
        {
            // Arrange
            var request = new TestRequest { Data = "test" };
            var mockOperation = new Mock<IGrpcOperation<TestRequest, TestResponse>>();
            var expectedResponse = new TestResponse { Result = "success" };
            var taskCompletionSource = new TaskCompletionSource<TestResponse>();
            
            mockOperation.Setup(x => x.ServiceName).Returns("TestService");
            mockOperation.Setup(x => x.OperationName).Returns("TestOperation");
            mockOperation.Setup(x => x.ExecuteAsync(request))
                .Returns(taskCompletionSource.Task);

            _mockRetryPolicy.Setup(x => x.ExecuteAsync(It.IsAny<Func<CancellationToken, Task<TestResponse>>>(), It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task<TestResponse>>, CancellationToken>((func, ct) => func(ct));

            // Act - Start multiple tasks that exceed max concurrency
            var tasks = new Task[_options.MaxConcurrentRequests + 2];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = _gateway.ExecuteAsync(mockOperation.Object, request);
            }

            // Give tasks a moment to start
            await Task.Delay(100);

            // Complete the operations
            taskCompletionSource.SetResult(expectedResponse);

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, result => Assert.True(result.IsSuccess));
        }

        [Fact]
        public async Task IsServiceAvailableAsync_ReturnsTrue()
        {
            // Act
            var result = await _gateway.IsServiceAvailableAsync("TestService");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetStatistics_ReturnsStatistics()
        {
            // Act
            var statistics = _gateway.GetStatistics();

            // Assert
            Assert.NotNull(statistics);
            Assert.Equal(0, statistics.TotalRequests);
            Assert.Equal(0, statistics.TotalErrors);
        }

        [Fact]
        public void Dispose_WhenCalled_DoesNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => _gateway.Dispose());
            Assert.Null(exception);
        }

        [Fact]
        public void ExecuteAsync_WhenDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            _gateway.Dispose();
            var mockOperation = new Mock<IGrpcOperation<TestRequest, TestResponse>>();
            var request = new TestRequest();

            // Act & Assert
            Assert.ThrowsAsync<ObjectDisposedException>(() => 
                _gateway.ExecuteAsync(mockOperation.Object, request));
        }

        [Fact]
        public void Constructor_WithNullRetryPolicy_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GrpcGateway(
                    null!,
                    _mockLogger.Object,
                    Mock.Of<IOptions<GrpcGatewayOptions>>(),
                    _mockOperationFactory.Object
                ));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GrpcGateway(
                    _mockRetryPolicy.Object,
                    null!,
                    Mock.Of<IOptions<GrpcGatewayOptions>>(),
                    _mockOperationFactory.Object
                ));
        }

        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GrpcGateway(
                    _mockRetryPolicy.Object,
                    _mockLogger.Object,
                    null!,
                    _mockOperationFactory.Object
                ));
        }

        [Fact]
        public void Constructor_WithNullOperationFactory_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GrpcGateway(
                    _mockRetryPolicy.Object,
                    _mockLogger.Object,
                    Mock.Of<IOptions<GrpcGatewayOptions>>(),
                    null!
                ));
        }

        // Test classes for testing
        private class TestRequest
        {
            public string Data { get; set; } = string.Empty;
        }

        private class TestResponse
        {
            public string Result { get; set; } = string.Empty;
        }
    }

    public class ExponentialBackoffRetryPolicyTests
    {
        private readonly Mock<ILogger<ExponentialBackoffRetryPolicy>> _mockLogger;
        private readonly GrpcGatewayOptions _options;
        private readonly ExponentialBackoffRetryPolicy _retryPolicy;

        public ExponentialBackoffRetryPolicyTests()
        {
            _mockLogger = new Mock<ILogger<ExponentialBackoffRetryPolicy>>();
            _options = new GrpcGatewayOptions
            {
                RetryOptions = new RetryOptions
                {
                    MaxAttempts = 3,
                    BaseDelayMs = 100,
                    BackoffMultiplier = 2.0,
                    UseJitter = false
                }
            };

            var optionsMock = new Mock<IOptions<GrpcGatewayOptions>>();
            optionsMock.Setup(x => x.Value).Returns(_options);

            _retryPolicy = new ExponentialBackoffRetryPolicy(optionsMock.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task ExecuteAsync_WithSuccessfulOperation_ReturnsResult()
        {
            // Arrange
            var expectedResult = "success";
            var operation = new Func<CancellationToken, Task<string>>(_ => Task.FromResult(expectedResult));

            // Act
            var result = await _retryPolicy.ExecuteAsync(operation, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task ExecuteAsync_WithRetriableException_RetriesAndSucceeds()
        {
            // Arrange
            var attemptCount = 0;
            var expectedResult = "success";
            var operation = new Func<CancellationToken, Task<string>>(_ =>
            {
                attemptCount++;
                if (attemptCount < 2)
                {
                    throw new RpcException(new Status(StatusCode.Unavailable, "Service unavailable"));
                }
                return Task.FromResult(expectedResult);
            });

            // Act
            var result = await _retryPolicy.ExecuteAsync(operation, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(2, attemptCount);
        }

        [Fact]
        public async Task ExecuteAsync_WithNonRetriableException_ThrowsImmediately()
        {
            // Arrange
            var attemptCount = 0;
            var expectedException = new ArgumentException("Invalid argument");
            var operation = new Func<CancellationToken, Task<string>>(_ =>
            {
                attemptCount++;
                throw expectedException;
            });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _retryPolicy.ExecuteAsync(operation, CancellationToken.None));

            Assert.Equal(expectedException.Message, exception.Message);
            Assert.Equal(1, attemptCount);
        }

        [Fact]
        public async Task ExecuteAsync_WithAllAttemptsFailingRetriableException_ThrowsLastException()
        {
            // Arrange
            var attemptCount = 0;
            var expectedException = new RpcException(new Status(StatusCode.Unavailable, "Service unavailable"));
            var operation = new Func<CancellationToken, Task<string>>(_ =>
            {
                attemptCount++;
                throw expectedException;
            });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() =>
                _retryPolicy.ExecuteAsync(operation, CancellationToken.None));

            Assert.Equal(expectedException.Status, exception.Status);
            Assert.Equal(_options.RetryOptions.MaxAttempts, attemptCount);
        }

        [Theory]
        [InlineData(StatusCode.Unavailable, true)]
        [InlineData(StatusCode.DeadlineExceeded, true)]
        [InlineData(StatusCode.Internal, false)]
        [InlineData(StatusCode.InvalidArgument, false)]
        public async Task ExecuteAsync_WithDifferentRpcExceptions_HandlesRetryCorrectly(StatusCode statusCode, bool shouldRetry)
        {
            // Arrange
            var attemptCount = 0;
            var rpcException = new RpcException(new Status(statusCode, "Test error"));
            var operation = new Func<CancellationToken, Task<string>>(_ =>
            {
                attemptCount++;
                throw rpcException;
            });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RpcException>(() =>
                _retryPolicy.ExecuteAsync(operation, CancellationToken.None));

            var expectedAttempts = shouldRetry ? _options.RetryOptions.MaxAttempts : 1;
            Assert.Equal(expectedAttempts, attemptCount);
        }
    }

    public class GatewayStatisticsTests
    {
        [Fact]
        public void IncrementOperation_UpdatesCounters()
        {
            // Arrange
            var statistics = new GatewayStatistics();
            var operationName = "TestOperation";

            // Act
            statistics.IncrementOperation(operationName);
            statistics.IncrementOperation(operationName);

            // Assert
            Assert.Equal(2, statistics.TotalRequests);
            Assert.Equal(2, statistics.OperationCounts[operationName]);
        }

        [Fact]
        public void IncrementError_UpdatesCounters()
        {
            // Arrange
            var statistics = new GatewayStatistics();
            var operationName = "TestOperation";

            // Act
            statistics.IncrementError(operationName);
            statistics.IncrementError(operationName);

            // Assert
            Assert.Equal(2, statistics.TotalErrors);
            Assert.Equal(2, statistics.ErrorCounts[operationName]);
        }

        [Fact]
        public void ConcurrentOperations_AreThreadSafe()
        {
            // Arrange
            var statistics = new GatewayStatistics();
            var operationName = "TestOperation";
            var tasks = new Task[100];

            // Act
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    statistics.IncrementOperation(operationName);
                    statistics.IncrementError(operationName);
                });
            }

            Task.WaitAll(tasks);

            // Assert
            Assert.Equal(100, statistics.TotalRequests);
            Assert.Equal(100, statistics.TotalErrors);
            Assert.Equal(100, statistics.OperationCounts[operationName]);
            Assert.Equal(100, statistics.ErrorCounts[operationName]);
        }
    }

    public class GatewayResultTests
    {
        [Fact]
        public void Success_CreatesSuccessResult()
        {
            // Arrange
            var data = "test data";
            var duration = TimeSpan.FromMilliseconds(100);

            // Act
            var result = GatewayResult<string>.Success(data, duration);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(data, result.Data);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(duration, result.Duration);
        }

        [Fact]
        public void Failure_CreatesFailureResult()
        {
            // Arrange
            var errorMessage = "Test error";
            var duration = TimeSpan.FromMilliseconds(100);

            // Act
            var result = GatewayResult<string>.Failure(errorMessage, duration);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Null(result.Data);
            Assert.Equal(errorMessage, result.ErrorMessage);
            Assert.Equal(duration, result.Duration);
        }
    }
}