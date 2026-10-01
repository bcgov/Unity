using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile.Addresses;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.Modules.Shared;
using Volo.Abp.Uow;

namespace Unity.GrantManager.ApplicantProfile;

/// <summary>
/// Authorized facade over <see cref="IApplicantAddressManager"/> for the internal
/// Applicant Addresses widget.
/// </summary>
[Authorize]
public class ApplicantAddressAppService(IApplicantAddressManager applicantAddressManager)
    : GrantManagerAppService, IApplicantAddressAppService
{
    /// <inheritdoc />
    [Authorize(UnitySelector.ApplicantManagement.Addresses.Default)]
    public virtual async Task<AddressInfoItemDto> GetAsync(Guid applicantId, Guid addressId)
    {
        var address = await applicantAddressManager.GetOwnedAsync(applicantId, addressId);
        return MapToDto(address);
    }

    /// <inheritdoc />
    [Authorize(UnitySelector.ApplicantManagement.Addresses.Update)]
    [UnitOfWork(isTransactional: true)]
    public virtual async Task<AddressInfoItemDto> UpdateAsync(
        Guid applicantId, Guid addressId, UpdateApplicantProfileAddressDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var address = await applicantAddressManager.UpdateAsync(
            applicantId,
            addressId,
            new ApplicantAddressInput(
                addressId, input.Street, input.Street2, input.Unit,
                input.City, input.Province, input.PostalCode),
            input.AddressType,
            input.IsPrimary);

        return MapToDto(address);
    }

    /// <inheritdoc />
    [Authorize(UnitySelector.ApplicantManagement.Addresses.Update)]
    [UnitOfWork(isTransactional: true)]
    public virtual async Task<bool> SetPrimaryAsync(Guid applicantId, Guid addressId)
    {
        await applicantAddressManager.SetPrimaryAsync(applicantId, addressId);
        return true;
    }

    // ReferenceNo is left unset: filling it needs an extra Application lookup and no consumer reads it.
    private static AddressInfoItemDto MapToDto(ApplicantAddress address)
    {
        return new AddressInfoItemDto
        {
            Id = address.Id,
            AddressType = AddressTypeMapper.ToDisplayName(address.AddressType),
            Street = address.Street ?? string.Empty,
            Street2 = address.Street2 ?? string.Empty,
            Unit = address.Unit ?? string.Empty,
            City = address.City ?? string.Empty,
            Province = address.Province ?? string.Empty,
            PostalCode = address.Postal ?? string.Empty,
            Country = address.Country ?? string.Empty,
            IsPrimary = address.IsFlaggedPrimary(),
            IsEditable = !address.ApplicationId.HasValue
        };
    }
}
