using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.GrantManager.GrantPrograms;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.GrantPrograms
{
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
