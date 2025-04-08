using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Notifications.Templates 
{
    public interface ITemplateService : IApplicationService
    {
        Task<EmailTemplate?> CreateAsync(EmailTempateDto dto);
        Task<EmailTemplate?> UpdateTemplate(Guid id, EmailTempateDto template);
        Task<List<EmailTemplate>> GetTemplatesByTenent();
        Task<EmailTemplate?> GetTemplateById(Guid id);
        Task DeleteTemplate(Guid id);
    }
}