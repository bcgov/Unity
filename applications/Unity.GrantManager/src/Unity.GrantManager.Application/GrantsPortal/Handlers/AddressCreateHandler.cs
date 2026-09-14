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
    IApplicantAddressManager applicantAddressManager,
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
            AddressType = AddressTypeMapper.FromPortalValue(innerData.AddressType)
        };

        EntityHelper.TrySetId(address, () => addressId);

        address.SetProperty(AddressExtraPropertyNames.ProfileId, profileId.ToString());
        address.SetPrimaryFlag(innerData.IsPrimary);

        if (innerData.IsPrimary)
        {
            // Primary is scoped to the address type, so only same-type siblings are demoted.
            await applicantAddressManager.DemotePrimarySiblingsAsync(
                innerData.ApplicantId,
                address.AddressType,
                addressId);
        }

        await applicantAddressRepository.InsertAsync(address);

        logger.LogInformation("Address {AddressId} created successfully", addressId);
        return "Address created successfully";
    }
}
