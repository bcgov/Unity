using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.GrantApplications;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IApplicationStatusService))]
public class ApplicationStatusAppService : CrudAppService<
        ApplicationStatus, 
        ApplicationStatusDto, 
        Guid, 
        PagedAndSortedResultRequestDto>, 
        IApplicationStatusService 
{
    private readonly IApplicationStatusRepository _applicationStatusRepository;
    public ApplicationStatusAppService(IApplicationStatusRepository repository) : base(repository)
    {
        _applicationStatusRepository = repository;
    }

    public async Task<PagedResultDto<ApplicationStatusDto>> GetListAsync()
    {       

        var statuses = await _applicationStatusRepository.GetListAsync();        

        var totalCount = await _applicationStatusRepository.CountAsync();

        var mapperConfig = new MapperConfiguration(cfg => {
            cfg.CreateMap<ApplicationStatus, ApplicationStatusDto>();
        });
        var mapper = mapperConfig.CreateMapper();
                 
        return new PagedResultDto<ApplicationStatusDto>(
            totalCount,
            mapper.Map<List<ApplicationStatus>, List<ApplicationStatusDto>>(statuses)
        );
    }
}
