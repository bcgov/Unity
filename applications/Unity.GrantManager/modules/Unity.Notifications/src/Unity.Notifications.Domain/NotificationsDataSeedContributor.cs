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
            new() { Name = "Applicant name", Token = "applicant_name", MapTo = "applicant.applicantName" },
            new() { Name = "Submission #", Token = "submission_number", MapTo = "referenceNo" },
            new() { Name = "Submission Date", Token = "submission_date", MapTo = "submissionDate" },
            new() { Name = "Category", Token = "category", MapTo = "applicationForm.category" },
            new() { Name = "Status", Token = "status", MapTo = "status" },
            new() { Name = "Approved Amount", Token = "approved_amount", MapTo = "approvedAmount" },
            new() { Name = "Approval date", Token = "approval_date", MapTo = "finalDecisionDate" },
            new() { Name = "Community", Token = "community", MapTo = "community" },
            new() { Name = "Contact Full Name", Token = "contact_full_name", MapTo = "contactFullName" },
            new() { Name = "Contact Title", Token = "contact_title", MapTo = "contactTitle" },
            new() { Name = "Decline Rationale", Token = "decline_rationale", MapTo = "declineRational" },
            new() { Name = "Registered Organization Name", Token = "organization_name", MapTo = "organizationName" },
            new() { Name = "Project Start Date", Token = "project_start_date", MapTo = "projectStartDate" },
            new() { Name = "Project End Date", Token = "project_end_date", MapTo = "projectEndDate" },
            new() { Name = "Project Name", Token = "project_name", MapTo = "projectName" },
            new() { Name = "Project Summary", Token = "project_summary", MapTo = "projectSummary" },
            new() { Name = "Signing Authority Full Name", Token = "signing_authority_full_name", MapTo = "signingAuthorityFullName" },
            new() { Name = "Signing Authority Title", Token = "signing_authority_title", MapTo = "signingAuthorityTitle" },
            new() { Name = "Applicant ID", Token = "applicant_id", MapTo = "applicant.unityApplicantId" },
            new() { Name = "Requested Amount", Token = "requested_amount", MapTo = "requestedAmount" },
            new() { Name = "Recommended Amount", Token = "recommended_amount", MapTo = "recommendedAmount" },
            new() { Name = "Unity Application ID", Token = "unity_application_id", MapTo = "unityApplicationId" },
            new() { Name = "Today's Date", Token = "today_date", MapTo = "" }
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
            new() {Name = "FSB-AP", Description = "This group manages the recipients for PO-related payments, which will be sent to FSB-AP to update contracts and initiate payment creation.",Type = "static"},
            new() {Name = "Payments", Description = "This group manages the recipients for payment notifications, such as failures or errors",Type = "static"}
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
