using System;
using System.Linq;
using System.Threading.Tasks;
using Instrument.Data.DataContext;
using Instrument.Data.Entities;
using Instrument.Data.Entities.Enums;
using Instrument.Data.Initialization;
using Instrument.Data.Interfaces;
using Instrument.Data.Repository;
using Instrument.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Instrument.Data.Tests.Integration
{
    /// <summary>
    /// Integration tests that use a real in-memory database to test
    /// repositories and services working together.
    /// </summary>
    public class IntegrationTests : IDisposable
    {
        private readonly SchedulerDbContext _dbContext;
        private readonly ISequenceRepository _sequenceRepository;
        private readonly IParameterRepository _parameterRepository;
        private readonly ISequenceGroupRepository _sequenceGroupRepository;
        private readonly SequenceService _sequenceService;
        private readonly SequenceGroupService _sequenceGroupService;
        
        public IntegrationTests()
        {
            // Create in-memory database for testing
            var options = new DbContextOptionsBuilder<SchedulerDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
                
            _dbContext = new SchedulerDbContext(options);
            
            // Create repositories
            _sequenceRepository = new SequenceRepository(_dbContext);
            _parameterRepository = new ParameterRepository(_dbContext);
            _sequenceGroupRepository = new SequenceGroupRepository(_dbContext);
            
            // Create services
            _sequenceService = new SequenceService(
                _sequenceRepository,
                _parameterRepository,
                new NullLogger<SequenceService>());
                
            _sequenceGroupService = new SequenceGroupService(
                _sequenceGroupRepository,
                _sequenceRepository,
                new NullLogger<SequenceGroupService>());
                
            // Seed test data
            SeedTestDataAsync().Wait();
        }
        
        private async Task SeedTestDataAsync()
        {
            // Add test sequences
            var sequence1 = new Sequence
            {
                Id = "test-seq-1",
                Name = "Integration Test Sequence 1",
                Description = "First integration test sequence",
                WorstCaseTime = TimeSpan.FromMinutes(5),
                CanBeParallel = true
            };
            
            var sequence2 = new Sequence
            {
                Id = "test-seq-2",
                Name = "Integration Test Sequence 2",
                Description = "Second integration test sequence",
                WorstCaseTime = TimeSpan.FromMinutes(10),
                CanBeParallel = false
            };
            
            await _dbContext.Sequences.AddRangeAsync(sequence1, sequence2);
            
            // Add test parameters
            var parameter1 = new Parameter
            {
                Id = "test-param-1",
                Name = "Test Temperature",
                Type = ParameterType.Number,
                DefaultValue = "25",
                Min = "0",
                Max = "100"
            };
            
            var parameter2 = new Parameter
            {
                Id = "test-param-2",
                Name = "Test Duration",
                Type = ParameterType.Number,
                DefaultValue = "60",
                Min = "10",
                Max = "300"
            };
            
            await _dbContext.Parameters.AddRangeAsync(parameter1, parameter2);
            
            // Link parameter to sequence
            var sequenceParameter = new SequenceParameter
            {
                SequenceId = sequence1.Id,
                ParameterId = parameter1.Id
            };
            
            await _dbContext.SequenceParameters.AddAsync(sequenceParameter);
            
            // Create a sequence group
            var group = new SequenceGroup
            {
                Id = "test-group-1",
                Name = "Integration Test Group",
                Description = "Group for integration testing"
            };
            
            await _dbContext.SequenceGroups.AddAsync(group);
            
            // Link sequence to group with order
            var groupSequence = new SequenceGroupSequences
            {
                SequenceGroupId = group.Id,
                SequenceId = sequence1.Id,
                Order = 1
            };
            
            await _dbContext.SequenceGroupSequences.AddAsync(groupSequence);
            
            // Save all changes
            await _dbContext.SaveChangesAsync();
        }
        
        [Fact]
        public async Task IntegrationTest_SequenceService_GetAndUpdateSequence()
        {
            // Act - Get sequence
            var sequence = await _sequenceService.GetSequenceByIdAsync("test-seq-1");
            
            // Assert - Verify sequence retrieved
            Assert.NotNull(sequence);
            Assert.Equal("Integration Test Sequence 1", sequence.Name);
            
            // Act - Update sequence
            sequence = sequence.Update(
                name: "Updated Integration Sequence",
                worstCaseTime: TimeSpan.FromMinutes(7),
                description: "Updated description",
                canBeParallel: false);
                
            await _sequenceService.UpdateSequenceAsync(sequence);
            
            // Act - Get updated sequence
            var updatedSequence = await _sequenceService.GetSequenceByIdAsync("test-seq-1");
            
            // Assert - Verify changes were saved
            Assert.NotNull(updatedSequence);
            Assert.Equal("Updated Integration Sequence", updatedSequence.Name);
            Assert.Equal(TimeSpan.FromMinutes(7), updatedSequence.WorstCaseTime);
            Assert.Equal("Updated description", updatedSequence.Description);
            Assert.False(updatedSequence.CanBeParallel);
        }
        
        [Fact]
        public async Task IntegrationTest_SequenceService_AddAndRemoveParameter()
        {
            // Act - Get current parameters
            var initialParameters = await _sequenceService.GetSequenceParametersAsync("test-seq-2");
            
            // Assert - Initially no parameters
            Assert.Empty(initialParameters);
            
            // Act - Add parameter
            await _sequenceService.AddParameterToSequenceAsync("test-seq-2", "test-param-2");
            
            // Act - Get updated parameters
            var updatedParameters = await _sequenceService.GetSequenceParametersAsync("test-seq-2");
            
            // Assert - Parameter was added
            Assert.Single(updatedParameters);
            Assert.Equal("test-param-2", updatedParameters.First().Id);
            
            // Act - Remove parameter
            await _sequenceService.RemoveParameterFromSequenceAsync("test-seq-2", "test-param-2");
            
            // Act - Get parameters after removal
            var finalParameters = await _sequenceService.GetSequenceParametersAsync("test-seq-2");
            
            // Assert - Parameter was removed
            Assert.Empty(finalParameters);
        }
        
        [Fact]
        public async Task IntegrationTest_SequenceGroupService_AddSequenceToGroup()
        {
            // Act - Get initial group with sequences
            var initialGroup = await _sequenceGroupService.GetSequenceGroupWithSequencesAsync("test-group-1");
            
            // Assert - Initially one sequence
            Assert.NotNull(initialGroup);
            Assert.Single(initialGroup.SequenceGroupSequences);
            Assert.Equal("test-seq-1", initialGroup.SequenceGroupSequences.First().SequenceId);
            
            // Act - Add another sequence to group
            await _sequenceGroupService.AddSequenceToGroupAsync("test-group-1", "test-seq-2", 2);
            
            // Act - Get updated group
            var updatedGroup = await _sequenceGroupService.GetSequenceGroupWithSequencesAsync("test-group-1");
            
            // Assert - Now has two sequences
            Assert.NotNull(updatedGroup);
            Assert.Equal(2, updatedGroup.SequenceGroupSequences.Count);
            Assert.Contains(updatedGroup.SequenceGroupSequences, sgs => sgs.SequenceId == "test-seq-2" && sgs.Order == 2);
            
            // Act - Get ordered sequences
            var orderedSequences = await _sequenceGroupService.GetOrderedSequencesAsync("test-group-1");
            
            // Assert - Sequences are in correct order
            Assert.Equal(2, orderedSequences.Count);
            Assert.Equal("test-seq-1", orderedSequences[1].Id);
            Assert.Equal("test-seq-2", orderedSequences[2].Id);
        }
        
        [Fact]
        public async Task IntegrationTest_CompleteWorkflow_CreateSequenceGroupWithSequencesAndParameters()
        {
            // Step 1: Create a new parameter
            var parameter = new Parameter
            {
                Id = "workflow-param",
                Name = "Workflow Parameter",
                Type = ParameterType.String,
                DefaultValue = "default"
            };
            
            await _parameterRepository.AddAsync(parameter);
            
            // Step 2: Create a new sequence with the parameter
            var sequence = new Sequence
            {
                Id = "workflow-seq",
                Name = "Workflow Sequence",
                Description = "Created in integration test workflow",
                WorstCaseTime = TimeSpan.FromMinutes(15),
                CanBeParallel = true
            };
            
            var createdSequence = await _sequenceService.CreateSequenceAsync(sequence);
            await _sequenceService.AddParameterToSequenceAsync(createdSequence.Id, parameter.Id);
            
            // Step 3: Create a sequence group
            var group = await _sequenceGroupService.CreateSequenceGroupAsync(
                "Workflow Group", 
                "Created in integration test workflow");
                
            // Step 4: Add existing and new sequence to group
            await _sequenceGroupService.AddSequenceToGroupAsync(group.Id, "test-seq-1", 1);
            await _sequenceGroupService.AddSequenceToGroupAsync(group.Id, createdSequence.Id, 2);
            
            // Step 5: Validate the group
            var isValid = await _sequenceGroupService.ValidateSequenceGroupAsync(group.Id);
            Assert.True(isValid);
            
            // Step 6: Reorder sequences
            await _sequenceGroupService.ReorderSequenceInGroupAsync(group.Id, createdSequence.Id, 1);
            await _sequenceGroupService.ReorderSequenceInGroupAsync(group.Id, "test-seq-1", 2);
            
            // Step 7: Get final ordered sequences
            var orderedSequences = await _sequenceGroupService.GetOrderedSequencesAsync(group.Id);
            
            // Assert final state
            Assert.Equal(2, orderedSequences.Count);
            Assert.Equal(createdSequence.Id, orderedSequences[1].Id);
            Assert.Equal("test-seq-1", orderedSequences[2].Id);
            
            // Check that parameter is linked to sequence
            var parameters = await _sequenceService.GetSequenceParametersAsync(createdSequence.Id);
            Assert.Single(parameters);
            Assert.Equal(parameter.Id, parameters.First().Id);
        }
        
        public void Dispose()
        {
            // Clean up test database
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
