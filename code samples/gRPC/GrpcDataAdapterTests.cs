using Instrument.Data.Adapters;
using Instrument.Data.Adapters.Grpc;
using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Instrument.Data.UT.Adapters;

public class GrpcDataAdapterTests
{
    private readonly Mock<ISequenceService> _mockSequenceService;
    private readonly Mock<IParameterService> _mockParameterService;
    private readonly Mock<IResourceService> _mockResourceService;
    private readonly Mock<ISequenceGroupService> _mockSequenceGroupService;
    private readonly Mock<ISequenceGrpcClient> _mockSequenceGrpcClient;
    private readonly Mock<IParameterGrpcClient> _mockParameterGrpcClient;
    private readonly Mock<IResourceGrpcClient> _mockResourceGrpcClient;
    private readonly Mock<ISequenceGroupGrpcClient> _mockSequenceGroupGrpcClient;
    private readonly Mock<ILogger<GrpcDataAdapter>> _mockLogger;
    private readonly GrpcAdapterOptions _options;
    private readonly SchedulerDbContext _dbContext;
    
    public GrpcDataAdapterTests()
    {
        _mockSequenceService = new Mock<ISequenceService>();
        _mockParameterService = new Mock<IParameterService>();
        _mockResourceService = new Mock<IResourceService>();
        _mockSequenceGroupService = new Mock<ISequenceGroupService>();
        _mockSequenceGrpcClient = new Mock<ISequenceGrpcClient>();
        _mockParameterGrpcClient = new Mock<IParameterGrpcClient>();
        _mockResourceGrpcClient = new Mock<IResourceGrpcClient>();
        _mockSequenceGroupGrpcClient = new Mock<ISequenceGroupGrpcClient>();
        _mockLogger = new Mock<ILogger<GrpcDataAdapter>>();
        
        _options = new GrpcAdapterOptions
        {
            BaseAddress = "https://localhost:5001",
            TimeoutSeconds = 10,
            UseSecureConnection = true,
            ClearExistingDataBeforeImport = false,
            MaxBatchSize = 50
        };
        
        var optionsMock = new Mock<IOptions<GrpcAdapterOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);
        
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<SchedulerDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDB_" + Guid.NewGuid().ToString())
            .Options;
            
