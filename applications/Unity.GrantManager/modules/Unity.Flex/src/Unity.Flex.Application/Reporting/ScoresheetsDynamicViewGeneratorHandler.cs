﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.Flex.Reporting
{
    public class ScoresheetsDynamicViewGeneratorHandler(
            IScoresheetRepository scoresheetRepository,
            ICurrentTenant currentTenant,
            IUnitOfWorkManager unitOfWorkManager,
            ILogger<ScoresheetsDynamicViewGeneratorHandler> logger) : ILocalEventHandler<ScoresheetsDynamicViewGeneratorEto>, ITransientDependency
    {
        public async Task HandleEventAsync(ScoresheetsDynamicViewGeneratorEto viewGenerationEvent)
        {
            try
            {
                using (currentTenant.Change(viewGenerationEvent.TenantId))
                {
                    using var uow = unitOfWorkManager.Begin(isTransactional: false);

                    var worksheet = await scoresheetRepository.GetAsync(viewGenerationEvent.ScoresheetId);

                    if (worksheet != null)
                    {
                        var dbContext = await scoresheetRepository.GetDbContextAsync();
                        FormattableString sql = $@"CALL generate_scoresheets_view({viewGenerationEvent.ScoresheetId});";
                        await dbContext.Database.ExecuteSqlAsync(sql);
                    }

                    await uow.CompleteAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{ErrorMessage}", ex.Message);
            }
        }
    }
}
