using Instrument.Scheduling.Data.DataContext;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Exceptions;
using Instrument.Scheduling.Data.Repository;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace Instrument.Scheduling.Data.UT;
public class SequenceRepositoryTests
{
    private readonly Mock<SchedulerDbContext> _mockDbContext;
    private readonly Mock<DbSet<Sequence>> _mockSequenceDbSet;
    private readonly Mock<DbSet<SequenceParameter>> _mockSequenceParameterDbSet;
    private readonly SequenceRepository _repository;
    
    public SequenceRepositoryTests()
    {
        // Create mock DbSets
        _mockSequenceDbSet = MockDbSetSetup();
        _mockSequenceParameterDbSet = MockDbSetSetup<SequenceParameter>();
        
        // Create mock DbContext
        _mockDbContext = new Mock<SchedulerDbContext>(new DbContextOptionsBuilder<SchedulerDbContext>().Options);
        _mockDbContext.Setup(db => db.Sequences).Returns(_mockSequenceDbSet.Object);
        _mockDbContext.Setup(db => db.SequenceParameters).Returns(_mockSequenceParameterDbSet.Object);

        // Create repository
        _repository = new SequenceRepository(_mockDbContext.Object);
    }
    
    private Mock<DbSet<T>> MockDbSetSetup<T>() where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        var data = new List<T>().AsQueryable();
        
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        mockSet.Setup(m => m.Add(It.IsAny<T>())).Returns((EntityEntry<T>)null);
        
