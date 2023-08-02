using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantPrograms
{
    public interface IGrantProgramAppService :
        ICrudAppService< //Defines CRUD methods
            GrantProgramDto, //Used to show books
            Guid, //Primary key of the book entity
            PagedAndSortedResultRequestDto, //Used for paging/sorting
            CreateUpdateGrantProgramDto> //Used to create/update a book
    {

    }
}