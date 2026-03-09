using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantsPortal.Messages;
using Unity.GrantManager.GrantsPortal.Messages.Commands;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal.Handlers;

public class OrganizationEditHandler(
    IApplicantRepository applicantRepository,
    ILogger<OrganizationEditHandler> logger) : IPortalCommandHandler, ITransientDependency
{
    public string DataType => "ORGANIZATION_EDIT_COMMAND";

    [UnitOfWork]
    public virtual async Task<string> HandleAsync(PluginDataPayload payload)
    {
        var innerData = payload.Data?.ToObject<OrganizationEditData>()
                        ?? throw new ArgumentException("Organization data is required");

        logger.LogInformation("Editing organization for profile {ProfileId}", payload.ProfileId);

        // TODO: Determine the correct lookup strategy for the Applicant entity.
        // For now, use organizationId from the payload as a direct Applicant ID.
        var organizationId = Guid.Parse(payload.OrganizationId ?? throw new ArgumentException("organizationId is required"));
        var applicant = await applicantRepository.GetAsync(organizationId);

        applicant.OrgName = innerData.Name;
        applicant.OrganizationType = innerData.OrganizationType;
        applicant.OrgNumber = innerData.OrganizationNumber;
        applicant.OrgStatus = innerData.Status;
        applicant.NonRegOrgName = innerData.NonRegOrgName;
        applicant.FiscalMonth = innerData.FiscalMonth;
        applicant.OrganizationSize = innerData.OrganizationSize;

        if (int.TryParse(innerData.FiscalDay, out var fiscalDay))
        {
            applicant.FiscalDay = fiscalDay;
        }

        await applicantRepository.UpdateAsync(applicant, autoSave: true);

        logger.LogInformation("Organization {OrganizationId} updated successfully", organizationId);
        return "Organization updated successfully";
    }
}
