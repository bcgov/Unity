﻿using System.Threading.Tasks;
using System;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Worksheets
{
    public interface IWorksheetSectionAppService : IApplicationService
    {
        Task<CustomFieldDto> CreateCustomFieldAsync(Guid id, CreateCustomFieldDto dto);
        Task<WorksheetSectionDto> GetAsync(Guid id);
        Task<WorksheetSectionDto> EditAsync(Guid id, EditSectionDto dto);
    }
}