using Microsoft.AspNetCore.Authorization;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.GrantPrograms
{
    [Authorize]
    public class GrantProgramAppService :
    CrudAppService<
        GrantProgram,      // The Grant entity
        GrantProgramDto,   // Used to show grants
        Guid,       //Primary key of the grant entity
        PagedAndSortedResultRequestDto, // Used for paging/sorting
        CreateUpdateGrantProgramDto>,          //Used to create/update a grant
    IGrantProgramAppService                    //implement the IGrantProgramAppService
    {
        public GrantProgramAppService(IRepository<GrantProgram, Guid> repository)
            : base(repository)
        {

        }        
    }
}
