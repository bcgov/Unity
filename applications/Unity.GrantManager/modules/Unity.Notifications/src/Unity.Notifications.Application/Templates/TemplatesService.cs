using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Unity.Notifications.Templates;


[Dependency(ReplaceServices = false)]
[ExposeServices(typeof(TemplateService), typeof(ITemplateService))]
public class TemplateService : ApplicationService, ITemplateService
{

    private readonly ITemplatesRepository _templatesRepository;
    private readonly ICurrentTenant _currentTenant;


    public TemplateService(
         ITemplatesRepository templatesRepository,
        ICurrentTenant currentTenant

        )
    {
        _templatesRepository = templatesRepository;
        _currentTenant = currentTenant;
    }

    public async Task<EmailTemplate?> CreateAsync(EmailTempateDto template)
    {
        // When being called here the current tenant is in context - verified by looking at the tenant id
        return await _templatesRepository.InsertAsync(
            new EmailTemplate(Guid.NewGuid(),
            "name-TBD",
            template.Description,
            template.Subject,
            template.BodyText,
            template.BodyHTML, "send-from"));
    }

    public async Task<EmailTemplate?> UpdateTemplate(Guid id, string name, string description, string subject, string bodyText, string? bodyHTML)
    {
        EmailTemplate template = await _templatesRepository.GetAsync(id);

        template.Description = description;
        template.Subject = subject;
        template.BodyText = bodyText;
        template.BodyHTML = bodyHTML != null ? bodyHTML : "";

        // When being called here the current tenant is in context - verified by looking at the tenant id
        EmailTemplate updatedTemplate = await _templatesRepository.UpdateAsync(template, autoSave: true);
        return template;
    }

    public async Task<List<EmailTemplate>> GetTemplatesByTenent()
    {
        var tenentId = _currentTenant.Id;
        return await _templatesRepository.GetByTenentIdAsync(tenentId);
    }
    public async Task<EmailTemplate?> GetTemplateById(Guid id)
    {
        return await _templatesRepository.GetAsync(id);
    }

    public async Task DeleteTemplate(Guid id)
    {
        await _templatesRepository.DeleteAsync(id);
    }
}