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
        var templateType = NormalizeTemplateType(templateDto.TemplateType);

        // When being called here the current tenant is in context - verified by looking at the tenant id
        return await _templatesRepository.InsertAsync(
            new EmailTemplate(Guid.NewGuid(),
            templateDto.Name,
            templateDto.Description,
            templateDto.Subject,
            templateDto.BodyText,
            templateDto.BodyHTML,
            templateDto.SendFrom,
            templateDto.RecipientCategory,
            templateDto.RecipientIdentifier,
            templateType));
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
        template.RecipientCategory = templateDto.RecipientCategory;
        template.RecipientIdentifier = templateDto.RecipientIdentifier;
        template.TemplateType = NormalizeTemplateType(templateDto.TemplateType);

        // When being called here the current tenant is in context - verified by looking at the tenant id
        EmailTemplate updatedTemplate = await _templatesRepository.UpdateAsync(template, autoSave: true);
        return updatedTemplate;
    }

    public async Task<List<EmailTemplate>> GetTemplatesByTenant()
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
    
    public Task<List<string>> GetTemplateTypes()
    {
        return Task.FromResult(new List<string>
        {
            TemplateTypes.Application,
            TemplateTypes.Applicant
        });
    }

    public async Task<List<TemplateVariable>> GetTemplateVariables(string? templateType = null)
    {
        var selectedType = NormalizeTemplateType(templateType);
        var templateVariables = await _templateVariablesRepository.GetQueryableAsync();
        return templateVariables
            .Where(x => x.TemplateType == selectedType)
            .OrderBy(x => x.Name)
            .ToList();
    }

    private static string NormalizeTemplateType(string? templateType)
    {
        if (string.Equals(templateType, TemplateTypes.Applicant, StringComparison.OrdinalIgnoreCase))
        {
            return TemplateTypes.Applicant;
        }

        if (string.Equals(templateType, TemplateTypes.Application, StringComparison.OrdinalIgnoreCase))
        {
            return TemplateTypes.Application;
        }

        return TemplateTypes.Application;
    }
}
