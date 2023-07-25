using System;
using System.Collections.Generic;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications
{
    public class GrantApplicationAppService : ApplicationService, IGrantApplicationAppService
    {
        public GrantApplicationAppService()
        {
        }

        public GrantApplicationsDto GetList()
        {
            // Using this as mock data for now until the data model is fleshed out
            return new GrantApplicationsDto()
            {
                Draw = 10,
                RecordsFiltered = 2,
                RecordsTotal = 2,
                Data = new List<GrantApplicationDto>()
                {
                    new GrantApplicationDto()
                    {
                        Name = "Application One",
                        CreationTime = DateTime.UtcNow,
                        CreatorId = Guid.NewGuid(),
                        Id = Guid.NewGuid(),
                        LastModificationTime = DateTime.UtcNow,
                        LastModifierId = Guid.NewGuid()
                    },
                    new GrantApplicationDto()
                    {
                        Name = "Application Two",
                        CreationTime = DateTime.UtcNow,
                        CreatorId = Guid.NewGuid(),
                        Id = Guid.NewGuid(),
                        LastModificationTime = DateTime.UtcNow,
                        LastModifierId = Guid.NewGuid()
                    }
                }
            };
        }
    }
}


