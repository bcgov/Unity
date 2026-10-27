using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Notifications.Permissions;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.Notifications.Templates;


[Dependency(ReplaceServices = false)]
[ExposeServices(typeof(TemplateService), typeof(ITemplateService))]
public class TemplateService(
     ITemplatesRepository templatesRepository,
     ICurrentTenant currentTenant,
     ITemplateVariablesRepository templateVariablesRepository,
     IUnitOfWorkManager unitOfWorkManager,
     Volo.Abp.Domain.Repositories.IRepository<GrantManager.Notifications.ScheduledNotification, Guid> scheduledNotificationsRepository

        ) : ApplicationService, ITemplateService
{

    private readonly ITemplatesRepository _templatesRepository = templatesRepository;
    private readonly ICurrentTenant _currentTenant = currentTenant;
    private readonly ITemplateVariablesRepository _templateVariablesRepository = templateVariablesRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager = unitOfWorkManager;
    private readonly Volo.Abp.Domain.Repositories.IRepository<GrantManager.Notifications.ScheduledNotification, Guid> _scheduledNotificationsRepository = scheduledNotificationsRepository;

    [Authorize(NotificationsPermissions.Email.Template.Default)]
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

    [Authorize(NotificationsPermissions.Email.Template.Default)]
    public async Task<EmailTemplate?> UpdateTemplate(Guid id, EmailTempateDto templateDto)
    {

        EmailTemplate template = await _templatesRepository.GetAsync(id);

        template.Description = templateDto.Description;
        template.Subject = templateDto.Subject;
        template.BodyText = templateDto.BodyText;
        template.BodyHTML = templateDto.BodyHTML ?? "";
        template.Name = templateDto.Name;
        template.SendFrom = templateDto.SendFrom;
        template.RecipientCategory = templateDto.RecipientCategory;
        template.RecipientIdentifier = templateDto.RecipientIdentifier;
        template.TemplateType = NormalizeTemplateType(templateDto.TemplateType);

        // When being called here the current tenant is in context - verified by looking at the tenant id
        EmailTemplate updatedTemplate = await _templatesRepository.UpdateAsync(template, autoSave: true);
        return updatedTemplate;
    }

    [Authorize(NotificationsPermissions.Email.Default)]
    public async Task<List<EmailTemplate>> GetTemplatesByTenant()
    {
        var tenentId = _currentTenant.Id;
        return await _templatesRepository.GetByTenentIdAsync(tenentId);
    }

    // Shared lookup endpoint: accessible to anyone with email/notification access (lookup, schedule, send)
    [Authorize(NotificationsPermissions.Email.Default)]
    public async Task<EmailTemplate?> GetTemplateById(Guid id)
    {
        return await _templatesRepository.GetAsync(id);
    }

    [Authorize(NotificationsPermissions.Email.Template.Default)]
    public async Task DeleteTemplate(Guid id)
    {
        using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
        var template = await _templatesRepository.LockActiveAsync(id)
            ?? throw new Volo.Abp.Domain.Entities.EntityNotFoundException(typeof(EmailTemplate), id);

        var scheduledQuery = await _scheduledNotificationsRepository.GetQueryableAsync();
        var inUse = await scheduledQuery.AnyAsync(x => x.EmailTemplateId == id);
        if (inUse)
        {
            throw new Volo.Abp.BusinessException("Notifications:TemplateInUse");
        }

        await _templatesRepository.DeleteAsync(template);
        await uow.CompleteAsync();
    }
    [Authorize(NotificationsPermissions.Email.Default)]
    public async Task<EmailTemplate?> GetTemplateByName(string name)
    {
        var data =  await _templatesRepository.GetByNameAsync(name);
        return data;
    }

    // Shared lookup endpoint: accessible to anyone with email/notification access
    [Authorize(NotificationsPermissions.Email.Default)]
    public Task<List<string>> GetTemplateTypes()
    {
        return Task.FromResult(new List<string>
        {
            TemplateTypes.Application,
            TemplateTypes.Applicant
        });
    }

    // Shared lookup endpoint: accessible to anyone with email/notification access (forms, templates, scheduling)
    [Authorize(NotificationsPermissions.Email.Default)]
    public async Task<List<TemplateVariable>> GetTemplateVariables(string? templateType = null)
    {
        var selectedType = NormalizeTemplateType(templateType);
        var templateVariables = await _templateVariablesRepository.GetQueryableAsync();
        return [.. templateVariables
            .Where(x => x.TemplateType == selectedType)
            .OrderBy(x => x.Name)];
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
