using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.WorksheetLinks;
using Unity.Flex.WorksheetLinks;
using Volo.Abp;

namespace Unity.Flex.Worksheets
{
    [Authorize]
    public class WorksheetLinkAppService(IWorksheetLinkRepository worksheetLinkRepository,
        IWorksheetInstanceRepository worksheetInstanceRepository) : FlexAppService, IWorksheetLinkAppService
    {
        public virtual async Task<WorksheetLinkDto> CreateAsync(CreateWorksheetLinkDto dto)
        {
            var existing = await worksheetLinkRepository.GetExistingLinkAsync(dto.WorksheetId, dto.CorrelationId, dto.CorrelationProvider);

            if (existing != null)
            {
                throw new UserFriendlyException("Link already exists, use versioning to update links");
            }

            var worksheetLink = await worksheetLinkRepository.InsertAsync(new WorksheetLink(Guid.NewGuid(), dto.WorksheetId, dto.CorrelationId, dto.CorrelationProvider, dto.UiAnchor));

            return ObjectMapper.Map<WorksheetLink, WorksheetLinkDto>(worksheetLink);
        }

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

        public async Task<List<WorksheetLinkDto>> UpdateWorksheetLinksAsync(Guid correlationId, string correlationProvider, UpdateWorksheetLinksDto dto)
        {
            var worksheetLinks = await worksheetLinkRepository.GetListByCorrelationAsync(correlationId, correlationProvider);
            var refreshedLinks = new List<WorksheetLinkDto>();

            // Update or delete
            foreach (var link in worksheetLinks)
            {
                if (dto.WorksheetAnchors.TryGetValue(link.WorksheetId, out string? value))
                {
                    if (link.UiAnchor != value)
                    {
                        var worksheetInstances = await worksheetInstanceRepository.GetByWorksheetAnchorAsync(link.WorksheetId, link.UiAnchor);
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
                    await worksheetLinkRepository.DeleteAsync(link);
                }
            }

            // Add new
            foreach (var wsAnchor in dto.WorksheetAnchors)
            {
                if (worksheetLinks.Find(s => s.CorrelationId == correlationId
                    && s.CorrelationProvider == correlationProvider
                        && s.WorksheetId == wsAnchor.Key) == null)
                {
                    var newLink = new WorksheetLink(Guid.NewGuid(), wsAnchor.Key, correlationId, correlationProvider, wsAnchor.Value);
                    refreshedLinks.Add(MapWorksheetLink(newLink));
                    await worksheetLinkRepository.InsertAsync(newLink);
                }
            }

            return refreshedLinks;
        }

        private WorksheetLinkDto MapWorksheetLink(WorksheetLink link)
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
