using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.ScoresheetInstances;

namespace Unity.Flex.Scoresheets
{
    public class ScoresheetInstanceAppService(IScoresheetInstanceRepository scoresheetInstanceRepository) : FlexAppService, IScoresheetInstanceAppService
    {
        public async Task<ScoresheetInstanceDto> CreateAsync(CreateScoresheetInstanceDto dto)
        {
            return ObjectMapper.Map<ScoresheetInstance, ScoresheetInstanceDto>(await scoresheetInstanceRepository
                .InsertAsync(new ScoresheetInstance(Guid.NewGuid(), dto.ScoresheetId, dto.CorrelationId, dto.CorrelationProvider)));
        }

        public async Task<ScoresheetInstanceDto?> GetByCorrelationAsync(Guid id)
        {
            var instance = await scoresheetInstanceRepository.GetByCorrelationAsync(id);
            if (instance == null) return null;
            return ObjectMapper.Map<ScoresheetInstance, ScoresheetInstanceDto>(instance);
        }
    }
}
