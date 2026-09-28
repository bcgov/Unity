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
            new() { TemplateType = TemplateTypes.Application, Name = "Applicant name", Token = "applicant_name", MapTo = "application.applicantName" },
            new() { TemplateType = TemplateTypes.Application, Name = "Registered Organization Name", Token = "organization_name", MapTo = "application.organizationName" },
            new() { TemplateType = TemplateTypes.Application, Name = "Applicant ID", Token = "applicant_id", MapTo = "application.unityApplicantId" },
            new() { TemplateType = TemplateTypes.Application, Name = "Today's Date", Token = "today_date", MapTo = "" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Applicant name", Token = "applicant_name", MapTo = "applicantName" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Registered Organization Name", Token = "organization_name", MapTo = "orgName" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Applicant ID", Token = "applicant_id", MapTo = "unityApplicantId" },
            new() { TemplateType = TemplateTypes.Applicant, Name = "Today's Date", Token = "today_date", MapTo = "" }
        };

        try
        {
            var allVariables = await templateVariablesRepository.GetListAsync();

            foreach (var template in emailTemplateVariableDtos)
            {
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

                    if (existingVariable.Name != template.Name)
                    {
                        existingVariable.Name = template.Name;
                        needsUpdate = true;
                    }

                    if (existingVariable.MapTo != template.MapTo)
                    {
                        existingVariable.MapTo = template.MapTo;
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
