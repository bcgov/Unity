using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Domain.Services;
using Unity.Flex.Reporting.DataGenerators;
using Unity.Flex.Scoresheets;
using Unity.Flex.Scoresheets.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.Validation;

namespace Unity.Flex.Handlers
{
    public class PersistScoresheetSectionInstanceHandler(ScoresheetsManager scoresheetsManager) : ILocalEventHandler<PersistScoresheetSectionInstanceEto>, ITransientDependency
    {
        public async Task HandleEventAsync(PersistScoresheetSectionInstanceEto eventData)
        {

            await scoresheetsManager.PersistScoresheetData(eventData);
        }
    }
}
