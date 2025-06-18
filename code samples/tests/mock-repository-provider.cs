using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Instrument.Data.Entities;
using Instrument.Data.Interfaces;
using Moq;
using Moq.AutoMock;

namespace Instrument.Data.Tests.Mocks
{
    /// <summary>
    /// A factory class for creating and configuring repository mocks for unit testing services.
    /// This class helps maintain consistency and reduce duplication in test setup.
    /// </summary>
    public class MockRepositoryProvider
    {
        private readonly AutoMocker _mocker;
        
        public MockRepositoryProvider()
        {
            _mocker = new AutoMocker();
            SetupDefaultBehaviors();
        }
        
        /// <summary>
        /// Configures safe default behaviors for repository methods
        /// </summary>
        private void SetupDefaultBehaviors()
        {
            // Setup default behaviors for commonly used repositories
            
            // Sequence Repository defaults
            GetMock<ISequenceRepository>()
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Sequence>());
            
            GetMock<ISequenceRepository>()
                .Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => null);
                
            // Parameter Repository defaults
            GetMock<IParameterRepository>()
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Parameter>());
                
            GetMock<IParameterRepository>()
                .Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => null);
                
            // SequenceGroup Repository defaults
            GetMock<ISequenceGroupRepository>()
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<SequenceGroup>());
                
            GetMock<ISequenceGroupRepository>()
                .Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => null);
                
            // Resource Repository defaults
            GetMock<IResourceRepository>()
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Resource>());
                
            GetMock<IResourceRepository>()
                .Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => null);
                
            // Range Repository defaults
            GetMock<IRangeRepository>()
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Range>());
                
            GetMock<IRangeRepository>()
                .Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => null);
        }
        
        /// <summary>
        /// Gets an existing mock or creates a new one if it doesn't exist
        /// </summary>
        public Mock<T> GetMock<T>() where T : class
        {
            return _mocker.GetMock<T>();
        }
        
        /// <summary>
        /// Creates a service with all dependencies auto-mocked
        /// </summary>
        public T CreateService<T>() where T : class
        {
            return _mocker.CreateInstance<T>();
        }
        
        /// <summary>
        /// Sets up basic CRUD operations for any repository
        /// </summary>
        public void SetupRepository<T>(Mock<IRepository<T>> mock, List<T> entities) where T : class
        {
            // Setup GetAllAsync
            mock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(entities);
                
            // Setup GetByIdAsync using reflection to get the Id property
            mock.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => entities.FirstOrDefault(e => 
                    e.GetType().GetProperty("Id").GetValue(e).ToString() == id));
                    
            // Setup GetQueryableAsync
            mock.Setup(r => r.GetQueryableAsync())
                .ReturnsAsync(entities.AsQueryable());
                
            // Setup AddAsync
            mock.Setup(r => r.AddAsync(It.IsAny<T>()))
                .ReturnsAsync((T entity) => {
                    entities.Add(entity);
                    return entity;
                });
                
            // Setup UpdateAsync
            mock.Setup(r => r.UpdateAsync(It.IsAny<T>()))
                .Returns(Task.CompletedTask);
                
            // Setup DeleteAsync
            mock.Setup(r => r.DeleteAsync(It.IsAny<string>()))
                .Returns((string id) => {
                    var entity = entities.FirstOrDefault(e => 
                        e.GetType().GetProperty("Id").GetValue(e).ToString() == id);
                    
                    if (entity != null)
                    {
                        entities.Remove(entity);
                    }
                    
                    return Task.CompletedTask;
                });
        }
        
        /// <summary>
        /// Sets up Sequence repository with test data
        /// </summary>
        public void SetupSequences(List<Sequence> sequences)
        {
            var mock = GetMock<ISequenceRepository>();
            
            // Setup base repository methods
            SetupRepository(mock, sequences);
            
            // Setup specialized methods
            mock.Setup(r => r.GetByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((string namePattern) => 
                {
                    return sequences.Where(s => s.Name.Contains(namePattern)).ToList();
                });
                
            mock.Setup(r => r.GetByParameterIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string parameterId) => 
                {
                    return sequences.Where(s => 
                        s.SequenceParameters != null && 
                        s.SequenceParameters.Any(sp => sp.ParameterId == parameterId))
                        .ToList();
                });
                
            mock.Setup(r => r.GetWithParametersAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => 
                {
                    return sequences.FirstOrDefault(s => s.Id == id);
                });
                
            mock.Setup(r => r.AddParameterToSequenceAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string sequenceId, string parameterId) => 
                {
                    var sequence = sequences.FirstOrDefault(s => s.Id == sequenceId);
                    if (sequence != null && sequence.SequenceParameters != null)
                    {
                        sequence.SequenceParameters.Add(new SequenceParameter 
                        { 
                            SequenceId = sequenceId, 
                            ParameterId = parameterId 
                        });
                    }
                    return Task.CompletedTask;
                });
                
            mock.Setup(r => r.RemoveParameterFromSequenceAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string sequenceId, string parameterId) => 
                {
                    var sequence = sequences.FirstOrDefault(s => s.Id == sequenceId);
                    if (sequence != null && sequence.SequenceParameters != null)
                    {
                        var param = sequence.SequenceParameters.FirstOrDefault(sp => 
                            sp.SequenceId == sequenceId && sp.ParameterId == parameterId);
                            
                        if (param != null)
                        {
                            sequence.SequenceParameters.Remove(param);
                        }
                    }
                    return Task.CompletedTask;
                });
        }
        
        /// <summary>
        /// Sets up Parameter repository with test data
        /// </summary>
        public void SetupParameters(List<Parameter> parameters)
        {
            var mock = GetMock<IParameterRepository>();
            
            // Setup base repository methods
            SetupRepository(mock, parameters);
            
            // Setup specialized methods
            mock.Setup(r => r.GetBySequenceIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string sequenceId) => 
                {
                    return parameters.Where(p => 
                        p.SequenceParameters != null && 
                        p.SequenceParameters.Any(sp => sp.SequenceId == sequenceId))
                        .ToList();
                });
                
            mock.Setup(r => r.GetByRangeIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string rangeId) => 
                {
                    return parameters.Where(p => p.RangeId == rangeId).ToList();
                });
                
            mock.Setup(r => r.GetByResourceIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string resourceId) => 
                {
                    return parameters.Where(p => p.ResourceId == resourceId).ToList();
                });
        }
        
        /// <summary>
        /// Sets up SequenceGroup repository with test data
        /// </summary>
        public void SetupSequenceGroups(List<SequenceGroup> groups, List<Sequence> sequences = null)
        {
            var mock = GetMock<ISequenceGroupRepository>();
            
            // Setup base repository methods
            SetupRepository(mock, groups);
            
            // Setup specialized methods
            mock.Setup(r => r.GetWithSequencesAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => 
                {
                    return groups.FirstOrDefault(g => g.Id == id);
                });
                
            mock.Setup(r => r.GetSequencesInGroupAsync(It.IsAny<string>()))
                .ReturnsAsync((string groupId) => 
                {
                    if (sequences == null)
                        return new List<Sequence>();
                        
                    var group = groups.FirstOrDefault(g => g.Id == groupId);
                    if (group == null || group.SequenceGroupSequences == null)
                        return new List<Sequence>();
                        
                    return group.SequenceGroupSequences
                        .OrderBy(sgs => sgs.Order)
                        .Select(sgs => sequences.FirstOrDefault(s => s.Id == sgs.SequenceId))
                        .Where(s => s != null)
                        .ToList();
                });
        }
        
        /// <summary>
        /// Sets up Resource repository with test data
        /// </summary>
        public void SetupResources(List<Resource> resources)
        {
            var mock = GetMock<IResourceRepository>();
            
            // Setup base repository methods
            SetupRepository(mock, resources);
            
            // Setup specialized methods (if any)
        }
        
        /// <summary>
        /// Sets up Range repository with test data
        /// </summary>
        public void SetupRanges(List<Range> ranges)
        {
            var mock = GetMock<IRangeRepository>();
            
            // Setup base repository methods
            SetupRepository(mock, ranges);
            
            // Setup specialized methods (if any)
        }
    }
}