        _dbContext = new SchedulerDbContext(options);
    }
    
    [Fact]
    public async Task ImportSequencesAsync_ShouldCreateNewSequences()
    {
        // Arrange
        var sequences = new List<Sequence>
        {
            new Sequence { Id = 1, Name = "Sequence 1", Description = "Test Sequence 1" },
            new Sequence { Id = 2, Name = "Sequence 2", Description = "Test Sequence 2" }
        };
        
        _mockSequenceGrpcClient.Setup(c => c.GetAllSequencesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sequences);
            
        _mockSequenceService.Setup(s => s.GetSequenceByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Sequence?)null);
            
        var adapter = new GrpcDataAdapter(
            _dbContext,
            _mockLogger.Object,
            Mock.Of<IOptions<GrpcAdapterOptions>>(o => o.Value == _options),
            _mockSequenceService.Object,
            _mockParameterService.Object,
            _mockResourceService.Object,
            _mockSequenceGroupService.Object,
            _mockSequenceGrpcClient.Object,
            _mockParameterGrpcClient.Object,
            _mockResourceGrpcClient.Object,
            _mockSequenceGroupGrpcClient.Object);
            
        // Act
        await adapter.ImportSequencesAsync();
        
        // Assert
        _mockSequenceGrpcClient.Verify(c => c.GetAllSequencesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockSequenceService.Verify(s => s.CreateSequenceAsync(It.IsAny<Sequence>()), Times.Exactly(2));
        _mockSequenceService.Verify(s => s.UpdateSequenceAsync(It.IsAny<Sequence>()), Times.Never);
    }
    
    [Fact]
    public async Task ImportSequencesAsync_ShouldUpdateExistingSequences()
    {
        // Arrange
        var sequences = new List<Sequence>
        {
            new Sequence { Id = 1, Name = "Sequence 1", Description = "Test Sequence 1" },
            new Sequence { Id = 2, Name = "Sequence 2", Description = "Test Sequence 2" }
        };
        
        _mockSequenceGrpcClient.Setup(c => c.GetAllSequencesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sequences);
            
        _mockSequenceService.Setup(s => s.GetSequenceByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new Sequence());
            
        var adapter = new GrpcDataAdapter(
            _dbContext,
            _mockLogger.Object,
            Mock.Of<IOptions<GrpcAdapterOptions>>(o => o.Value == _options),
            _mockSequenceService.Object,
            _mockParameterService.Object,
            _mockResourceService.Object,
            _mockSequenceGroupService.Object,
            _mockSequenceGrpcClient.Object,
            _mockParameterGrpcClient.Object,
            _mockResourceGrpcClient.Object,
            _mockSequenceGroupGrpcClient.Object);
            
        // Act
        await adapter.ImportSequencesAsync();
        
        // Assert
        _mockSequenceGrpcClient.Verify(c => c.GetAllSequencesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockSequenceService.Verify(s => s.CreateSequenceAsync(It.IsAny<Sequence>()), Times.Never);
        _mockSequenceService.Verify(s => s.UpdateSequenceAsync(It.IsAny<Sequence>()), Times.Exactly(2));
    }
    
    [Fact]
    public async Task TestConnectionsAsync_ShouldReturnConnectionResults()
    {
        // Arrange
        _mockSequenceGrpcClient.Setup(c => c.TestConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockParameterGrpcClient.Setup(c => c.TestConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockResourceGrpcClient.Setup(c => c.TestConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockSequenceGroupGrpcClient.Setup(c => c.TestConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
            
        var adapter = new GrpcDataAdapter(
            _dbContext,
            _mockLogger.Object,
            Mock.Of<IOptions<GrpcAdapterOptions>>(o => o.Value == _options),
            _mockSequenceService.Object,
            _mockParameterService.Object,
            _mockResourceService.Object,
            _mockSequenceGroupService.Object,
            _mockSequenceGrpcClient.Object,
            _mockParameterGrpcClient.Object,
            _mockResourceGrpcClient.Object,
            _mockSequenceGroupGrpcClient.Object);
            
        // Act
        var results = await adapter.TestConnectionsAsync();
        
        // Assert
        Assert.Equal(4, results.Count);
        Assert.True(results["SequenceService"]);
        Assert.True(results["ParameterService"]);
        Assert.False(results["ResourceService"]);
        Assert.True(results["SequenceGroupService"]);
    }
    
    [Fact]
    public async Task ImportAllDataAsync_ShouldCallAllImportMethods()
    {
        // Arrange
        var adapter = new GrpcDataAdapterWrapper(
            _dbContext,
            _mockLogger.Object,
            Mock.Of<IOptions<GrpcAdapterOptions>>(o => o.Value == _options),
            _mockSequenceService.Object,
            _mockParameterService.Object,
            _mockResourceService.Object,
            _mockSequenceGroupService.Object,
            _mockSequenceGrpcClient.Object,
            _mockParameterGrpcClient.Object,
            _mockResourceGrpcClient.Object,
            _mockSequenceGroupGrpcClient.Object);
            
        // Act
        await adapter.ImportAllDataAsync();
        
        // Assert
        Assert.Equal(1, adapter.SequenceImportCallCount);
        Assert.Equal(1, adapter.ParameterImportCallCount);
        Assert.Equal(1, adapter.ResourceImportCallCount);
        Assert.Equal(1, adapter.SequenceGroupImportCallCount);
    }
    
    /// <summary>
    /// Test wrapper class for the GrpcDataAdapter to track method calls
    /// </summary>
    private class GrpcDataAdapterWrapper : GrpcDataAdapter
    {
        public int SequenceImportCallCount { get; private set; }
        public int ParameterImportCallCount { get; private set; }
        public int ResourceImportCallCount { get; private set; }
        public int SequenceGroupImportCallCount { get; private set; }
        
        public GrpcDataAdapterWrapper(
            SchedulerDbContext dbContext,
            ILogger<GrpcDataAdapter> logger,
            IOptions<GrpcAdapterOptions> options,
            ISequenceService sequenceService,
            IParameterService parameterService,
            IResourceService resourceService,
            ISequenceGroupService sequenceGroupService,
            ISequenceGrpcClient sequenceGrpcClient,
            IParameterGrpcClient parameterGrpcClient,
            IResourceGrpcClient resourceGrpcClient,
            ISequenceGroupGrpcClient sequenceGroupGrpcClient)
            : base(dbContext, logger, options, sequenceService, parameterService, resourceService, 
                  sequenceGroupService, sequenceGrpcClient, parameterGrpcClient, resourceGrpcClient, 
                  sequenceGroupGrpcClient)
        {
        }
        
        public override async Task ImportSequencesAsync(CancellationToken cancellationToken = default)
        {
            SequenceImportCallCount++;
            await Task.CompletedTask;
        }
        
        public override async Task ImportParametersAsync(CancellationToken cancellationToken = default)
        {
            ParameterImportCallCount++;
            await Task.CompletedTask;
        }
        
        public override async Task ImportResourcesAsync(CancellationToken cancellationToken = default)
        {
            ResourceImportCallCount++;
            await Task.CompletedTask;
        }
        
        public override async Task ImportSequenceGroupsAsync(CancellationToken cancellationToken = default)
        {
            SequenceGroupImportCallCount++;
            await Task.CompletedTask;
        }
    }
}