using System;
using Unity.GrantManager.Forms;
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
    }
}
