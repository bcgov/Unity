using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Applications;
using Unity.Payments.Domain.PaymentRequests;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.ApplicantProfile
{
    /// <summary>
    /// Provides payment information for the applicant profile by querying
    /// payment requests linked to the applicant's form submissions.
    /// </summary>
    [ExposeServices(typeof(IApplicantProfileDataProvider))]
    public class PaymentInfoDataProvider(
        ICurrentTenant currentTenant,
        IRepository<ApplicationFormSubmission, Guid> applicationFormSubmissionRepository,
        IRepository<Application, Guid> applicationRepository,
        IRepository<PaymentRequest, Guid> paymentRequestRepository)
        : IApplicantProfileDataProvider, ITransientDependency
    {
        /// <inheritdoc />
        public string Key => ApplicantProfileKeys.PaymentInfo;

        /// <inheritdoc />
        public async Task<ApplicantProfileDataDto> GetDataAsync(ApplicantProfileInfoRequest request)
        {
            var dto = new ApplicantPaymentInfoDto
            {
                Payments = []
            };

            var normalizedSubject = SubjectNormalizer.Normalize(request.Subject);
            if (normalizedSubject is null) return dto;

            using (currentTenant.Change(request.TenantId))
            {
                var submissionsQuery = await applicationFormSubmissionRepository.GetQueryableAsync();
                var applicationsQuery = await applicationRepository.GetQueryableAsync();

                var applicationLookup = await (
                    from submission in submissionsQuery
                    join application in applicationsQuery on submission.ApplicationId equals application.Id
                    where submission.OidcSub == normalizedSubject
                    select new { application.Id, application.ReferenceNo }
                ).Distinct().ToDictionaryAsync(a => a.Id, a => a.ReferenceNo);

                if (applicationLookup.Count == 0) return dto;

                // Payment info is secured via feature flags and permissions, so direct query for this data instead of using module service

                var paymentsQueryable = await paymentRequestRepository.GetQueryableAsync();
                var paymentDetails = await paymentsQueryable
                    .Where(pr => applicationLookup.Keys.Contains(pr.CorrelationId))
                    .ToListAsync();

                dto.Payments.AddRange(paymentDetails.Select(p => new PaymentInfoItemDto
                {
                    Id = p.Id,
                    PaymentNumber = p.InvoiceNumber ?? string.Empty,
                    ReferenceNo = applicationLookup.TryGetValue(p.CorrelationId, out var refNo) ? refNo : string.Empty,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    PaymentStatus = p.Status.ToString()
                }));
            }

            return dto;
        }
    }
}
