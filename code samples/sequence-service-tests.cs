using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Instrument.Data.Entities;
using Instrument.Data.Exceptions;
using Instrument.Data.Interfaces;
using Instrument.Data.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Instrument.Data.Tests.Services
{
    /// <summary>
    /// Unit tests for the SequenceService focused on testing business logic
    /// and error handling, not the underlying repository
    /// </summary>
    public class SequenceServiceTests : ServiceTestBase<SequenceService>
    {
        private readonly List<Sequence> _testSequences;
        private readonly List<Parameter> _testParameters;
        
        public SequenceServiceTests() : base()
        {
            // Setup test data
            _testSequences = new List<Sequence>
            {
                new Sequence 
                { 
                    Id = "1", 
                    Name = "Test Sequence 1", 
                    Description = "First test sequence",
                    WorstCaseTime = TimeSpan.FromMinutes(5),
                    CanBeParallel = true,
                    SequenceParameters = new List<SequenceParameter>()
                },
                new Sequence 
                { 
                    Id = "2", 
                    Name = "Test Sequence 2", 
                    Description = "Second test sequence",
                    WorstCaseTime = TimeSpan.FromMinutes(10),
                    CanBeParallel = false,
                    SequenceParameters = new List<SequenceParameter>()
                }
            };
            
            _testParameters = new List<Parameter>
            {
                new Parameter
                {
                    Id = "p1",
                    Name = "Temperature",
                    Type = Entities.Enums.ParameterType.Number,
                    DefaultValue = "25",
                    SequenceParameters = new List<SequenceParameter>()
                },
                new Parameter
                {
                    Id = "p2",
                    Name = "Duration",
                    Type = Entities.Enums.ParameterType.Number,
                    DefaultValue = "60",
                    SequenceParameters = new List<SequenceParameter>()
                }
            };
            
            // Setup sequence-parameter relationships
            var relationship = new SequenceParameter
            {
                SequenceId = "1",
                ParameterId = "p1",
                Sequence = _testSequences[0],
                Parameter = _testParameters[0]
            };
            
            _testSequences[0].SequenceParameters.Add(relationship);
            _testParameters[0].SequenceParameters.Add(relationship);
            
            // Configure mocks with test data
            Mocks.SetupSequences(_testSequences);
            Mocks.SetupParameters(_testParameters);
        }
        
        [Fact]
        public async Task GetAllSequencesAsync_ReturnsAllSequences()
        {
            // Act
            var result = await Service.GetAllSequencesAsync();
            
            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, s => s.Id == "1");
            Assert.Contains(result, s => s.Id == "2");
            
            // Verify repository was called
            Mocks.GetMock<ISequenceRepository>().Verify(r => r.GetAllAsync(), Times.Once);
        }
        
        [Fact]
        public async Task GetSequenceByIdAsync_WithValidId_ReturnsSequence()
        {
            // Act
            var result = await Service.GetSequenceByIdAsync("1");
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Sequence 1", result.Name);
            
            // Verify repository was called
            Mocks.GetMock<ISequenceRepository>().Verify(r => r.GetByIdAsync("1"), Times.Once);
        }
        
        [Fact]
        public async Task GetSequenceByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await Service.GetSequenceByIdAsync("999");
            
            // Assert
            Assert.Null(result);
            
            // Verify repository was called
            Mocks.GetMock<ISequenceRepository>().Verify(r => r.GetByIdAsync("999"), Times.Once);
        }
        
        [Fact]
        public async Task CreateSequenceAsync_CreatesNewSequence()
        {
            // Arrange
            var newSequence = new Sequence
            {
                Id = "3",
                Name = "New Sequence",
                Description = "A new test sequence",
                WorstCaseTime = TimeSpan.FromMinutes(15),
                CanBeParallel = true
            };
            
            // Act
            var result = await Service.CreateSequenceAsync(newSequence);
            
            // Assert
            Assert.Equal("3", result.Id);
            Assert.Equal("New Sequence", result.Name);
            
            // Verify repository was called
            Mocks.GetMock<ISequenceRepository>().Verify(r => r.AddAsync(It.IsAny<Sequence>()), Times.Once);
        }
        
        [Fact]
        public async Task CreateSequenceAsync_WithInvalidData_ThrowsValidationException()
        {
            // Arrange
            var invalidSequence = new Sequence
            {
                Id = "3",
                Name = "", // Invalid - empty name
                WorstCaseTime = TimeSpan.FromMinutes(15),
                CanBeParallel = true
            };
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => Service.CreateSequenceAsync(invalidSequence));
                
            Assert.Contains("name", exception.Message.ToLower());
            
            // Verify repository was not called
            Mocks.GetMock<ISequenceRepository>().Verify(r => r.AddAsync(It.IsAny<Sequence>()), Times.Never);
        }
        
        [Fact]
        public async Task UpdateSequenceAsync_UpdatesExistingSequence()
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
            await Service.UpdateSequenceAsync(updatedSequence);
            
            // Assert - Verify repository was called
            Mocks.GetMock<ISequenceRepository>().Verify(r => r.UpdateAsync(It.IsAny<Sequence>()), Times.Once);
        }
        
        [Fact]
        public async Task UpdateSequenceAsync_WithNonExistentId_ThrowsNotFoundException()
        {
            // Arrange
            // Configure GetByIdAsync to return null for ID "999"
            Mocks.GetMock<ISequenceRepository>()
                .Setup(r => r.GetByIdAsync("999"))
                .ReturnsAsync((Sequence)null);
                
            var nonExistentSequence = new Sequence
            {
                Id = "999",
                Name = "Non-existent",
                WorstCaseTime = TimeSpan.FromMinutes(5)
            };
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(
                () => Service.UpdateSequenceAsync(nonExistentSequence));
                
            Assert.Contains("999", exception.Message);
            
            // Verify repository methods were called correctly
            Mocks.GetMock<ISequenceRepository>().Verify(r => r.GetByIdAsync("999"), Times.Once);
            Mocks.GetMock<ISequenceRepository>().Verify(r => r.UpdateAsync(It.IsAny<Sequence>()), Times.Never);
        }
        
        [Fact]
        public async Task DeleteSequenceAsync_WithValidId_DeletesSequence()
        {
            // Act
            await Service.DeleteSequenceAsync("1");
            
            // Assert - Verify repository was called
            Mocks.GetMock<ISequenceRepository>().Verify(r => r.DeleteAsync("1"), Times.Once);
        }
        
        [Fact]
        public async Task GetSequenceParametersAsync_ReturnsParametersForSequence()
        {
            // Act
            var result = await Service.GetSequenceParametersAsync("1");
            
            // Assert
            Assert.Single(result);
            Assert.Equal("p1", result.First().Id);
            Assert.Equal("Temperature", result.First().Name);
            
            // Verify repository was called
            Mocks.GetMock<IParameterRepository>().Verify(r => r.GetBySequenceIdAsync("1"), Times.Once);
        }
        
        [Fact]
        public async Task AddParameterToSequenceAsync_AddsParameterToSequence()
        {
            // Act
            await Service.AddParameterToSequenceAsync("2", "p2");
            
            // Assert - Verify repositories were called
            Mocks.GetMock<ISequenceRepository>().Verify(r => r.GetByIdAsync("2"), Times.Once);
            Mocks.GetMock<IParameterRepository>().Verify(r => r.GetByIdAsync("p2"), Times.Once);
            Mocks.GetMock<ISequenceRepository>().Verify(
                r => r.AddParameterToSequenceAsync("2", "p2"), Times.Once);
        }
        
        [Fact]
        public async Task AddParameterToSequenceAsync_WithNonExistentSequence_ThrowsNotFoundException()
        {
            // Arrange
            // Configure GetByIdAsync to return null for sequence ID "999"
            Mocks.GetMock<ISequenceRepository>()
                .Setup(r => r.GetByIdAsync("999"))
                .ReturnsAsync((Sequence)null);
                
            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(
                () => Service.AddParameterToSequenceAsync("999", "p1"));
                
            Assert.Contains("Sequence", exception.Message);
            Assert.Contains("999", exception.Message);
            
            // Verify repository interactions
            Mocks.GetMock<ISequenceRepository>().Verify(r => r.GetByIdAsync("999"), Times.Once);
            Mocks.GetMock<IParameterRepository>().Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
            Mocks.GetMock<ISequenceRepository>().Verify(
                r => r.AddParameterToSequenceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        
        [Fact]
        public async Task AddParameterToSequenceAsync_WithNonExistentParameter_ThrowsNotFoundException()
        {
            // Arrange
            // Configure GetByIdAsync to return null for parameter ID "p999"
            Mocks.GetMock<IParameterRepository>()
                .Setup(r => r.GetByIdAsync("p999"))
                .ReturnsAsync((Parameter)null);
                
            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(
                () => Service.AddParameterToSequenceAsync("1", "p999"));
                
            Assert.Contains("Parameter", exception.Message);
            Assert.Contains("p999", exception.Message);
            
            // Verify repository interactions
            Mocks.GetMock<ISequenceRepository>().Verify(r => r.GetByIdAsync("1"), Times.Once);
            Mocks.GetMock<IParameterRepository>().Verify(r => r.GetByIdAsync("p999"), Times.Once);
            Mocks.GetMock<ISequenceRepository>().Verify(
                r => r.AddParameterToSequenceAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        
        [Fact]
        public async Task RemoveParameterFromSequenceAsync_RemovesParameterFromSequence()
        {
            // Act
            await Service.RemoveParameterFromSequenceAsync("1", "p1");
            
            // Assert - Verify repository was called
            Mocks.GetMock<ISequenceRepository>().Verify(
                r => r.RemoveParameterFromSequenceAsync("1", "p1"), Times.Once);
        }
        
        [Fact]
        public async Task GetSequenceByIdAsync_DatabaseError_ThrowsStorageProviderException()
        {
            // Arrange
            Mocks.GetMock<ISequenceRepository>()
                .Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ThrowsAsync(new DbUpdateException("Database error", new Exception("Inner exception")));
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<StorageProviderException>(
                () => Service.GetSequenceByIdAsync("1"));
                
            Assert.Contains("Failed to retrieve sequence", exception.Message);
            Assert.IsType<DbUpdateException>(exception.InnerException);
            
            // Verify error was logged
            VerifyLogError<SequenceService>();
        }
    }
}
