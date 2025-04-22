using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Instrument.Scheduling.Data.Entities;
using Instrument.Scheduling.Data.Interfaces;

namespace Instrument.Scheduling.Data.Services
{
    public class SequenceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SequenceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SequenceDefinition?> GetSequenceAsync(string id)
        {
            return await _unitOfWork.SequenceDefinitions.GetByIdAsync(id);
        }

        public async Task CreateSequenceAsync(SequenceDefinition sequence)
        {
            sequence.CreatedDate = DateTime.UtcNow;
            await _unitOfWork.SequenceDefinitions.AddAsync(sequence);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateSequenceAsync(SequenceDefinition sequence)
        {
            await _unitOfWork.SequenceDefinitions.UpdateAsync(sequence);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteSequenceAsync(string id)
        {
            await _unitOfWork.SequenceDefinitions.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<SequenceDefinition>> GetAllSequencesAsync()
        {
            return await _unitOfWork.SequenceDefinitions.GetAllAsync();
        }

        public async Task<IEnumerable<SequenceDefinition>> SearchSequencesAsync(
            Func<SequenceDefinition, bool> predicate)
        {
            var queryable = await _unitOfWork.SequenceDefinitions.GetQueryableAsync();
            return queryable.Where(predicate).ToList();
        }
    }
}
