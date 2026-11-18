using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Notifications.Templates 
{
    public interface ITemplateService : IApplicationService
    {
        Task<EmailTemplate?> CreateAsync(EmailTemplateDto templateDto);
        Task<EmailTemplate?> UpdateTemplate(Guid id, EmailTemplateDto templateDto);
        Task<List<EmailTemplate>> GetTemplatesByTenant();
        Task<EmailTemplate?> GetTemplateById(Guid id);
        Task DeleteTemplate(Guid id);
        Task<EmailTemplate?> GetTemplateByName(string name);

        Task<List<string>> GetTemplateTypes();
        Task<List<TemplateVariable>> GetTemplateVariables(string? templateType = null);
    }
}