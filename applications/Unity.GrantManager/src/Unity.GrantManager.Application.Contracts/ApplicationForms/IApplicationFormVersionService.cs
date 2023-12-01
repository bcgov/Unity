using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Forms;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.ApplicationForms
{
    public interface IApplicationFormVersionAppService : ICrudAppService<
            ApplicationFormVersionDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateApplicationFormVersionDto>
    {
        Task<IList<ApplicationFormVersionDto>> GetListAsync(Guid applicationFormId);
    }

}
