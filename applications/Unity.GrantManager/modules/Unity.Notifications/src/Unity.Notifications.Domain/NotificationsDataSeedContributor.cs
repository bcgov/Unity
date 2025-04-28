﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Notifications.Templates;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;

namespace Unity.Notifications;

public class NotificationsDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly ITemplateVariablesRepository _templateVariablesRepository;

    public NotificationsDataSeedContributor(ITemplateVariablesRepository templateVariablesRepository)
    {

        _templateVariablesRepository = templateVariablesRepository;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        List<TemplateVariable> variableList = await _templateVariablesRepository.GetListAsync();
        if (variableList.Count > 0)
        {
            return; // already seeded
        }
        var EmailTempateVariableDtos = new List<EmailTempateVariableDto>
        {
            new EmailTempateVariableDto { Name = "Applicant name", Token = "applicant_name", MapTo = "applicant.applicantName" },
            new EmailTempateVariableDto { Name = "Submission #", Token = "submission_number", MapTo = "referenceNo" },
            new EmailTempateVariableDto { Name = "Submission Date", Token = "submission_date", MapTo = "submissionDate" },
            new EmailTempateVariableDto { Name = "Category", Token = "category", MapTo = "category" },
            new EmailTempateVariableDto { Name = "Status", Token = "status", MapTo = "status" },
            new EmailTempateVariableDto { Name = "Approved Amount", Token = "approved_amount", MapTo = "approvedAmount" },
            new EmailTempateVariableDto { Name = "Approval date", Token = "approval_date", MapTo = "finalDecisionDate" },
            new EmailTempateVariableDto { Name = "Community", Token = "community", MapTo = "community" },
            new EmailTempateVariableDto { Name = "Contact Full Name", Token = "contact_full_name", MapTo = "contactFullName" },
            new EmailTempateVariableDto { Name = "Contact Title", Token = "contact_title", MapTo = "contactTitle" },
            new EmailTempateVariableDto { Name = "Decline Rationale", Token = "decline_rationale", MapTo = "declineRational" },
            new EmailTempateVariableDto { Name = "Registered Organization Name", Token = "organization_name", MapTo = "organizationName" },
            new EmailTempateVariableDto { Name = "Project Start Date", Token = "project_start_date", MapTo = "projectStartDate" },
            new EmailTempateVariableDto { Name = "Project End Date", Token = "project_end_date", MapTo = "projectEndDate" },
            new EmailTempateVariableDto { Name = "Project Name", Token = "project_name", MapTo = "projectName" },
            new EmailTempateVariableDto { Name = "Project Summary", Token = "project_summary", MapTo = "projectSummary" },
            new EmailTempateVariableDto { Name = "Signing Authority Full Name", Token = "signing_authority_full_name", MapTo = "signingAuthorityFullName" },
            new EmailTempateVariableDto { Name = "Signing Authority Title", Token = "signing_authority_title", MapTo = "signingAuthorityTitle" }
        };

        if (context.TenantId != null) // only try seed into a tenant database
        {
            foreach (var template in EmailTempateVariableDtos)
            {
                await _templateVariablesRepository.InsertAsync(new TemplateVariable { Name = template.Name, Token = template.Token, MapTo = template.MapTo }, autoSave: true);
            }
        }

    }
}

internal class EmailTempateVariableDto
{
    public string Name { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string MapTo { get; set; } = string.Empty;
}