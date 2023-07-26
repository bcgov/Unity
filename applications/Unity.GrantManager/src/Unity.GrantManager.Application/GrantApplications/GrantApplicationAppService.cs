using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.GrantApplications
{
    public class GrantApplicationAppService :
        CrudAppService<
        GrantApplication,
        GrantApplicationDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateGrantApplicationDto>,
        IGrantApplicationAppService
    {
        public GrantApplicationAppService(IRepository<GrantApplication, Guid> repository)
             : base(repository)
        {
        }

        public override async Task<PagedResultDto<GrantApplicationDto>> GetListAsync(PagedAndSortedResultRequestDto input)
        {
            // What we store in the DB vs what we present will likely be very different
            // This can also be done in conjunction with AutoMapper
            // There is no sorting here etc as this is done in the base method
            return await (Task.FromResult(new PagedResultDto<GrantApplicationDto>(
                2,
                new List<GrantApplicationDto>()
                {
                    new GrantApplicationDto()
                    {
                        ProjectName = "New Helicopter Fund",
                        ReferenceNo = "ABC123",
                        EligibleAmount = 10000.00M,
                        RequestedAmount = 12500.00M,
                        Assignees = new List<GrantApplicationAssigneeDto>() { new GrantApplicationAssigneeDto() { UserId = Guid.NewGuid(), Username = "John Smith" } },
                        Probability = 70,
                        ProposalDate = new DateTime(2022, 10, 02),
                        SubmissionDate = new DateTime(2023, 01, 02),
                        Status = GrantApplicationStatus.Active,
                    },
                    new GrantApplicationDto()
                    {
                        ProjectName = "Courtyard Landscaping",
                        ReferenceNo = "AF17GB",
                        EligibleAmount = 12400.00M,
                        RequestedAmount = 22500.00M,
                        Assignees = new List<GrantApplicationAssigneeDto>() { new GrantApplicationAssigneeDto() { UserId = Guid.NewGuid(), Username = "John Smith" } },
                        Probability = 60,
                        ProposalDate = new DateTime(2022, 10, 02),
                        SubmissionDate = new DateTime(2023, 01, 02),
                        Status = GrantApplicationStatus.Awaiting
                    }
                }
            )));
        }
    }
}



