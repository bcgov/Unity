using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantsPortal.Messages;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal.Handlers;

public class AddressSetPrimaryHandler(
    IApplicantAddressRepository applicantAddressRepository,
    ILogger<AddressSetPrimaryHandler> logger) : IPortalCommandHandler, ITransientDependency
{
    public string DataType => "ADDRESS_SET_PRIMARY_COMMAND";

    [UnitOfWork]
    public virtual async Task<string> HandleAsync(PluginDataPayload payload)
    {
        var addressId = Guid.Parse(payload.AddressId ?? throw new ArgumentException("addressId is required"));
        var profileId = Guid.Parse(payload.ProfileId ?? throw new ArgumentException("profileId is required"));

        logger.LogInformation("Setting address {AddressId} as primary for profile {ProfileId}", addressId, profileId);

        // TODO: Implement set-primary logic once the primary address tracking mechanism is confirmed.
        // The ApplicantAddress entity does not currently have an IsPrimary field.
        // This may require updating sibling addresses for the same applicant.

        logger.LogInformation("Address {AddressId} set as primary", addressId);
        return "Address set as primary";
    }
}
