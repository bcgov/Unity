using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Notifications.Templates 
{
    public interface ITemplateService : IApplicationService
    {
        Task<EmailTemplate?> CreateAsync(EmailTempateDto templateDto);
        Task<EmailTemplate?> UpdateTemplate(Guid id, EmailTempateDto templateDto);
        Task<List<EmailTemplate>> GetTemplatesByTenent();
        Task<EmailTemplate?> GetTemplateById(Guid id);
        Task DeleteTemplate(Guid id);
        Task<EmailTemplate?> GetTemplateByName(string name);

        Task<List<TemplateVariable>> GetTemplateVariables();
    }
}