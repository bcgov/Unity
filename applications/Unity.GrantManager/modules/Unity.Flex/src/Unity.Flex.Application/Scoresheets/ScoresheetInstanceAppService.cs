using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.ScoresheetInstances;
using Volo.Abp.Validation;

namespace Unity.Flex.Scoresheets
{
    public class ScoresheetInstanceAppService(IScoresheetInstanceRepository scoresheetInstanceRepository) : FlexAppService, IScoresheetInstanceAppService
    {
        public async Task<ScoresheetInstanceDto?> CreateAsync(CreateScoresheetInstanceDto dto)
        {
            if(dto.RelatedCorrelationId == null) 
            {
                // No assessments yet on the application, therefore create the scoresheet instance
                return ObjectMapper.Map<ScoresheetInstance, ScoresheetInstanceDto>(await scoresheetInstanceRepository
                    .InsertAsync(new ScoresheetInstance(Guid.NewGuid(), dto.ScoresheetId, dto.CorrelationId, dto.CorrelationProvider)));
            }
            else
            {
                // There are already other assessments in the application
                var otherAssessment = await scoresheetInstanceRepository.GetByCorrelationAsync(dto.RelatedCorrelationId ?? throw new AbpValidationException("Invalid Assessment Id."));
                if(otherAssessment != null)
                {
                    // Use the scoresheet of the other existing assessments
                    return ObjectMapper.Map<ScoresheetInstance, ScoresheetInstanceDto>(await scoresheetInstanceRepository
                        .InsertAsync(new ScoresheetInstance(Guid.NewGuid(), otherAssessment.ScoresheetId, dto.CorrelationId, dto.CorrelationProvider)));

                } else
                {
                    // Other assessments already uses default scoresheet, therefore use default scoresheet
                    return null;
                }
            }
            
        }

        public async Task<ScoresheetInstanceDto?> GetByCorrelationAsync(Guid id)
        {
            var instance = await scoresheetInstanceRepository.GetByCorrelationAsync(id);
            if (instance == null) return null;
            return ObjectMapper.Map<ScoresheetInstance, ScoresheetInstanceDto>(instance);
        }
    }
}
