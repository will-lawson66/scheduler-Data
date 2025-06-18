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
    /// Tests for the SequenceRepository specialized methods.
    /// Note that we don't re-test the base Repository<T> methods
    /// because they're already covered in BaseRepositoryTests.
    /// </summary>
    public class SequenceRepositoryTests
    {
        private readonly List<Sequence> _sequences;
        private readonly List<SequenceParameter> _sequenceParameters;
        private readonly List<Parameter> _parameters;
        private readonly Mock<SchedulerDbContext> _mockContext;
        private readonly Mock<DbSet<Sequence>> _mockSequenceSet;
        private readonly Mock<DbSet<SequenceParameter>> _mockSequenceParameterSet;
        private readonly Mock<DbSet<Parameter>> _mockParameterSet;
        private readonly SequenceRepository _repository;
        
        public SequenceRepositoryTests()
        {
            // Setup test data for sequences
            _sequences = new List<Sequence>
            {
                new Sequence 
                { 
                    Id = "1", 
                    Name = "First Test Sequence", 
                    Description = "First test sequence description",
                    WorstCaseTime = TimeSpan.FromMinutes(5),
                    CanBeParallel = true,
                    SequenceParameters = new List<SequenceParameter>()
                },
                new Sequence 
                { 
                    Id = "2", 
                    Name = "Second Test Sequence", 
                    Description = "Second test sequence description",
                    WorstCaseTime = TimeSpan.FromMinutes(10),
                    CanBeParallel = false,
                    SequenceParameters = new List<SequenceParameter>()
                },
                new Sequence 
                { 
                    Id = "3", 
                    Name = "Temperature Analysis", 
                    Description = "A sequence for temperature analysis",
                    WorstCaseTime = TimeSpan.FromMinutes(15),
                    CanBeParallel = true,
                    SequenceParameters = new List<SequenceParameter>()
                }
            };
            
            // Setup test data for parameters
            _parameters = new List<Parameter>
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
            
            // Setup test data for sequence parameters (relationship table)
            _sequenceParameters = new List<SequenceParameter>
            {
                new SequenceParameter
                {
                    SequenceId = "1",
                    ParameterId = "p1",
                    Sequence = _sequences[0],
                    Parameter = _parameters[0]
                },
                new SequenceParameter
                {
                    SequenceId = "3",
                    ParameterId = "p1",
                    Sequence = _sequences[2],
                    Parameter = _parameters[0]
                }
            };
            
            // Update the navigation properties
            _sequences[0].SequenceParameters.Add(_sequenceParameters[0]);
            _sequences[2].SequenceParameters.Add(_sequenceParameters[1]);
            _parameters[0].SequenceParameters.Add(_sequenceParameters[0]);
            _parameters[0].SequenceParameters.Add(_sequenceParameters[1]);
            
            // Setup mocks
            _mockSequenceSet = MockDbSetHelper.CreateMockDbSet(_sequences);
            _mockParameterSet = MockDbSetHelper.CreateMockDbSet(_parameters);
            _mockSequenceParameterSet = MockDbSetHelper.CreateMockDbSet(_sequenceParameters);
            
            _mockContext = MockDbSetHelper.CreateMockDbContext();
            _mockContext.Setup(c => c.Sequences).Returns(_mockSequenceSet.Object);
            _mockContext.Setup(c => c.Parameters).Returns(_mockParameterSet.Object);
            _mockContext.Setup(c => c.SequenceParameters).Returns(_mockSequenceParameterSet.Object);
            
            // Create repository
            _repository = new SequenceRepository(_mockContext.Object);
        }
        
        [Fact]
        public async Task GetByNameAsync_ReturnsMatchingSequences()
        {
            // Act
            var result = await _repository.GetByNameAsync("Test");
            
            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, s => s.Id == "1");
            Assert.Contains(result, s => s.Id == "2");
        }
        
        [Fact]
        public async Task GetByNameAsync_WithSpecificName_ReturnsMatchingSequence()
        {
            // Act
            var result = await _repository.GetByNameAsync("Temperature");
            
            // Assert
            Assert.Single(result);
            Assert.Equal("3", result.First().Id);
        }
        
        [Fact]
        public async Task GetByParameterIdAsync_ReturnsSequencesWithParameter()
        {
            // Act
            var result = await _repository.GetByParameterIdAsync("p1");
            
            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, s => s.Id == "1");
            Assert.Contains(result, s => s.Id == "3");
        }
        
        [Fact]
        public async Task GetByParameterIdAsync_WithNoMatches_ReturnsEmptyCollection()
        {
            // Act
            var result = await _repository.GetByParameterIdAsync("non-existent");
            
            // Assert
            Assert.Empty(result);
        }
        
        [Fact]
        public async Task GetWithParametersAsync_IncludesParameterData()
        {
            // Act - This is a custom method that includes the Parameters navigation property
            var result = await _repository.GetWithParametersAsync("1");
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("1", result.Id);
            Assert.NotEmpty(result.SequenceParameters);
            Assert.Equal("p1", result.SequenceParameters.First().ParameterId);
            Assert.NotNull(result.SequenceParameters.First().Parameter);
            Assert.Equal("Temperature", result.SequenceParameters.First().Parameter.Name);
        }
        
        [Fact]
        public async Task AddParameterToSequenceAsync_CreatesRelationship()
        {
            // Arrange - Setup for a new relationship
            var sequenceId = "2"; // Second Test Sequence
            var parameterId = "p2"; // Duration parameter
            
            // Act
            await _repository.AddParameterToSequenceAsync(sequenceId, parameterId);
            
            // Assert
            Assert.Equal(3, _sequenceParameters.Count);
            var newRelation = _sequenceParameters.FirstOrDefault(sp => 
                sp.SequenceId == sequenceId && sp.ParameterId == parameterId);
            
            Assert.NotNull(newRelation);
            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task RemoveParameterFromSequenceAsync_RemovesRelationship()
        {
            // Arrange - We'll remove an existing relationship
            var sequenceId = "1"; // First Test Sequence
            var parameterId = "p1"; // Temperature parameter
            
            // Act
            await _repository.RemoveParameterFromSequenceAsync(sequenceId, parameterId);
            
            // Assert
            Assert.Single(_sequenceParameters); // Should be only one left
            Assert.DoesNotContain(_sequenceParameters, sp => 
                sp.SequenceId == sequenceId && sp.ParameterId == parameterId);
            
            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }
    }
}
