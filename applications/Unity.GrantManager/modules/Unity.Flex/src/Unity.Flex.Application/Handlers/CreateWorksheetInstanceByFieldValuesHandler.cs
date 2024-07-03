using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Services;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.WorksheetInstances;
using Unity.Flex.Worksheets.Collectors;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Unity.Flex.Handlers
{
    public class CreateWorksheetInstanceByFieldValuesHandler(WorksheetsManager worksheetsManager, IServiceProvider serviceProvider) : ILocalEventHandler<CreateWorksheetInstanceByFieldValuesEto>, ITransientDependency
    {
        public async Task HandleEventAsync(CreateWorksheetInstanceByFieldValuesEto eventData)
        {
            List<(Worksheet worksheet, WorksheetInstance worksheetIntance)> workSheetInstancePairs = await worksheetsManager.CreateWorksheetDataByFields(eventData);

            foreach (var (worksheet, worksheetIntance) in workSheetInstancePairs.Where(s => s.worksheet.RequiresCollection()))
            {
                await worksheetIntance.CollectAsync(worksheet, serviceProvider);
            }
        }
    }
}
