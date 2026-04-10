using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantsPortal.Messages;
using Unity.GrantManager.GrantsPortal.Messages.Commands;
using Volo.Abp.Data;
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

        if (innerData.IsPrimary && address.ApplicantId.HasValue)
        {
            await DemoteSiblingPrimaryAddressesAsync(address.ApplicantId.Value, addressId);
        }

        address.SetProperty(AddressExtraPropertyNames.IsPrimary, innerData.IsPrimary);

        await applicantAddressRepository.UpdateAsync(address);

        logger.LogInformation("Address {AddressId} updated successfully", addressId);
        return "Address updated successfully";
    }

    private async Task DemoteSiblingPrimaryAddressesAsync(Guid applicantId, Guid excludeAddressId)
    {
        var siblingAddresses = await applicantAddressRepository.FindByApplicantIdAsync(applicantId);

        foreach (var sibling in siblingAddresses)
        {
            if (sibling.Id == excludeAddressId) continue;
            if (!sibling.HasProperty(AddressExtraPropertyNames.IsPrimary)) continue;
            if (!sibling.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary)) continue;

            var trackedSibling = await applicantAddressRepository.GetAsync(sibling.Id);
            trackedSibling.SetProperty(AddressExtraPropertyNames.IsPrimary, false);
            await applicantAddressRepository.UpdateAsync(trackedSibling);
        }
    }

    private static AddressType MapAddressType(string? portalAddressType)
    {
        return portalAddressType?.ToUpperInvariant() switch
        {
            "MAILING" => AddressType.MailingAddress,
            "PHYSICAL" => AddressType.PhysicalAddress,
            "BUSINESS" => AddressType.BusinessAddress,
            _ => AddressType.PhysicalAddress
        };
    }
}
