using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.WorksheetLinks;
using Unity.Flex.WorksheetLinkInstance;
using Unity.Flex.WorksheetLinks;
using Volo.Abp.EventBus.Local;

namespace Unity.Flex.Worksheets
{
    [Authorize]
    public class WorksheetLinkAppService(
        ILocalEventBus localEventBus,
        IWorksheetLinkRepository worksheetLinkRepository,
        IWorksheetInstanceRepository worksheetInstanceRepository) : FlexAppService, IWorksheetLinkAppService
    {
        const string ORPHANED = "Orphaned";

        public virtual async Task<List<WorksheetLinkDto>> GetListByCorrelationAsync(Guid correlationId, string correlationProvider)
        {
            var worksheetLinks = await worksheetLinkRepository.GetListByCorrelationAsync(correlationId, correlationProvider);

            return ObjectMapper.Map<List<WorksheetLink>, List<WorksheetLinkDto>>(worksheetLinks);
        }

        public virtual async Task<List<WorksheetLinkDto>> GetListByWorksheetAsync(Guid worksheetId, string correlationProvider)
        {
            var worksheetLinks = await worksheetLinkRepository.GetListByWorksheetAsync(worksheetId, correlationProvider);

            return ObjectMapper.Map<List<WorksheetLink>, List<WorksheetLinkDto>>(worksheetLinks);
        }

        public async Task<List<WorksheetLinkDto>> UpdateWorksheetLinksAsync(UpdateWorksheetLinksDto dto)
        {
            var worksheetLinks = await worksheetLinkRepository.GetListByCorrelationAsync(dto.CorrelationId, dto.CorrelationProvider);
            var refreshedLinks = new List<WorksheetLinkDto>();
            
            await UpdateAndDeleteLinksAsync(worksheetLinkRepository, worksheetInstanceRepository, dto, worksheetLinks, refreshedLinks);
            await AddAndUnorphanLinksAsync(worksheetLinkRepository, worksheetInstanceRepository, dto, worksheetLinks, refreshedLinks);

            return refreshedLinks;
        }

        private static async Task AddAndUnorphanLinksAsync(IWorksheetLinkRepository worksheetLinkRepository, IWorksheetInstanceRepository worksheetInstanceRepository, UpdateWorksheetLinksDto dto, List<WorksheetLink> worksheetLinks, List<WorksheetLinkDto> refreshedLinks)
        {
            // Add new
            foreach (var wsAnchor in dto.WorksheetAnchors)
            {
                if (worksheetLinks.Find(s => s.CorrelationId == dto.CorrelationId
                    && s.CorrelationProvider == dto.CorrelationProvider
                        && s.WorksheetId == wsAnchor.Key) == null)
                {
                    var newLink = new WorksheetLink(Guid.NewGuid(), wsAnchor.Key, dto.CorrelationId, dto.CorrelationProvider, wsAnchor.Value);

                    var worksheetInstances = await worksheetInstanceRepository.GetByWorksheetCorrelationAsync(wsAnchor.Key, ORPHANED, dto.CorrelationId, dto.CorrelationProvider);
                    foreach (var worksheetInstance in worksheetInstances)
                    {
                        worksheetInstance.SetAnchor(wsAnchor.Value);
                    }

                    refreshedLinks.Add(MapWorksheetLink(newLink));
                    await worksheetLinkRepository.InsertAsync(newLink);
                }
            }
        }

        private async Task UpdateAndDeleteLinksAsync(IWorksheetLinkRepository worksheetLinkRepository, IWorksheetInstanceRepository worksheetInstanceRepository, UpdateWorksheetLinksDto dto, List<WorksheetLink> worksheetLinks, List<WorksheetLinkDto> refreshedLinks)
        {
            // Update or delete
            foreach (var link in worksheetLinks)
            {
                var worksheetInstances = await worksheetInstanceRepository.GetByWorksheetCorrelationAsync(link.WorksheetId, link.UiAnchor, dto.CorrelationId, dto.CorrelationProvider);

                if (dto.WorksheetAnchors.TryGetValue(link.WorksheetId, out string? value))
                {
                    if (link.UiAnchor != value)
                    {
                        foreach (var worksheetInstance in worksheetInstances)
                        {
                            worksheetInstance.SetAnchor(value);
                        }

                        link.SetAnchor(value);
                    }
                    refreshedLinks.Add(MapWorksheetLink(link));
                }
                else
                {
                    foreach (var worksheetInstance in worksheetInstances)
                    {
                        worksheetInstance.SetAnchor(ORPHANED);
                    }

                    await SendDeleteEvent(link);
                    await worksheetLinkRepository.DeleteAsync(link);
                }
            }
        }

        private async Task SendDeleteEvent(WorksheetLink link)
        {
            // Cleanup the mapping json for the worksheet that is deleted
            await localEventBus.PublishAsync(
                new WorksheetLinkEto
                {
                    Action = "DeleteWorksheetLink",
                    FormVersionId = link.CorrelationId,
                    Name = $"custom_{link.Worksheet.Name}"
                }
            );
        }

        private static WorksheetLinkDto MapWorksheetLink(WorksheetLink link)
        {
            return new WorksheetLinkDto()
            {
                Id = link.Id,
                CorrelationId = link.CorrelationId,
                CorrelationProvider = link.CorrelationProvider,
                UiAnchor = link.UiAnchor,
                WorksheetId = link.WorksheetId
            };
        }
    }
}
