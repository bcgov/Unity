using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Notifications.EmailGroups;
using Unity.Notifications.Templates;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;


namespace Unity.Notifications;

public class NotificationsDataSeedContributor(ITemplateVariablesRepository templateVariablesRepository,
                                              IEmailGroupsRepository emailGroupsRepository) : IDataSeedContributor, ITransientDependency
{

    public async Task SeedAsync(DataSeedContext context)
    {
        if (context.TenantId == null) // only seed into a tenant database
        {
            return;
        }

        var emailTemplateVariableDtos = new List<EmailTempateVariableDto>
        {
            new EmailTempateVariableDto { Name = "Applicant name", Token = "applicant_name", MapTo = "applicant.applicantName" },
            new EmailTempateVariableDto { Name = "Submission #", Token = "submission_number", MapTo = "referenceNo" },
            new EmailTempateVariableDto { Name = "Submission Date", Token = "submission_date", MapTo = "submissionDate" },
            new EmailTempateVariableDto { Name = "Category", Token = "category", MapTo = "applicationForm.category" },
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
            new EmailTempateVariableDto { Name = "Signing Authority Title", Token = "signing_authority_title", MapTo = "signingAuthorityTitle" },
            new EmailTempateVariableDto { Name = "Applicant ID", Token = "applicant_id", MapTo = "applicant.unityApplicantId" },
            new EmailTempateVariableDto { Name = "Requested Amount", Token = "requested_amount", MapTo = "requestedAmount" }
        };

        try
        {
            foreach (var template in emailTemplateVariableDtos)
            {
                var allVariables = await templateVariablesRepository.GetListAsync();
                var existingVariable = allVariables.FirstOrDefault(tv => tv.Token == template.Token);
                if (existingVariable == null)
                {
                    await templateVariablesRepository.InsertAsync(
                        new TemplateVariable { Name = template.Name, Token = template.Token, MapTo = template.MapTo },
                        autoSave: true
                    );
                }
                else if (existingVariable.Token == "category" && existingVariable.MapTo == "category")
                {
                    existingVariable.MapTo = "applicationForm.category";
                    await templateVariablesRepository.UpdateAsync(existingVariable, autoSave: true);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error seeding Notifications Data for Templates: {ex.Message}");
        }

        var emailGroups = new List<EmailGroupDto>
        {
            new EmailGroupDto  {Name = "FSB-AP", Description = "This group manages the recipients for PO-related payments, which will be sent to FSB-AP to update contracts and initiate payment creation.",Type = "static"},
            new EmailGroupDto  {Name = "Payments", Description = "This group manages the recipients for payment notifications, such as failures or errors",Type = "dynamic"}
        };
        try
        {
            var allGroups = await emailGroupsRepository.GetListAsync();
            foreach (var emailGroup in emailGroups)
            {
                var existingGroup = allGroups.FirstOrDefault(g => g.Name == emailGroup.Name);
                if (existingGroup == null)
                {
                    await emailGroupsRepository.InsertAsync(
                        new EmailGroup { Name = emailGroup.Name, Description = emailGroup.Description, Type = emailGroup.Type },
                        autoSave: true
                    );
                }
            }

        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error seeding Notifications Data for Email Groups: {ex.Message}");
        }
    }

    internal class EmailGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
    internal class EmailTempateVariableDto
    {
        public string Name { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string MapTo { get; set; } = string.Empty;
    }
}