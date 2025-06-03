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
    /// Unit tests for the SequenceGroupService which has more complex behaviors
    /// interacting with multiple repositories and validation logic
    /// </summary>
    public class SequenceGroupServiceTests : ServiceTestBase<SequenceGroupService>
    {
        private readonly List<SequenceGroup> _testGroups;
        private readonly List<Sequence> _testSequences;
        
        public SequenceGroupServiceTests() : base()
        {
            // Setup test data - Sequences
            _testSequences = new List<Sequence>
            {
                new Sequence 
                { 
                    Id = "seq1", 
                    Name = "First Sequence", 
                    WorstCaseTime = TimeSpan.FromMinutes(5),
                    CanBeParallel = true,
                    SequenceParameters = new List<SequenceParameter>()
                },
                new Sequence 
                { 
                    Id = "seq2", 
                    Name = "Second Sequence", 
                    WorstCaseTime = TimeSpan.FromMinutes(10),
                    CanBeParallel = false,
                    SequenceParameters = new List<SequenceParameter>()
                },
                new Sequence 
                { 
                    Id = "seq3", 
                    Name = "Third Sequence", 
                    WorstCaseTime = TimeSpan.FromMinutes(7),
                    CanBeParallel = true,
                    SequenceParameters = new List<SequenceParameter>()
                }
            };
            
            // Setup test data - Groups
            _testGroups = new List<SequenceGroup>
            {
                new SequenceGroup
                {
                    Id = "group1",
                    Name = "Test Group 1",
                    Description = "First test group",
                    SequenceGroupSequences = new List<SequenceGroupSequences>
                    {
                        new SequenceGroupSequences
                        {
                            SequenceGroupId = "group1",
                            SequenceId = "seq1",
                            Order = 1,
                            Sequence = _testSequences[0]
                        },
                        new SequenceGroupSequences
                        {
                            SequenceGroupId = "group1",
                            SequenceId = "seq2",
                            Order = 2,
                            Sequence = _testSequences[1]
                        }
                    }
                },
                new SequenceGroup
                {
                    Id = "group2",
                    Name = "Empty Group",
                    Description = "A group with no sequences",
                    SequenceGroupSequences = new List<SequenceGroupSequences>()
                }
            };
            
            // Configure mocks with test data
            Mocks.SetupSequences(_testSequences);
            Mocks.SetupSequenceGroups(_testGroups, _testSequences);
        }
        
        [Fact]
        public async Task GetAllSequenceGroupsAsync_ReturnsAllGroups()
        {
            // Act
            var result = await Service.GetAllSequenceGroupsAsync();
            
            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, g => g.Id == "group1");
            Assert.Contains(result, g => g.Id == "group2");
            
            // Verify repository was called
            Mocks.GetMock<ISequenceGroupRepository>().Verify(r => r.GetAllAsync(), Times.Once);
        }
        
        [Fact]
        public async Task GetSequenceGroupByIdAsync_WithValidId_ReturnsGroup()
        {
            // Act
            var result = await Service.GetSequenceGroupByIdAsync("group1");
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Group 1", result.Name);
            
            // Verify repository was called
            Mocks.GetMock<ISequenceGroupRepository>().Verify(r => r.GetByIdAsync("group1"), Times.Once);
        }
        
        [Fact]
        public async Task GetSequenceGroupWithSequencesAsync_ReturnsGroupWithSequences()
        {
            // Act
            var result = await Service.GetSequenceGroupWithSequencesAsync("group1");
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Group 1", result.Name);
            Assert.Equal(2, result.SequenceGroupSequences.Count);
            
            // Verify repository was called
            Mocks.GetMock<ISequenceGroupRepository>().Verify(r => r.GetWithSequencesAsync("group1"), Times.Once);
        }
        
        [Fact]
        public async Task CreateSequenceGroupAsync_CreatesNewGroup()
        {
            // Arrange
            var groupName = "New Group";
            var groupDescription = "A new test group";
            
            // Configure repository to return the new group
            Mocks.GetMock<ISequenceGroupRepository>()
                .Setup(r => r.AddAsync(It.IsAny<SequenceGroup>()))
                .ReturnsAsync((SequenceGroup group) => group);
                
            // Act
            var result = await Service.CreateSequenceGroupAsync(groupName, groupDescription);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(groupName, result.Name);
            Assert.Equal(groupDescription, result.Description);
            Assert.Empty(result.SequenceGroupSequences);
            
            // Verify repository was called
            Mocks.GetMock<ISequenceGroupRepository>().Verify(
                r => r.AddAsync(It.Is<SequenceGroup>(g => 
                    g.Name == groupName && g.Description == groupDescription)), 
                Times.Once);
        }
        
        [Fact]
        public async Task AddSequenceToGroupAsync_WithValidIds_AddsSequenceToGroup()
        {
            // Arrange
            var groupId = "group1";
            var sequenceId = "seq3"; // Third sequence - not yet in group1
            var order = 3;
            
            // Act
            var result = await Service.AddSequenceToGroupAsync(groupId, sequenceId, order);
            
            // Assert
            Assert.True(result);
            
            // Verify repository calls
            Mocks.GetMock<ISequenceGroupRepository>().Verify(r => r.GetByIdAsync(groupId), Times.Once);
            Mocks.GetMock<ISequenceRepository>().Verify(r => r.GetByIdAsync(sequenceId), Times.Once);
            Mocks.GetMock<ISequenceGroupRepository>().Verify(
                r => r.AddSequenceToGroupAsync(groupId, sequenceId, order), Times.Once);
        }
        
        [Fact]
        public async Task AddSequenceToGroupAsync_WithInvalidGroupId_ReturnsFalseAndLogsWarning()
        {
            // Arrange
            var invalidGroupId = "invalid-group";
            var sequenceId = "seq1";
            var order = 1;
            
            // Configure repository to return null for invalid group ID
            Mocks.GetMock<ISequenceGroupRepository>()
                .Setup(r => r.GetByIdAsync(invalidGroupId))
                .ReturnsAsync((SequenceGroup)null);
                
            // Setup log capture
            List<(LogLevel Level, string Message)> capturedLogs;
            SetupLogCapture<SequenceGroupService>(out capturedLogs);
            
            // Act
            var result = await Service.AddSequenceToGroupAsync(invalidGroupId, sequenceId, order);
            
            // Assert
            Assert.False(result);
            
            // Verify warning was logged
            Assert.Contains(capturedLogs, log => 
                log.Level == LogLevel.Warning && 
                log.Message.Contains(invalidGroupId));
                
            // Verify repository interactions
            Mocks.GetMock<ISequenceGroupRepository>().Verify(r => r.GetByIdAsync(invalidGroupId), Times.Once);
            Mocks.GetMock<ISequenceRepository>().Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
            Mocks.GetMock<ISequenceGroupRepository>().Verify(
                r => r.AddSequenceToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), 
                Times.Never);
        }
        
        [Fact]
        public async Task AddSequenceToGroupAsync_WithInvalidSequenceId_ReturnsFalseAndLogsWarning()
        {
            // Arrange
            var groupId = "group1";
            var invalidSequenceId = "invalid-sequence";
            var order = 3;
            
            // Configure repository to return null for invalid sequence ID
            Mocks.GetMock<ISequenceRepository>()
                .Setup(r => r.GetByIdAsync(invalidSequenceId))
                .ReturnsAsync((Sequence)null);
                
            // Setup log capture
            List<(LogLevel Level, string Message)> capturedLogs;
            SetupLogCapture<SequenceGroupService>(out capturedLogs);
            
            // Act
            var result = await Service.AddSequenceToGroupAsync(groupId, invalidSequenceId, order);
            
            // Assert
            Assert.False(result);
            
            // Verify warning was logged
            Assert.Contains(capturedLogs, log => 
                log.Level == LogLevel.Warning && 
                log.Message.Contains(invalidSequenceId));
                
            // Verify repository interactions
            Mocks.GetMock<ISequenceGroupRepository>().Verify(r => r.GetByIdAsync(groupId), Times.Once);
            Mocks.GetMock<ISequenceRepository>().Verify(r => r.GetByIdAsync(invalidSequenceId), Times.Once);
            Mocks.GetMock<ISequenceGroupRepository>().Verify(
                r => r.AddSequenceToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), 
                Times.Never);
        }
        
        [Fact]
        public async Task GetOrderedSequencesAsync_ReturnsSequencesInOrder()
        {
            // Act
            var result = await Service.GetOrderedSequencesAsync("group1");
            
            // Assert
            var sequences = result.ToList();
            Assert.Equal(2, sequences.Count);
            Assert.Equal(1, sequences[0].Key); // First order value
            Assert.Equal("seq1", sequences[0].Value.Id); // First sequence
            
            Assert.Equal(2, sequences[1].Key); // Second order value
            Assert.Equal("seq2", sequences[1].Value.Id); // Second sequence
            
            // Verify repository was called
            Mocks.GetMock<ISequenceGroupRepository>().Verify(
                r => r.GetWithSequencesAsync("group1"), Times.Once);
        }
        
        [Fact]
        public async Task RemoveSequenceFromGroupAsync_RemovesSequence()
        {
            // Act
            var result = await Service.RemoveSequenceFromGroupAsync("group1", "seq1");
            
            // Assert
            Assert.True(result);
            
            // Verify repository was called
            Mocks.GetMock<ISequenceGroupRepository>().Verify(
                r => r.RemoveSequenceFromGroupAsync("group1", "seq1"), Times.Once);
        }
        
        [Fact]
        public async Task ReorderSequenceInGroupAsync_ChangesSequenceOrder()
        {
            // Act
            var result = await Service.ReorderSequenceInGroupAsync("group1", "seq2", 1);
            
            // Assert
            Assert.True(result);
            
            // Verify repository was called
            Mocks.GetMock<ISequenceGroupRepository>().Verify(
                r => r.UpdateSequenceOrderInGroupAsync("group1", "seq2", 1), Times.Once);
        }
        
        [Fact]
        public async Task ValidateSequenceGroupAsync_WithValidGroup_ReturnsTrue()
        {
            // Act
            var result = await Service.ValidateSequenceGroupAsync("group1");
            
            // Assert
            Assert.True(result);
            
            // Verify repository calls
            Mocks.GetMock<ISequenceGroupRepository>().Verify(
                r => r.GetWithSequencesAsync("group1"), Times.Once);
            
            // Verify validation passed
            VerifyLogInformation<SequenceGroupService>();
        }
        
        [Fact]
        public async Task ValidateSequenceGroupAsync_WithEmptyGroup_ReturnsFalseAndLogsWarning()
        {
            // Act
            var result = await Service.ValidateSequenceGroupAsync("group2");
            
            // Assert
            Assert.False(result);
            
            // Verify repository calls
            Mocks.GetMock<ISequenceGroupRepository>().Verify(
                r => r.GetWithSequencesAsync("group2"), Times.Once);
            
            // Verify validation failed with warning
            VerifyLogWarning<SequenceGroupService>();
        }
        
        [Fact]
        public async Task DeleteSequenceGroupAsync_DeletesGroup()
        {
            // Act
            var result = await Service.DeleteSequenceGroupAsync("group1");
            
            // Assert
            Assert.True(result);
            
            // Verify repository was called
            Mocks.GetMock<ISequenceGroupRepository>().Verify(
                r => r.DeleteAsync("group1"), Times.Once);
        }
        
        [Fact]
        public async Task DeleteSequenceGroupAsync_WithDatabaseError_LogsErrorAndReturnsFalse()
        {
            // Arrange - Setup repository to throw an exception
            Mocks.GetMock<ISequenceGroupRepository>()
                .Setup(r => r.DeleteAsync("group1"))
                .ThrowsAsync(new DbUpdateException("Database error", new Exception("Inner exception")));
                
            // Act
            var result = await Service.DeleteSequenceGroupAsync("group1");
            
            // Assert
            Assert.False(result);
            
            // Verify error was logged
            VerifyLogError<SequenceGroupService>();
        }
        
        [Fact]
        public async Task GetSequenceGroupWithSequencesAsync_DatabaseError_ThrowsStorageProviderException()
        {
            // Arrange
            Mocks.GetMock<ISequenceGroupRepository>()
                .Setup(r => r.GetWithSequencesAsync(It.IsAny<string>()))
                .ThrowsAsync(new DbUpdateException("Database error", new Exception("Inner exception")));
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<StorageProviderException>(
                () => Service.GetSequenceGroupWithSequencesAsync("group1"));
                
            Assert.Contains("Failed to retrieve sequence group", exception.Message);
            Assert.IsType<DbUpdateException>(exception.InnerException);
            
            // Verify error was logged
            VerifyLogError<SequenceGroupService>();
        }
    }
}