        return mockSet;
    }
    
    private Mock<DbSet<Sequence>> MockDbSetSetup()
    {
        var sequences = new List<Sequence>
        {
            new Sequence { Id = "seq1", Name = "Sequence 1", Description = "Sequence description", WorstCaseTime = TimeSpan.Zero},
            new Sequence { Id = "seq2", Name = "Sequence 2", Description = "Sequence 2 description", WorstCaseTime = TimeSpan.Zero }
        }.AsQueryable();
        
        var mockSet = new Mock<DbSet<Sequence>>();
        mockSet.As<IQueryable<Sequence>>().Setup(m => m.Provider).Returns(sequences.Provider);
        mockSet.As<IQueryable<Sequence>>().Setup(m => m.Expression).Returns(sequences.Expression);
        mockSet.As<IQueryable<Sequence>>().Setup(m => m.ElementType).Returns(sequences.ElementType);
        mockSet.As<IQueryable<Sequence>>().Setup(m => m.GetEnumerator()).Returns(sequences.GetEnumerator());
        
        mockSet.Setup(m => m.Find(It.IsAny<object[]>())).Returns<object[]>(ids => 
        {
            var id = ids[0].ToString();
            return sequences.FirstOrDefault(s => s.Id == id);
        });
        
        mockSet.Setup(m => m.Add(It.IsAny<Sequence>())).Returns((EntityEntry<Sequence>)null);
        
        return mockSet;
    }
    
    [Fact]
    public async Task GetAllAsync_CallsDbContext_AndReturnsResult()
    {
        // Arrange
        _mockSequenceDbSet.Setup(m => m.ToListAsync(default))
            .ReturnsAsync(new List<Sequence>
            {
                new Sequence { Id = "seq1", Name = "Sequence 1", Description = "Sequence description", WorstCaseTime = TimeSpan.Zero},
                new Sequence { Id = "seq2", Name = "Sequence 2", Description = "Sequence 2 description", WorstCaseTime = TimeSpan.Zero }
            });
        
        // Act
        var result = await _repository.GetAllAsync();
        
        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, s => s.Id == "seq1");
        Assert.Contains(result, s => s.Id == "seq2");
        _mockSequenceDbSet.Verify(m => m.ToListAsync(default), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_CallsDbContext_WithCorrectId()
    {
        // Arrange
        var sequence = new Sequence { Id = "test-id", Name = "Test Sequence", Description = "Test description", WorstCaseTime = TimeSpan.Zero };
        _mockSequenceDbSet.Setup(m => m.FindAsync(new object[] { "test-id" }))
            .ReturnsAsync(sequence);
        
        // Act
        var result = await _repository.GetByIdAsync("test-id");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-id", result.Id);
        Assert.Equal("Test Sequence", result.Name);
        _mockSequenceDbSet.Verify(m => m.FindAsync(new object[] { "test-id" }), Times.Once);
    }
    
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenSequenceNotFound()
    {
        // Arrange
        _mockSequenceDbSet.Setup(m => m.FindAsync(new object[] { "non-existent" }))
            .ReturnsAsync((Sequence)null);
        
        // Act
        var result = await _repository.GetByIdAsync("non-existent");
        
        // Assert
        Assert.Null(result);
        _mockSequenceDbSet.Verify(m => m.FindAsync(new object[] { "non-existent" }), Times.Once);
    }
    
    [Fact]
    public async Task GetQueryableAsync_ReturnsQueryable()
    {
        // Act
        var result = await _repository.GetQueryableAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IQueryable<Sequence>>(result);
    }
    
    [Fact]
    public async Task AddAsync_CallsDbContext_WithCorrectEntity()
    {
        // Arrange
        var sequence = new Sequence { Id = "new-id", Name = "New Sequence", Description = "New description", WorstCaseTime = TimeSpan.Zero };
        
        // Act
        await _repository.AddAsync(sequence);
        
        // Assert
        _mockSequenceDbSet.Verify(m => m.AddAsync(sequence, default), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAsync_CallsDbContext_WithCorrectEntity()
    {
        // Arrange
        var sequence = new Sequence { Id = "update-id", Name = "Updated Sequence", Description = "Updated description", WorstCaseTime = TimeSpan.Zero };
        
        // Act
        await _repository.UpdateAsync(sequence);
        
        // Assert
        _mockSequenceDbSet.Verify(m => m.Update(sequence), Times.Once);
    }
    
    [Fact]
    public async Task DeleteAsync_CallsDbContext_WithCorrectId()
    {
        // Arrange
        var sequence = new Sequence { Id = "delete-id", Name = "Delete Sequence", Description = "Delete description", WorstCaseTime = TimeSpan.Zero };
        
        _mockSequenceDbSet.Setup(m => m.FindAsync(new object[] { "delete-id" }))
            .ReturnsAsync(sequence);
        
        // Act
        await _repository.DeleteAsync("delete-id");
        
        // Assert
        _mockSequenceDbSet.Verify(m => m.Remove(sequence), Times.Once);
    }
    
    [Fact]
    public async Task DeleteAsync_ThrowsException_WhenEntityNotFound()
    {
        // Arrange
        _mockSequenceDbSet.Setup(m => m.FindAsync(new object[] { "non-existent" }))
            .ReturnsAsync((Sequence)null);
        
        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _repository.DeleteAsync("non-existent"));
    }
    
    [Fact]
    public async Task SaveChangesAsync_CallsDbContext_SaveChanges()
    {
        // Act
        await _repository.SaveChangesAsync();
        
        // Assert
        _mockDbContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
    }
    
    [Fact]
    public async Task GetSequenceWithParametersAsync_ReturnsSequenceWithParameters()
    {
        // Arrange
        var sequences = new List<Sequence>
        {
            new Sequence 
            { 
                Id = "seq1", 
                Name = "Sequence 1", 
                Description = "Sequence description", 
                WorstCaseTime = TimeSpan.Zero,
                SequenceParameters = new List<SequenceParameter>
                {
                    new SequenceParameter { SequenceId = "seq1", ParameterId = "param1", OrderNumber = 1 }
                }
            }
        }.AsQueryable();
        
        var mockDbSet = new Mock<DbSet<Sequence>>();
        mockDbSet.As<IQueryable<Sequence>>().Setup(m => m.Provider).Returns(sequences.Provider);
        mockDbSet.As<IQueryable<Sequence>>().Setup(m => m.Expression).Returns(sequences.Expression);
        mockDbSet.As<IQueryable<Sequence>>().Setup(m => m.ElementType).Returns(sequences.ElementType);
        mockDbSet.As<IQueryable<Sequence>>().Setup(m => m.GetEnumerator()).Returns(sequences.GetEnumerator());
        
        _mockDbContext.Setup(db => db.Sequences).Returns(mockDbSet.Object);
        
        // Act
        var result = await _repository.GetSequenceWithParametersAsync("seq1");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("seq1", result.Id);
        Assert.NotNull(result.SequenceParameters);
        Assert.Single(result.SequenceParameters);
    }
    
    [Fact]
    public async Task GetSequencesByNameAsync_ReturnsMatchingSequences()
    {
        // Arrange
        var sequences = new List<Sequence>
        {
            new Sequence { Id = "seq1", Name = "Alpha Sequence", Description = "Alpha description", WorstCaseTime = TimeSpan.Zero },
            new Sequence { Id = "seq2", Name = "Beta Sequence", Description = "Beta description", WorstCaseTime = TimeSpan.Zero },
            new Sequence { Id = "seq3", Name = "Gamma Sequence", Description = "Gamma description", WorstCaseTime = TimeSpan.Zero }
        }.AsQueryable();
        
        var mockDbSet = new Mock<DbSet<Sequence>>();
        mockDbSet.As<IQueryable<Sequence>>().Setup(m => m.Provider).Returns(sequences.Provider);
        mockDbSet.As<IQueryable<Sequence>>().Setup(m => m.Expression).Returns(sequences.Expression);
        mockDbSet.As<IQueryable<Sequence>>().Setup(m => m.ElementType).Returns(sequences.ElementType);
        mockDbSet.As<IQueryable<Sequence>>().Setup(m => m.GetEnumerator()).Returns(sequences.GetEnumerator());
        
        _mockDbContext.Setup(db => db.Sequences).Returns(mockDbSet.Object);
        
        // Act
        var result = await _repository.GetSequencesByNameAsync("Beta");
        
        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Beta Sequence", result.First().Name);
    }
    
    [Fact]
    public async Task RemoveParameterFromSequenceAsync_RemovesParameter()
    {
        // Arrange
        var parameterId = "param1";
        var sequenceId = "seq1";
        var sequenceParameter = new SequenceParameter { SequenceId = sequenceId, ParameterId = parameterId, OrderNumber = 1 };
        
        var mockQuery = new List<SequenceParameter> { sequenceParameter }.AsQueryable();
        _mockSequenceParameterDbSet.As<IQueryable<SequenceParameter>>().Setup(m => m.Provider).Returns(mockQuery.Provider);
        _mockSequenceParameterDbSet.As<IQueryable<SequenceParameter>>().Setup(m => m.Expression).Returns(mockQuery.Expression);
        _mockSequenceParameterDbSet.As<IQueryable<SequenceParameter>>().Setup(m => m.ElementType).Returns(mockQuery.ElementType);
        _mockSequenceParameterDbSet.As<IQueryable<SequenceParameter>>().Setup(m => m.GetEnumerator()).Returns(mockQuery.GetEnumerator());
        
        // Act
        await _repository.RemoveParameterFromSequenceAsync(parameterId, sequenceId);
        
        // Assert
        _mockSequenceParameterDbSet.Verify(m => m.Remove(It.IsAny<SequenceParameter>()), Times.Once);
        _mockDbContext.Verify(m => m.SaveChangesAsync(default), Times.Once);
    }
    
    [Fact]
    public async Task GetSequencesByIdsAsync_ReturnsMatchingSequences()
    {
        // Arrange
        var sequences = new List<Sequence>
        {
            new Sequence { Id = "seq1", Name = "Sequence 1", Description = "Description 1", WorstCaseTime = TimeSpan.Zero },
            new Sequence { Id = "seq2", Name = "Sequence 2", Description = "Description 2", WorstCaseTime = TimeSpan.Zero },
            new Sequence { Id = "seq3", Name = "Sequence 3", Description = "Description 3", WorstCaseTime = TimeSpan.Zero }
        }.AsQueryable();
        
        var mockDbSet = new Mock<DbSet<Sequence>>();
        mockDbSet.As<IQueryable<Sequence>>().Setup(m => m.Provider).Returns(sequences.Provider);
        mockDbSet.As<IQueryable<Sequence>>().Setup(m => m.Expression).Returns(sequences.Expression);
        mockDbSet.As<IQueryable<Sequence>>().Setup(m => m.ElementType).Returns(sequences.ElementType);
        mockDbSet.As<IQueryable<Sequence>>().Setup(m => m.GetEnumerator()).Returns(sequences.GetEnumerator());
        
        _mockDbContext.Setup(db => db.Sequences).Returns(mockDbSet.Object);
        
        // Act
        var result = await _repository.GetSequencesByIdsAsync(new[] { "seq1", "seq3" });
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, s => s.Id == "seq1");
        Assert.Contains(result, s => s.Id == "seq3");
        Assert.DoesNotContain(result, s => s.Id == "seq2");
    }
}
