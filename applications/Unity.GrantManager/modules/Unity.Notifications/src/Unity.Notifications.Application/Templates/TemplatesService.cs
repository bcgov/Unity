using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly ITemplateVariablesRepository _templateVariablesRepository;


    public TemplateService(
         ITemplatesRepository templatesRepository,
        ICurrentTenant currentTenant,
         ITemplateVariablesRepository templateVariablesRepository

        )
    {
        _templatesRepository = templatesRepository;
        _currentTenant = currentTenant;
        _templateVariablesRepository = templateVariablesRepository;
    }

    public async Task<EmailTemplate?> CreateAsync(EmailTempateDto templateDto)
    {
        // When being called here the current tenant is in context - verified by looking at the tenant id
        return await _templatesRepository.InsertAsync(
            new EmailTemplate(Guid.NewGuid(),
            templateDto.Name,
            templateDto.Description,
            templateDto.Subject,
            templateDto.BodyText,
            templateDto.BodyHTML, templateDto.SendFrom));
    }

    public async Task<EmailTemplate?> UpdateTemplate(Guid id, EmailTempateDto templateDto)
    {
        
        EmailTemplate template = await _templatesRepository.GetAsync(id);

        template.Description = templateDto.Description;
        template.Subject = templateDto.Subject;
        template.BodyText = templateDto.BodyText;
        template.BodyHTML = templateDto.BodyHTML != null ? templateDto.BodyHTML : "";
        template.Name = templateDto.Name;
        template.SendFrom = templateDto.SendFrom;

        // When being called here the current tenant is in context - verified by looking at the tenant id
        EmailTemplate updatedTemplate = await _templatesRepository.UpdateAsync(template, autoSave: true);
        return updatedTemplate;
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
    public async Task<EmailTemplate?> GetTemplateByName(string name)
    {
        var data =  await _templatesRepository.GetByNameAsync(name);
        return data;
    } 
    
    public async Task<List<TemplateVariable>> GetTemplateVariables()
    {
        var templateVaraibles =  await _templateVariablesRepository.GetQueryableAsync();
        return templateVaraibles.OrderBy(x => x.Name).ToList();
    }
}