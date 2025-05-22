using Instrument.Data.Adapters;
using Instrument.Data.Api;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Instrument.Data.UT.Api;

public class GrpcDataImportApiTests
{
    private readonly Mock<IGrpcDataAdapter> _mockGrpcAdapter;
    private readonly Mock<ILogger<GrpcDataImportApi>> _mockLogger;
    
    public GrpcDataImportApiTests()
    {
        _mockGrpcAdapter = new Mock<IGrpcDataAdapter>();
        _mockLogger = new Mock<ILogger<GrpcDataImportApi>>();
    }
    
    [Fact]
    public async Task ImportAllDataAsync_ShouldCallAdapterImportAllData()
    {
        // Arrange
        var api = new GrpcDataImportApi(_mockGrpcAdapter.Object, _mockLogger.Object);
        
        // Act
        await api.ImportAllDataAsync();
        
        // Assert
        _mockGrpcAdapter.Verify(a => a.ImportAllDataAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task ImportSequencesAsync_ShouldCallAdapterImportSequences()
    {
        // Arrange
        var api = new GrpcDataImportApi(_mockGrpcAdapter.Object, _mockLogger.Object);
        
        // Act
        await api.ImportSequencesAsync();
        
        // Assert
        _mockGrpcAdapter.Verify(a => a.ImportSequencesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task ImportParametersAsync_ShouldCallAdapterImportParameters()
    {
        // Arrange
        var api = new GrpcDataImportApi(_mockGrpcAdapter.Object, _mockLogger.Object);
        
        // Act
        await api.ImportParametersAsync();
        
        // Assert
        _mockGrpcAdapter.Verify(a => a.ImportParametersAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task ImportResourcesAsync_ShouldCallAdapterImportResources()
    {
        // Arrange
        var api = new GrpcDataImportApi(_mockGrpcAdapter.Object, _mockLogger.Object);
        
        // Act
        await api.ImportResourcesAsync();
        
        // Assert
        _mockGrpcAdapter.Verify(a => a.ImportResourcesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task ImportSequenceGroupsAsync_ShouldCallAdapterImportSequenceGroups()
    {
        // Arrange
        var api = new GrpcDataImportApi(_mockGrpcAdapter.Object, _mockLogger.Object);
        
        // Act
        await api.ImportSequenceGroupsAsync();
        
        // Assert
        _mockGrpcAdapter.Verify(a => a.ImportSequenceGroupsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task TestConnectionsAsync_ShouldCallAdapterTestConnections()
    {
        // Arrange
        var testResults = new Dictionary<string, bool>
        {
            { "SequenceService", true },
            { "ParameterService", false }
        };
        
        _mockGrpcAdapter.Setup(a => a.TestConnectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testResults);
            
        var api = new GrpcDataImportApi(_mockGrpcAdapter.Object, _mockLogger.Object);
        
        // Act
        var results = await api.TestConnectionsAsync();
        
        // Assert
        _mockGrpcAdapter.Verify(a => a.TestConnectionsAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(2, results.Count);
        Assert.True(results["SequenceService"]);
        Assert.False(results["ParameterService"]);
    }
    
    [Fact]
    public async Task ImportAllDataAsync_ShouldHandleAndRethrowExceptions()
    {
        // Arrange
        _mockGrpcAdapter.Setup(a => a.ImportAllDataAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));
            
        var api = new GrpcDataImportApi(_mockGrpcAdapter.Object, _mockLogger.Object);
        
        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => api.ImportAllDataAsync());
    }
}