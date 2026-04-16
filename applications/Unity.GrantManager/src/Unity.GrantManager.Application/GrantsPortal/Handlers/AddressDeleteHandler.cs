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
            await applicantAddressRepository.DeleteAsync(address);
        }

        logger.LogInformation("Address {AddressId} deleted successfully", addressId);
        return "Address deleted successfully";
    }
}
