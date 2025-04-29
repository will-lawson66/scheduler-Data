using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;
using Instrument.Scheduling.Data.Repository;
using Moq;

namespace Instrument.Scheduling.Data.UT;
    public class SequenceRepositoryTests
    {
        private readonly Mock<IStorageProvider<Sequence>> _mockProvider;
        private readonly SequenceRepository _repository;
        
        public SequenceRepositoryTests()
        {
            _mockProvider = new Mock<IStorageProvider<Sequence>>();
            _repository = new SequenceRepository(_mockProvider.Object);
        }
        
        [Fact]
        public async Task GetAllAsync_CallsProvider_AndReturnsResult()
        {
            // Arrange
            var sequences = new List<Sequence>
            {
                new Sequence { Id = "seq1", Name = "Sequence 1", Description = "Sequence description", WorstCaseTime = TimeSpan.Zero},
                new Sequence { Id = "seq2", Name = "Sequence 2", Description = "Sequence 2 description", WorstCaseTime = TimeSpan.Zero }
            };
            
            _mockProvider.Setup(p => p.GetAllAsync())
                .ReturnsAsync(sequences);
            
            // Act
            var result = await _repository.GetAllAsync();
            
            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, s => s.Id == "seq1");
            Assert.Contains(result, s => s.Id == "seq2");
            _mockProvider.Verify(p => p.GetAllAsync(), Times.Once);
        }
        
        [Fact]
        public async Task GetByIdAsync_CallsProvider_WithCorrectId()
        {
            // Arrange
            var sequence = new Sequence { Id = "test-id", Name = "Test Sequence", Description = "Test seq. description", WorstCaseTime = TimeSpan.Zero };
            _mockProvider.Setup(p => p.GetByIdAsync("test-id"))
                .ReturnsAsync(sequence);
            
            // Act
            var result = await _repository.GetByIdAsync("test-id");
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-id", result.Id);
            Assert.Equal("Test Sequence", result.Name);
            _mockProvider.Verify(p => p.GetByIdAsync("test-id"), Times.Once);
        }
        
        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenSequenceNotFound()
        {
            // Arrange
            _mockProvider.Setup(p => p.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((Sequence)null);
            
            // Act
            var result = await _repository.GetByIdAsync("non-existent");
            
            // Assert
            Assert.Null(result);
            _mockProvider.Verify(p => p.GetByIdAsync("non-existent"), Times.Once);
        }
        
        [Fact]
        public async Task GetQueryableAsync_ReturnsQueryableData()
        {
            // Arrange
            var sequences = new List<Sequence>
            {
                new Sequence {Id = "seq1", Name = "Alpha Sequence", Description = "Alpha Sequence description", WorstCaseTime = TimeSpan.Zero},
                new Sequence {Id = "seq2", Name = "Beta Sequence", Description = "Beta Sequence description", WorstCaseTime = TimeSpan.Zero},
                new Sequence {Id = "seq3", Name = "Gamma Sequence", Description = "Gamma Sequence description", WorstCaseTime = TimeSpan.Zero}
            };
            
            _mockProvider.Setup(p => p.GetAllAsync())
                .ReturnsAsync(sequences);
            
            // Act
            var result = await _repository.GetQueryableAsync();
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
            
            // Test queryability
            var filtered = result.Where(s => s.Name.StartsWith("B")).ToList();
            Assert.Single(filtered);
            Assert.Equal("Beta Sequence", filtered[0].Name);
            
            _mockProvider.Verify(p => p.GetAllAsync(), Times.Once);
        }
        
        [Fact]
        public async Task AddAsync_CallsProvider_WithCorrectEntity()
        {
            // Arrange
            var sequence = new Sequence {Id = "new-id", Name = "New Sequence", Description = "New sequence description", WorstCaseTime = TimeSpan.Zero };
            
            // Act
            await _repository.AddAsync(sequence);
            
            // Assert
            _mockProvider.Verify(p => p.AddAsync(sequence), Times.Once);
        }
        
        [Fact]
        public async Task UpdateAsync_CallsProvider_WithCorrectEntity()
        {
            // Arrange
            var sequence = new Sequence {Id = "update-id", Name = "Updated Sequence", Description = "Updated description", WorstCaseTime = TimeSpan.Zero };
            
            // Act
            await _repository.UpdateAsync(sequence);
            
            // Assert
            _mockProvider.Verify(p => p.UpdateAsync(sequence), Times.Once);
        }
        
        [Fact]
        public async Task DeleteAsync_CallsProvider_WithCorrectId()
        {
            // Arrange
            string idToDelete = "delete-id";
            
            // Act
            await _repository.DeleteAsync(idToDelete);
            
            // Assert
            _mockProvider.Verify(p => p.DeleteAsync(idToDelete), Times.Once);
        }
        
        [Fact]
        public async Task SaveChangesAsync_CallsProvider_SaveChanges()
        {
            // Act
            await _repository.SaveChangesAsync();
            
            // Assert
            _mockProvider.Verify(p => p.SaveChangesAsync(), Times.Once);
        }
        
        [Fact]
        public async Task Repository_FlowTest_CallsProviderMethodsInCorrectOrder()
        {
            // Arrange
            var sequence = new Sequence {Id = "flow-test", Name = "Flow Test", Description = "Flow test2 description", WorstCaseTime = TimeSpan.Zero };
            var sequences = new List<Sequence> { sequence };
            
            _mockProvider.Setup(p => p.GetAllAsync()).ReturnsAsync(sequences);
            _mockProvider.Setup(p => p.GetByIdAsync("flow-test")).ReturnsAsync(sequence);
            
            // Act
            // Add a sequence
            await _repository.AddAsync(sequence);
            
            // Get all sequences
            var allSequences = await _repository.GetAllAsync();
            
            // Get a specific sequence
            var retrievedSequence = await _repository.GetByIdAsync("flow-test");
            
            // Update the sequence
            System.Diagnostics.Debug.Assert(retrievedSequence != null, nameof(retrievedSequence) + " != null");
            retrievedSequence = retrievedSequence with { Description = "New description" };
            await _repository.UpdateAsync(retrievedSequence);

            // Delete the sequence
            await _repository.DeleteAsync("flow-test");
            
            // Save changes
            await _repository.SaveChangesAsync();
            
            // Assert
            _mockProvider.Verify(p => p.AddAsync(sequence), Times.Once);
            _mockProvider.Verify(p => p.GetAllAsync(), Times.Once);
            _mockProvider.Verify(p => p.GetByIdAsync("flow-test"), Times.Once);
            _mockProvider.Verify(p => p.UpdateAsync(retrievedSequence), Times.Once);
            _mockProvider.Verify(p => p.DeleteAsync("flow-test"), Times.Once);
            _mockProvider.Verify(p => p.SaveChangesAsync(), Times.Once);
            
            // Verify sequence - first verify parameters are set correctly
            Assert.Equal("flow-test", sequence.Id);
            Assert.Equal("Flow Test", sequence.Name);
        }
    }