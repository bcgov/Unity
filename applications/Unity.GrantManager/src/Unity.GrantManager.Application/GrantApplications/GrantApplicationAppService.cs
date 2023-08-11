using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;

namespace Unity.GrantManager.GrantApplications
{
    [Authorize]
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
            return await Task.FromResult(new PagedResultDto<GrantApplicationDto>(
                GetMockData().Count,
                GetSortedAndPagedData(input)));
        }

        private static List<GrantApplicationDto> GetSortedAndPagedData(PagedAndSortedResultRequestDto input)
        {
            var query = GetMockData()
                .AsQueryable();

            return query
                .Sort(input)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToList();
        }

        private static List<GrantApplicationDto> GetMockData()
        {
            return new List<GrantApplicationDto>()
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
                        Status = GrantApplicationStatus.Active
                    },
                    new GrantApplicationDto()
                    {
                        ProjectName = "Shoebox",
                        ReferenceNo = "HCA123",
                        EligibleAmount = 22300.00M,
                        RequestedAmount = 332500.00M,
                        Assignees = new List<GrantApplicationAssigneeDto>() { new GrantApplicationAssigneeDto() { UserId = Guid.NewGuid(), Username = "John Smith" } },
                        Probability = 50,
                        ProposalDate = new DateTime(2022, 11, 03),
                        SubmissionDate = new DateTime(2023, 1, 04),
                        Status = GrantApplicationStatus.Awaiting
                    },
                     new GrantApplicationDto()
                    {
                        ProjectName = "Pony Club",
                        ReferenceNo = "111BGC",
                        EligibleAmount = 2212400.00M,
                        RequestedAmount = 2312500.00M,                        
                        Probability = 90,
                        ProposalDate = new DateTime(2021, 01, 03),
                        SubmissionDate = new DateTime(2023, 02, 02),
                        Status = GrantApplicationStatus.Awaiting
                    },
                      new GrantApplicationDto()
                    {
                        ProjectName = "Village Fountain Repair",
                        ReferenceNo = "BB11FF",
                        EligibleAmount = 13100.00M,
                        RequestedAmount = 11100.00M,
                        Assignees = new List<GrantApplicationAssigneeDto>() { new GrantApplicationAssigneeDto() { UserId = Guid.NewGuid(), Username = "John Smith" } },
                        Probability = 40,
                        ProposalDate = new DateTime(2024, 05, 02),
                        SubmissionDate = new DateTime(2025, 01, 03),
                        Status = GrantApplicationStatus.Awaiting
                    },
                       new GrantApplicationDto()
                    {
                        ProjectName = "Hoover",
                        ReferenceNo = "GG1731",
                        EligibleAmount = 232400.00M,
                        RequestedAmount = 332500.00M,
                        Assignees = new List<GrantApplicationAssigneeDto>()
                        {
                            new GrantApplicationAssigneeDto() { UserId = Guid.NewGuid(), Username = "John Smith" },
                            new GrantApplicationAssigneeDto() { UserId = Guid.NewGuid(), Username = "Jane Doe" }
                        },
                        Probability = 55,
                        ProposalDate = new DateTime(2022, 10, 02),
                        SubmissionDate = new DateTime(2023, 01, 02),
                        Status = GrantApplicationStatus.Declined
                    },
                        new GrantApplicationDto()
                    {
                        ProjectName = "Tree Planting",
                        ReferenceNo = "BBNNGG",
                        EligibleAmount = 1312400.00M,
                        RequestedAmount = 444400.00M,
                        Assignees = new List<GrantApplicationAssigneeDto>() { new GrantApplicationAssigneeDto() { UserId = Guid.NewGuid(), Username = "John Smith" } },
                        Probability = 20,
                        ProposalDate = new DateTime(2023, 10, 03),
                        SubmissionDate = new DateTime(2023, 02, 02),
                        Status = GrantApplicationStatus.Awaiting
                    },
                         new GrantApplicationDto()
                    {
                        ProjectName = "Pizza Joint",
                        ReferenceNo = "FF13BB",
                        EligibleAmount = 332100.00M,
                        RequestedAmount = 32100.00M,
                          Assignees = new List<GrantApplicationAssigneeDto>()
                        {
                            new GrantApplicationAssigneeDto() { UserId = Guid.NewGuid(), Username = "John Smith" },
                            new GrantApplicationAssigneeDto() { UserId = Guid.NewGuid(), Username = "Jane Doe" }
                        },
                        Probability = 55,
                        ProposalDate = new DateTime(2022, 09, 01),
                        SubmissionDate = new DateTime(2023, 08, 03),
                        Status = GrantApplicationStatus.Awaiting
                    },
                          new GrantApplicationDto()
                    {
                        ProjectName = "Froghopper Express",
                        ReferenceNo = "AD1FFB",
                        EligibleAmount = 3312300.00M,
                        RequestedAmount = 11100.00M,
                        Assignees = new List<GrantApplicationAssigneeDto>() { new GrantApplicationAssigneeDto() { UserId = Guid.NewGuid(), Username = "John Smith" } },
                        Probability = 80,
                        ProposalDate = new DateTime(2022, 11, 03),
                        SubmissionDate = new DateTime(2023, 11, 05),
                        Status = GrantApplicationStatus.Awaiting
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
                        Status = GrantApplicationStatus.Approved
                    },
                    new GrantApplicationDto()
                    {
                        ProjectName = "Disco Ball",
                        ReferenceNo = "AF11BB",
                        EligibleAmount = 1400.00M,
                        RequestedAmount = 3500.00M,
                        Assignees = new List<GrantApplicationAssigneeDto>() { new GrantApplicationAssigneeDto() { UserId = Guid.NewGuid(), Username = "John Smith" } },
                        Probability = 10,
                        ProposalDate = new DateTime(2023, 10, 03),
                        SubmissionDate = new DateTime(2023, 11, 02),
                        Status = GrantApplicationStatus.Scoring
                    },
                    new GrantApplicationDto()
                    {
                        ProjectName = "Gymnasium Repair",
                        ReferenceNo = "GYM007",
                        EligibleAmount = 332400.00M,
                        RequestedAmount = 112500.00M,
                        Assignees = new List<GrantApplicationAssigneeDto>()
                        {
                            new GrantApplicationAssigneeDto() { UserId = Guid.NewGuid(), Username = "John Smith" },
                            new GrantApplicationAssigneeDto() { UserId = Guid.NewGuid(), Username = "Jane Doe" }
                        },
                        Probability = 75,
                        ProposalDate = new DateTime(2023, 10, 02),
                        SubmissionDate = new DateTime(2023, 01, 02),
                        Status = GrantApplicationStatus.OnHold
                    },
                    new GrantApplicationDto()
                    {
                        ProjectName = "Holiday Abroad Funding",
                        ReferenceNo = "BG22CD",
                        EligibleAmount = 23400.00M,
                        RequestedAmount = 33500.00M,
                        Assignees = new List<GrantApplicationAssigneeDto>() { new GrantApplicationAssigneeDto() { UserId = Guid.NewGuid(), Username = "John Smith" } },
                        Probability = 60,
                        ProposalDate = new DateTime(2022, 10, 02),
                        SubmissionDate = new DateTime(2023, 01, 02),
                        Status = GrantApplicationStatus.Awaiting

                    }
                };
        }
    }

    public static class IQueryableExtensions
    {
        public static IQueryable<T> Sort<T>(this IQueryable<T> query, PagedAndSortedResultRequestDto input)
        {
            if (input is ISortedResultRequest sortInput)
            {
                if (!sortInput.Sorting.IsNullOrWhiteSpace())
                {
                    return query.OrderBy(input.Sorting);
                }
            }

            return query;
        }

        
    }
}



