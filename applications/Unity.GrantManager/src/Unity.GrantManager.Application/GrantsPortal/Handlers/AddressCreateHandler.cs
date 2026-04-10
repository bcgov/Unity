using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantsPortal.Messages;
using Unity.GrantManager.GrantsPortal.Messages.Commands;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal.Handlers;

public class AddressCreateHandler(
    IApplicantAddressRepository applicantAddressRepository,
    ILogger<AddressCreateHandler> logger) : IPortalCommandHandler, ITransientDependency
{
    public string DataType => "ADDRESS_CREATE_COMMAND";

    [UnitOfWork]
    public virtual async Task<string> HandleAsync(PluginDataPayload payload)
    {
        var addressId = Guid.Parse(payload.AddressId ?? throw new ArgumentException("addressId is required"));
        var profileId = Guid.Parse(payload.ProfileId ?? throw new ArgumentException("profileId is required"));
        var innerData = payload.Data?.ToObject<AddressCreateData>()
                        ?? throw new ArgumentException("Address data is required");

        if (innerData.ApplicantId == Guid.Empty)
        {
            throw new ArgumentException("applicantId is required");
        }

        // Idempotency: if the address already exists, treat as success
        var existing = await applicantAddressRepository.FindAsync(addressId);
        if (existing != null)
        {
            logger.LogInformation("Address {AddressId} already exists. Treating as idempotent success.", addressId);
            return "Address already exists";
        }

        logger.LogInformation("Creating address {AddressId} for profile {ProfileId}", addressId, profileId);

        var address = new ApplicantAddress
        {
            ApplicantId = innerData.ApplicantId,
            Street = innerData.Street,
            Street2 = innerData.Street2,
            Unit = innerData.Unit,
            City = innerData.City,
            Province = innerData.Province,
            Postal = innerData.PostalCode,
            Country = innerData.Country,
            AddressType = MapAddressType(innerData.AddressType)
        };

        EntityHelper.TrySetId(address, () => addressId);

        address.SetProperty(AddressExtraPropertyNames.ProfileId, profileId.ToString());
        address.SetProperty(AddressExtraPropertyNames.IsPrimary, innerData.IsPrimary);

        // Demote existing primary addresses for the same applicant
        if (innerData.IsPrimary)
        {
            var siblingAddresses = await applicantAddressRepository.FindByApplicantIdAsync(innerData.ApplicantId);

            foreach (var sibling in siblingAddresses)
            {
                if (!sibling.HasProperty(AddressExtraPropertyNames.IsPrimary)) continue;
                if (!sibling.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary)) continue;

                var trackedSibling = await applicantAddressRepository.GetAsync(sibling.Id);
                trackedSibling.SetProperty(AddressExtraPropertyNames.IsPrimary, false);
                await applicantAddressRepository.UpdateAsync(trackedSibling);
            }
        }

        await applicantAddressRepository.InsertAsync(address);

        logger.LogInformation("Address {AddressId} created successfully", addressId);
        return "Address created successfully";
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
