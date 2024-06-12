using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp.Validation;

namespace Unity.Flex.Scoresheets
{
    public class ScoresheetInstanceAppService : FlexAppService, IScoresheetInstanceAppService
    {
        private readonly IScoresheetInstanceRepository _instanceRepository;

        public ScoresheetInstanceAppService(IScoresheetInstanceRepository instanceRepository)
        {
            _instanceRepository = instanceRepository;
        }

        
    }
}
