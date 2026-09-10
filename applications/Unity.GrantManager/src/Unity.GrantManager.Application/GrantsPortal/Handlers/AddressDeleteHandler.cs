using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantsPortal.Messages;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal.Handlers;

public class AddressDeleteHandler(
    IApplicantAddressRepository applicantAddressRepository,
    IApplicantAddressManager applicantAddressManager,
    ILogger<AddressDeleteHandler> logger) : IPortalCommandHandler, ITransientDependency
{
    public string DataType => "ADDRESS_DELETE_COMMAND";

    [UnitOfWork]
    public virtual async Task<string> HandleAsync(PluginDataPayload payload)
    {
        var addressId = Guid.Parse(payload.AddressId ?? throw new ArgumentException("addressId is required"));

        logger.LogInformation("Deleting address {AddressId} for profile {ProfileId}", addressId, payload.ProfileId);

        var address = await applicantAddressRepository.FindAsync(addressId);
        if (address != null)
        {
            var wasPrimary = address.IsFlaggedPrimary();
            var addressType = address.AddressType;
            var applicantId = address.ApplicantId;

            await applicantAddressRepository.DeleteAsync(address);

            if (wasPrimary && applicantId.HasValue)
            {
                // The address type group just lost its primary, so promote the most recent survivor.
                await applicantAddressManager.ElectPrimaryAsync(applicantId.Value, addressType, addressId);
            }
        }

        logger.LogInformation("Address {AddressId} deleted successfully", addressId);
        return "Address deleted successfully";
    }
}
