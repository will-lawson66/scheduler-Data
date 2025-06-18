using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Interfaces;
using Instrument.Data.Repository;
using Instrument.Data.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Instrument.Data.Tests.Repository
{
    /// <summary>
    /// Tests for the generic Repository<T> functionality. 
    /// We test this once with Sequence as a representative entity type to avoid
    /// repeating these tests for each repository.
    /// </summary>
    public class BaseRepositoryTests
    {
        private readonly List<Sequence> _sequences;
        private readonly Mock<SchedulerDbContext> _mockContext;
        private readonly Mock<DbSet<Sequence>> _mockSet;
        private readonly Repository<Sequence> _repository;
        
        public BaseRepositoryTests()
        {
            // Setup test data
            _sequences = new List<Sequence>
            {
                new Sequence 
                { 
                    Id = "1", 
                    Name = "Sequence 1", 
                    Description = "Test Sequence 1",
                    WorstCaseTime = TimeSpan.FromMinutes(5),
                    CanBeParallel = true
                },
                new Sequence 
                { 
                    Id = "2", 
                    Name = "Sequence 2", 
                    Description = "Test Sequence 2",
                    WorstCaseTime = TimeSpan.FromMinutes(10),
                    CanBeParallel = false
                }
            };
            
            // Setup mock DbContext and DbSet
            _mockSet = MockDbSetHelper.CreateMockDbSet(_sequences);
            _mockContext = MockDbSetHelper.CreateMockDbContext();
            _mockContext.Setup(c => c.Set<Sequence>()).Returns(_mockSet.Object);
            
            // Create repository instance
            _repository = new Repository<Sequence>(_mockContext.Object);
        }
        
        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            // Act
            var result = await _repository.GetAllAsync();
            
            // Assert
            var sequences = result.ToList();
            Assert.Equal(2, sequences.Count);
            Assert.Contains(sequences, s => s.Id == "1");
            Assert.Contains(sequences, s => s.Id == "2");
        }
        
        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsEntity()
        {
            // Act
            var result = await _repository.GetByIdAsync("1");
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Sequence 1", result.Name);
            Assert.Equal(TimeSpan.FromMinutes(5), result.WorstCaseTime);
        }
        
        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByIdAsync("999");
            
            // Assert
            Assert.Null(result);
        }
        
        [Fact]
        public async Task GetQueryableAsync_ReturnsQueryableForEntities()
        {
            // Act
            var queryable = await _repository.GetQueryableAsync();
            var result = queryable.Where(s => s.CanBeParallel).ToList();
            
            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].Id);
        }
        
        [Fact]
        public async Task AddAsync_AddsNewEntityAndReturnsIt()
        {
            // Arrange
            var newSequence = new Sequence
            {
                Id = "3",
                Name = "New Sequence",
                Description = "Test new sequence",
                WorstCaseTime = TimeSpan.FromMinutes(15),
                CanBeParallel = true
            };
            
            // Act
            var result = await _repository.AddAsync(newSequence);
            
            // Assert
            Assert.Equal("3", result.Id);
            Assert.Equal("New Sequence", result.Name);
            
            // Verify DbContext interactions
            _mockSet.Verify(m => m.AddAsync(It.IsAny<Sequence>(), It.IsAny<System.Threading.CancellationToken>()), Times.Once);
            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task UpdateAsync_UpdatesExistingEntity()
        {
            // Arrange
            var updatedSequence = new Sequence
            {
                Id = "1",
                Name = "Updated Sequence",
                Description = "Updated description",
                WorstCaseTime = TimeSpan.FromMinutes(7),
                CanBeParallel = false
            };
            
            // Act
            await _repository.UpdateAsync(updatedSequence);
            
            // Assert
            // Verify DbContext was called to save changes
            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
            
            // Get the updated entity to verify changes
            var updated = _sequences.FirstOrDefault(s => s.Id == "1");
            Assert.NotNull(updated);
            Assert.Equal("Updated Sequence", updated.Name);
            Assert.Equal("Updated description", updated.Description);
            Assert.Equal(TimeSpan.FromMinutes(7), updated.WorstCaseTime);
            Assert.False(updated.CanBeParallel);
        }
        
        [Fact]
        public async Task DeleteAsync_RemovesEntity()
        {
            // Act
            await _repository.DeleteAsync("1");
            
            // Assert
            // Verify entity was removed
            Assert.Single(_sequences);
            Assert.DoesNotContain(_sequences, s => s.Id == "1");
            
            // Verify context interactions
            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task SaveChangesAsync_CallsContextSaveChanges()
        {
            // Act
            await _repository.SaveChangesAsync();
            
            // Assert
            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }
    }
}
