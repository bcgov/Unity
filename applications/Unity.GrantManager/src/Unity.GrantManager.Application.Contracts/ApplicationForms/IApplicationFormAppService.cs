using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Forms;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.ApplicationForms
{
    public interface IApplicationFormAppService : ICrudAppService<
            ApplicationFormDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateApplicationFormDto>
    {
        Task<AddressType> GetElectoralDistrictAddressTypeAsync(Guid id);
        Task<IList<ApplicationFormVersionDto>> GetVersionsAsync(Guid id);
        Task<IList<ApplicationFormVersionDto>> GetPublishedVersionsAsync(Guid id);
        Task PatchOtherConfig(Guid id, OtherConfigDto config);
    }
}
