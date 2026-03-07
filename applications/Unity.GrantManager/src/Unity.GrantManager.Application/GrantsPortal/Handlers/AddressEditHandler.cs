using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantsPortal.Messages;
using Unity.GrantManager.GrantsPortal.Messages.Commands;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal.Handlers;

public class AddressEditHandler(
    IApplicantAddressRepository applicantAddressRepository,
    ILogger<AddressEditHandler> logger) : IPortalCommandHandler, ITransientDependency
{
    public string DataType => "ADDRESS_EDIT_COMMAND";

    [UnitOfWork]
    public virtual async Task<string> HandleAsync(PluginDataPayload payload)
    {
        var addressId = Guid.Parse(payload.AddressId ?? throw new ArgumentException("addressId is required"));
        var innerData = payload.Data?.ToObject<AddressEditData>()
                        ?? throw new ArgumentException("Address data is required");

        logger.LogInformation("Editing address {AddressId} for profile {ProfileId}", addressId, payload.ProfileId);

        var address = await applicantAddressRepository.GetAsync(addressId);

        address.Street = innerData.Street;
        address.Street2 = innerData.Street2;
        address.Unit = innerData.Unit;
        address.City = innerData.City;
        address.Province = innerData.Province;
        address.Postal = innerData.PostalCode;
        address.Country = innerData.Country;
        address.AddressType = MapAddressType(innerData.AddressType);

        await applicantAddressRepository.UpdateAsync(address, autoSave: true);

        logger.LogInformation("Address {AddressId} updated successfully", addressId);
        return "Address updated successfully";
    }

    private static GrantApplications.AddressType MapAddressType(string? portalAddressType)
    {
        return portalAddressType?.ToUpperInvariant() switch
        {
            "MAILING" => GrantApplications.AddressType.MailingAddress,
            "PHYSICAL" => GrantApplications.AddressType.PhysicalAddress,
            "BUSINESS" => GrantApplications.AddressType.BusinessAddress,
            _ => GrantApplications.AddressType.PhysicalAddress
        };
    }
}
