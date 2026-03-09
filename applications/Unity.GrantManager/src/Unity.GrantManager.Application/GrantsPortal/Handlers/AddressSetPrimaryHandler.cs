using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantsPortal.Messages;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal.Handlers;

public class AddressSetPrimaryHandler(
    IApplicantAddressRepository applicantAddressRepository,
    ILogger<AddressSetPrimaryHandler> logger) 
    : IPortalCommandHandler, ITransientDependency
{
    public string DataType => "ADDRESS_SET_PRIMARY_COMMAND";

    [UnitOfWork]
    public virtual async Task<string> HandleAsync(PluginDataPayload payload)
    {
        var addressId = Guid.Parse(payload.AddressId ?? throw new ArgumentException("addressId is required"));
        var profileId = Guid.Parse(payload.ProfileId ?? throw new ArgumentException("profileId is required"));

        logger.LogInformation("Setting address {AddressId} as primary for profile {ProfileId}", addressId, profileId);

        var address = await applicantAddressRepository.GetAsync(addressId);

        address.SetProperty("profileId", profileId.ToString());
        address.SetProperty("isPrimary", true);

        if (address.ApplicantId.HasValue)
        {
            var siblingAddresses = await applicantAddressRepository.FindByApplicantIdAsync(address.ApplicantId.Value);

            foreach (var sibling in siblingAddresses)
            {
                if (sibling.Id == addressId) continue;
                if (!sibling.HasProperty("isPrimary")) continue;

                var trackedSibling = await applicantAddressRepository.GetAsync(sibling.Id);
                trackedSibling.SetProperty("isPrimary", false);
                await applicantAddressRepository.UpdateAsync(trackedSibling, autoSave: true);
            }
        }

        await applicantAddressRepository.UpdateAsync(address, autoSave: true);

        logger.LogInformation("Address {AddressId} set as primary", addressId);
        return "Address set as primary";
    }
}
