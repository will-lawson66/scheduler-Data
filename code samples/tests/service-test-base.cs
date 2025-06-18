using System;
using Instrument.Data.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;

namespace Instrument.Data.Tests.Services
{
    /// <summary>
    /// Base class for all service layer tests that provides common setup and verification methods.
    /// </summary>
    public abstract class ServiceTestBase<TService> where TService : class
    {
        protected MockRepositoryProvider Mocks { get; }
        protected TService Service { get; }
        
        protected ServiceTestBase()
        {
            // Setup mocks and create service
            Mocks = new MockRepositoryProvider();
            Service = Mocks.CreateService<TService>();
        }
        
        /// <summary>
        /// Verifies that an error was logged for a specific service
        /// </summary>
        protected void VerifyLogError<T>(Times times = null)
        {
            if (times == null)
            {
                times = Times.Once();
            }
            
            Mocks.GetMock<ILogger<T>>().Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                times);
        }
        
        /// <summary>
        /// Verifies that an information message was logged for a specific service
        /// </summary>
        protected void VerifyLogInformation<T>(Times times = null)
        {
            if (times == null)
            {
                times = Times.Once();
            }
            
            Mocks.GetMock<ILogger<T>>().Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                times);
        }
        
        /// <summary>
        /// Verifies that a warning message was logged for a specific service
        /// </summary>
        protected void VerifyLogWarning<T>(Times times = null)
        {
            if (times == null)
            {
                times = Times.Once();
            }
            
            Mocks.GetMock<ILogger<T>>().Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                times);
        }
        
        /// <summary>
        /// Configures the mock logger to capture log messages
        /// </summary>
        protected void SetupLogCapture<T>(out List<(LogLevel Level, string Message)> capturedLogs)
        {
            capturedLogs = new List<(LogLevel Level, string Message)>();
            
            Mocks.GetMock<ILogger<T>>()
                .Setup(x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)))
                .Callback<LogLevel, EventId, It.IsAnyType, Exception, Func<It.IsAnyType, Exception, string>>(
                    (level, id, state, ex, formatter) => 
                    {
                        var message = formatter(state, ex);
                        capturedLogs.Add((level, message));
                    });
        }
        
        /// <summary>
        /// Setup a repository method to throw an exception for testing error handling
        /// </summary>
        protected void SetupRepositoryException<T>(
            Expression<Func<T, Task>> methodExpression, 
            Exception exception) where T : class
        {
            Mocks.GetMock<T>()
                .Setup(methodExpression)
                .ThrowsAsync(exception);
        }
        
        /// <summary>
        /// Setup a repository method to throw an exception for testing error handling (non-async version)
        /// </summary>
        protected void SetupRepositoryException<T>(
            Expression<Func<T, Task>> methodExpression, 
            Exception exception) where T : class
        {
            Mocks.GetMock<T>()
                .Setup(methodExpression)
                .Throws(exception);
        }
    }
}
