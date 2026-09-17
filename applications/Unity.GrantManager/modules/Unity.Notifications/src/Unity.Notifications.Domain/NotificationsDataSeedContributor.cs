using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Notifications.EmailAddresses;
using Unity.Notifications.EmailGroups;
using Unity.Notifications.Settings;
using Unity.Notifications.Templates;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Settings;


namespace Unity.Notifications;

public class NotificationsDataSeedContributor(ITemplateVariablesRepository templateVariablesRepository,
                                              IEmailGroupsRepository emailGroupsRepository,
                                              IEmailAddressConfigurationsRepository emailAddressConfigurationsRepository,
                                              ISettingProvider settingProvider) : IDataSeedContributor, ITransientDependency
{

    public async Task SeedAsync(DataSeedContext context)
    {
        if (context.TenantId == null) // only seed into a tenant database
        {
            return;
        }

        var emailTemplateVariableDtos = new List<EmailTempateVariableDto>
        {
            new() { TemplateType = TemplateTypes.Application, Name = "Applicant name", Token = "applicant_name", MapTo = "applicant.applicantName" },
            new() { TemplateType = TemplateTypes.Application, Name = "Submission #", Token = "submission_number", MapTo = "referenceNo" },
            new() { TemplateType = TemplateTypes.Application, Name = "Submission Date", Token = "submission_date", MapTo = "submissionDate" },
            new() { TemplateType = TemplateTypes.Application, Name = "Category", Token = "category", MapTo = "applicationForm.category" },
            new() { TemplateType = TemplateTypes.Application, Name = "Status", Token = "status", MapTo = "status" },
            new() { TemplateType = TemplateTypes.Application, Name = "Approved Amount", Token = "approved_amount", MapTo = "approvedAmount" },
            new() { TemplateType = TemplateTypes.Application, Name = "Approval date", Token = "approval_date", MapTo = "finalDecisionDate" },
            new() { TemplateType = TemplateTypes.Application, Name = "Community", Token = "community", MapTo = "community" },
            new() { TemplateType = TemplateTypes.Application, Name = "Contact Full Name", Token = "contact_full_name", MapTo = "contactFullName" },
            new() { TemplateType = TemplateTypes.Application, Name = "Contact Title", Token = "contact_title", MapTo = "contactTitle" },
            new() { TemplateType = TemplateTypes.Application, Name = "Decline Rationale", Token = "decline_rationale", MapTo = "declineRational" },
            new() { TemplateType = TemplateTypes.Application, Name = "Registered Organization Name", Token = "organization_name", MapTo = "organizationName" },
            new() { TemplateType = TemplateTypes.Application, Name = "Project Start Date", Token = "project_start_date", MapTo = "projectStartDate" },
            new() { TemplateType = TemplateTypes.Application, Name = "Project End Date", Token = "project_end_date", MapTo = "projectEndDate" },
            new() { TemplateType = TemplateTypes.Application, Name = "Fiscal Year End", Token = "fiscal_year_end", MapTo = "applicant.fiscalYearEnd" },
            new() { TemplateType = TemplateTypes.Application, Name = "Project Name", Token = "project_name", MapTo = "projectName" },
            new() { TemplateType = TemplateTypes.Application, Name = "Project Summary", Token = "project_summary", MapTo = "projectSummary" },
            new() { TemplateType = TemplateTypes.Application, Name = "Signing Authority Full Name", Token = "signing_authority_full_name", MapTo = "signingAuthorityFullName" },
            new() { TemplateType = TemplateTypes.Application, Name = "Signing Authority Title", Token = "signing_authority_title", MapTo = "signingAuthorityTitle" },
            new() { TemplateType = TemplateTypes.Application, Name = "Applicant ID", Token = "applicant_id", MapTo = "applicant.unityApplicantId" },
            new() { TemplateType = TemplateTypes.Application, Name = "Requested Amount", Token = "requested_amount", MapTo = "requestedAmount" },
            new() { TemplateType = TemplateTypes.Application, Name = "Recommended Amount", Token = "recommended_amount", MapTo = "recommendedAmount" },
            new() { TemplateType = TemplateTypes.Application, Name = "Unity Application ID", Token = "unity_application_id", MapTo = "unityApplicationId" },
            new() { TemplateType = TemplateTypes.Application, Name = "Today's Date", Token = "today_date", MapTo = "" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Applicant name", Token = "applicant_name", MapTo = "applicantName" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Applicant ID", Token = "applicant_id", MapTo = "unityApplicantId" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Registered Organization Name", Token = "organization_name", MapTo = "orgName" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Non-registered Business Name", Token = "non_registered_business_name", MapTo = "nonRegisteredBusinessName" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Organization Number", Token = "organization_number", MapTo = "orgNumber" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Business Number", Token = "business_number", MapTo = "businessNumber" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Organization Status", Token = "organization_status", MapTo = "orgStatus" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Organization Type", Token = "organization_type", MapTo = "organizationType" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Applicant Status", Token = "applicant_status", MapTo = "status" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Sector", Token = "sector", MapTo = "sector" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Sub-sector", Token = "sub_sector", MapTo = "subSector" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Industry Description", Token = "industry_description", MapTo = "sectorSubSectorIndustryDesc" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Approximate Number of Employees", Token = "approximate_number_of_employees", MapTo = "approxNumberOfEmployees" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Indigenous Organization", Token = "indigenous_organization", MapTo = "indigenousOrgInd" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Fiscal Month", Token = "fiscal_month", MapTo = "fiscalMonth" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Fiscal Day", Token = "fiscal_day", MapTo = "fiscalDay" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Fiscal Year End", Token = "fiscal_year_end", MapTo = "fiscalYearEnd" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Started Operating Date", Token = "started_operating_date", MapTo = "startedOperatingDate" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Today's Date", Token = "today_date", MapTo = "" }
        };

        try
        {
            foreach (var template in emailTemplateVariableDtos)
            {
                var allVariables = await templateVariablesRepository.GetListAsync();
                var existingVariable = allVariables.FirstOrDefault(tv =>
                    tv.Token == template.Token &&
                    (tv.TemplateType == template.TemplateType ||
                     (template.TemplateType == TemplateTypes.Application && string.IsNullOrWhiteSpace(tv.TemplateType))));
                if (existingVariable == null)
                {
                    await templateVariablesRepository.InsertAsync(
                        new TemplateVariable { Name = template.Name, Token = template.Token, MapTo = template.MapTo, TemplateType = template.TemplateType },
                        autoSave: true
                    );
                }
                else
                {
                    var needsUpdate = false;
                    if (string.IsNullOrWhiteSpace(existingVariable.TemplateType))
                    {
                        existingVariable.TemplateType = template.TemplateType;
                        needsUpdate = true;
                    }

                    if (existingVariable.TemplateType == TemplateTypes.Application &&
                        existingVariable.Token == "category" &&
                        existingVariable.MapTo == "category")
                    {
                        existingVariable.MapTo = "applicationForm.category";
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                    {
                        await templateVariablesRepository.UpdateAsync(existingVariable, autoSave: true);
                    }
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

        var defaultFromAddress = await settingProvider.GetOrNullAsync(
            NotificationsSettings.Mailing.DefaultFromAddress);
        if (!string.IsNullOrWhiteSpace(defaultFromAddress))
        {
            var normalizedDefaultFromAddress = defaultFromAddress.Trim();
            var existingSenderAddress = await emailAddressConfigurationsRepository.GetListAsync(configuration =>
                configuration.EmailType == "Sender" &&
                configuration.EmailAddress.ToUpper() == normalizedDefaultFromAddress.ToUpper());
            var existingDefaultConfigurations = await emailAddressConfigurationsRepository.GetListAsync(
                configuration => configuration.IsDefault);

            if (existingSenderAddress.Count == 0)
            {
                foreach (var existingDefaultConfiguration in existingDefaultConfigurations)
                {
                    existingDefaultConfiguration.IsDefault = false;
                    await emailAddressConfigurationsRepository.UpdateAsync(existingDefaultConfiguration, autoSave: true);
                }

                await emailAddressConfigurationsRepository.InsertAsync(
                    new EmailAddressConfiguration(
                        Guid.NewGuid(),
                        normalizedDefaultFromAddress,
                        "Sender",
                        "Default sender address",
                        isDefault: true),
                    autoSave: true);
            }
            else if (!existingSenderAddress.Any(configuration => configuration.IsDefault))
            {
                foreach (var existingDefaultConfiguration in existingDefaultConfigurations)
                {
                    existingDefaultConfiguration.IsDefault = false;
                    await emailAddressConfigurationsRepository.UpdateAsync(existingDefaultConfiguration, autoSave: true);
                }

                var copiedConfiguration = existingSenderAddress.First();
                copiedConfiguration.IsActive = true;
                copiedConfiguration.IsDefault = true;
                await emailAddressConfigurationsRepository.UpdateAsync(copiedConfiguration, autoSave: true);
            }
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
        public string TemplateType { get; set; } = TemplateTypes.Application;
    }
}
