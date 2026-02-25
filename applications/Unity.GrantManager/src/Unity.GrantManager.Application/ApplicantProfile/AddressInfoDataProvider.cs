using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.ApplicantProfile
{
    /// <summary>
    /// Provides address information for the applicant profile by querying
    /// application addresses linked to the applicant's form submissions.
    /// Addresses are resolved via both the ApplicationId and ApplicantId
    /// relationships, with duplicates removed.
    /// </summary>
    [ExposeServices(typeof(IApplicantProfileDataProvider))]
    public class AddressInfoDataProvider(
        ICurrentTenant currentTenant,
        IRepository<ApplicationFormSubmission, Guid> applicationFormSubmissionRepository,
        IRepository<ApplicantAddress, Guid> applicantAddressRepository,
        IRepository<Application, Guid> applicationRepository)
        : IApplicantProfileDataProvider, ITransientDependency
    {
        /// <inheritdoc />
        public string Key => ApplicantProfileKeys.AddressInfo;

        /// <inheritdoc />
        public async Task<ApplicantProfileDataDto> GetDataAsync(ApplicantProfileInfoRequest request)
        {
            var dto = new ApplicantAddressInfoDto
            {
                Addresses = []
            };

            var subject = request.Subject ?? string.Empty;
            var normalizedSubject = subject.Contains('@')
                    ? subject[..subject.IndexOf('@')].ToUpperInvariant()
                    : subject.ToUpperInvariant();

            using (currentTenant.Change(request.TenantId))
            {
                var submissionsQuery = await applicationFormSubmissionRepository.GetQueryableAsync();
                var addressesQuery = await applicantAddressRepository.GetQueryableAsync();
                var applicationsQuery = await applicationRepository.GetQueryableAsync();

                var matchingSubmissions = submissionsQuery
                    .Where(s => s.OidcSub == normalizedSubject);

                // Addresses linked via ApplicationId — not editable (owned by an application)
                var byApplicationId =
                    from submission in matchingSubmissions
                    join address in addressesQuery on submission.ApplicationId equals address.ApplicationId
                    join application in applicationsQuery on address.ApplicationId equals application.Id
                    select new { address, address.CreationTime, application.ReferenceNo, IsEditable = false };

                // Addresses linked via ApplicantId — editable (directly from the applicant)
                var byApplicantId =
                    from submission in matchingSubmissions
                    join address in addressesQuery on submission.ApplicantId equals address.ApplicantId
                    join application in applicationsQuery on address.ApplicationId equals application.Id into apps
                    from application in apps.DefaultIfEmpty()
                    select new { address, address.CreationTime, ReferenceNo = application != null ? application.ReferenceNo : null, IsEditable = true };

                var results = await byApplicationId
                    .Concat(byApplicantId)
                    .ToListAsync();

                // Deduplicate by address Id — application-linked (IsEditable = false) takes priority
                var deduplicated = results
                    .GroupBy(r => r.address.Id)
                    .Select(g => g.OrderBy(r => r.IsEditable).First())
                    .ToList();

                var addressDtos = deduplicated.Select(r => new AddressInfoItemDto
                {
                    Id = r.address.Id,
                    AddressType = GetAddressTypeName(r.address.AddressType),
                    Street = r.address.Street ?? string.Empty,
                    Street2 = r.address.Street2 ?? string.Empty,
                    Unit = r.address.Unit ?? string.Empty,
                    City = r.address.City ?? string.Empty,
                    Province = r.address.Province ?? string.Empty,
                    PostalCode = r.address.Postal ?? string.Empty,
                    Country = r.address.Country ?? string.Empty,
                    IsPrimary = r.address.HasProperty("isPrimary") && r.address.GetProperty<bool>("isPrimary"),
                    IsEditable = r.IsEditable,
                    ReferenceNo = r.ReferenceNo
                }).ToList();

                // If no address is marked as primary, mark the most recent one as primary
                if (addressDtos.Count > 0 && !addressDtos.Any(a => a.IsPrimary))
                {
                    var mostRecent = deduplicated.OrderByDescending(r => r.CreationTime).First();
                    var mostRecentDto = addressDtos.First(a => a.Id == mostRecent.address.Id);
                    mostRecentDto.IsPrimary = true;
                }

                dto.Addresses.AddRange(addressDtos);
            }

            return dto;
        }

        /// <summary>
        /// Maps an <see cref="AddressType"/> enum value to a human-readable display name.
        /// </summary>
        private static string GetAddressTypeName(AddressType addressType)
        {
            return addressType switch
            {
                AddressType.PhysicalAddress => "Physical",
                AddressType.MailingAddress => "Mailing",
                AddressType.BusinessAddress => "Business",
                _ => addressType.ToString()
            };
        }
    }
}
