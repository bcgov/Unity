using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace Unity.GrantManager.Applications;

/// <summary>
/// Enforces the primary-address invariant: an applicant may hold at most one primary address
/// within each <see cref="AddressType"/> group. The rule is generic over the enum — no member
/// receives special treatment.
/// </summary>
public class ApplicantAddressManager(
    IApplicantAddressRepository applicantAddressRepository,
    IApplicantRepository applicantRepository,
    IApplicationRepository applicationRepository)
    : DomainService, IApplicantAddressManager
{
    /// <inheritdoc />
    public virtual async Task<Application?> FindLatestApplicationAsync(Guid applicantId)
    {
        var applications = await applicationRepository.GetQueryableAsync();
        return await AsyncExecuter.FirstOrDefaultAsync(applications.ForApplicantAddressCreation(applicantId));
    }

    /// <inheritdoc />
    public virtual async Task<(ApplicantAddress? PhysicalAddress, ApplicantAddress? MailingAddress)> SavePrimaryAddressesAsync(
        Guid applicantId,
        Guid? expectedApplicationId,
        ApplicantAddressInput? physicalAddress,
        ApplicantAddressInput? mailingAddress)
    {
        Application? creationApplication = null;
        if (physicalAddress?.Id == Guid.Empty || mailingAddress?.Id == Guid.Empty)
        {
            var applicant = await applicantRepository.GetAsync(applicantId);
            if (applicant.IsDeleted)
            {
                throw new BusinessException(GrantManagerDomainErrorCodes.AddressApplicantUnavailable);
            }

            creationApplication = await FindLatestApplicationAsync(applicantId)
                ?? throw new BusinessException(GrantManagerDomainErrorCodes.AddressApplicationRequired);

            if (expectedApplicationId != creationApplication.Id)
            {
                throw new BusinessException(GrantManagerDomainErrorCodes.AddressApplicationChanged);
            }
        }

        // Validate both sections before modifying either record.
        var physical = await ResolvePrimaryAddressAsync(
            applicantId, physicalAddress, AddressType.PhysicalAddress, creationApplication);
        var mailing = await ResolvePrimaryAddressAsync(
            applicantId, mailingAddress, AddressType.MailingAddress, creationApplication);

        return (await SaveAddressAsync(applicantId, physical, physicalAddress),
            await SaveAddressAsync(applicantId, mailing, mailingAddress));
    }

    private async Task<ApplicantAddress?> ResolvePrimaryAddressAsync(Guid applicantId,
        ApplicantAddressInput? input, AddressType expectedType, Application? creationApplication)
    {
        if (input == null)
        {
            return null;
        }

        if (input.Id == Guid.Empty)
        {
            if (string.IsNullOrWhiteSpace(input.Street) && string.IsNullOrWhiteSpace(input.Street2))
            {
                throw new BusinessException(expectedType == AddressType.PhysicalAddress
                    ? GrantManagerDomainErrorCodes.PhysicalAddressStreetRequired
                    : GrantManagerDomainErrorCodes.MailingAddressStreetRequired);
            }

            if (creationApplication == null)
            {
                throw new BusinessException(GrantManagerDomainErrorCodes.AddressApplicationRequired);
            }

            await EnsureAddressTypeMissingAsync(applicantId, expectedType);

            var newAddress = new ApplicantAddress
            {
                ApplicantId = applicantId,
                TenantId = creationApplication.TenantId,
                ApplicationId = creationApplication.Id,
                AddressType = expectedType
            };
            newAddress.SetPrimaryFlag(true);
            return newAddress;
        }

        var applicantAddress = await applicantAddressRepository.GetAsync(input.Id);
        if (applicantAddress.ApplicantId != applicantId)
        {
            throw new BusinessException("Unity:Applicant:AddressNotFound")
                .WithData("ApplicantId", applicantId)
                .WithData("AddressId", input.Id);
        }

        if (applicantAddress.AddressType != expectedType)
        {
            throw new BusinessException("Unity:Applicant:AddressTypeMismatch")
                .WithData("ApplicantId", applicantId)
                .WithData("AddressId", input.Id)
                .WithData("ExpectedType", expectedType.ToString());
        }

        return applicantAddress;
    }

    private async Task EnsureAddressTypeMissingAsync(Guid applicantId, AddressType addressType)
    {
        var addresses = await applicantAddressRepository.FindByApplicantIdAsync(applicantId);
        if (addresses.Any(address => address.AddressType == addressType))
        {
            throw new BusinessException(addressType == AddressType.PhysicalAddress
                ? GrantManagerDomainErrorCodes.PhysicalAddressAlreadyExists
                : GrantManagerDomainErrorCodes.MailingAddressAlreadyExists);
        }
    }

    private async Task<ApplicantAddress?> SaveAddressAsync(Guid applicantId,
        ApplicantAddress? address, ApplicantAddressInput? input)
    {
        if (address == null || input == null)
        {
            return null;
        }

        address.Street = input.Street?.Trim() ?? string.Empty;
        address.Street2 = input.Street2?.Trim() ?? string.Empty;
        address.Unit = input.Unit?.Trim() ?? string.Empty;
        address.City = input.City?.Trim() ?? string.Empty;
        address.Province = input.Province?.Trim() ?? string.Empty;
        address.Postal = input.PostalCode?.Trim() ?? string.Empty;

        if (input.Id == Guid.Empty)
        {
            // Best-effort recheck immediately before insertion; another request may still
            // insert before this unit of work commits. Concurrent writers are not serialized.
            await EnsureAddressTypeMissingAsync(applicantId, address.AddressType);
            return await applicantAddressRepository.InsertAsync(address);
        }

        return await applicantAddressRepository.UpdateAsync(address);
    }

    /// <inheritdoc />
    public virtual async Task DemotePrimarySiblingsAsync(
        Guid applicantId,
        AddressType addressType,
        Guid? excludeAddressId = null)
    {
        var siblings = await GetGroupAsync(applicantId, addressType, excludeAddressId);

        foreach (var sibling in siblings)
        {
            if (!sibling.IsFlaggedPrimary())
            {
                continue;
            }

            var trackedSibling = await GetTrackedAsync(sibling.Id);
            trackedSibling.SetPrimaryFlag(false);
            await applicantAddressRepository.UpdateAsync(trackedSibling);
        }
    }

    /// <inheritdoc />
    public virtual async Task<Guid?> ElectPrimaryAsync(
        Guid applicantId,
        AddressType addressType,
        Guid? excludeAddressId = null)
    {
        var candidates = await GetGroupAsync(applicantId, addressType, excludeAddressId);

        if (candidates.Count == 0 || candidates.Exists(candidate => candidate.IsFlaggedPrimary()))
        {
            return null;
        }

        var mostRecent = candidates
            .OrderByDescending(candidate => candidate.CreationTime)
            .First();

        var trackedAddress = await GetTrackedAsync(mostRecent.Id);
        trackedAddress.SetPrimaryFlag(true);
        await applicantAddressRepository.UpdateAsync(trackedAddress);

        return trackedAddress.Id;
    }

    /// <summary>
    /// Returns the applicant's addresses that belong to the given address type group,
    /// optionally skipping one address.
    /// </summary>
    private async Task<List<ApplicantAddress>> GetGroupAsync(
        Guid applicantId,
        AddressType addressType,
        Guid? excludeAddressId)
    {
        var addresses = await applicantAddressRepository.FindByApplicantIdAsync(applicantId);

        return
        [
            .. addresses
                .Where(address => address.AddressType == addressType)
                .Where(address => !excludeAddressId.HasValue || address.Id != excludeAddressId.Value)
        ];
    }

    /// <summary>
    /// Re-reads an address through the repository. <c>FindByApplicantIdAsync</c> queries with
    /// <c>AsNoTracking</c>, so the returned instances cannot be updated directly.
    /// </summary>
    private Task<ApplicantAddress> GetTrackedAsync(Guid addressId)
    {
        return applicantAddressRepository.GetAsync(addressId);
    }
}
