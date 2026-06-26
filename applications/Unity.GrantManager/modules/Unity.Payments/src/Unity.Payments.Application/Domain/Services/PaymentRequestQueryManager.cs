using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Enums;
using Unity.Payments.PaymentRequests;
using Unity.Payments.Suppliers;
using Volo.Abp.Domain.Services;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace Unity.Payments.Domain.Services
{
    public class PaymentRequestQueryManager(
        IPaymentRequestRepository paymentRequestRepository,
        ISiteRepository siteRepository,
        IExternalUserLookupServiceProvider externalUserLookupServiceProvider,
        CasPaymentRequestCoordinator casPaymentRequestCoordinator,
        IObjectMapper objectMapper,
        IApplicationRepository applicationRepository) : DomainService, IPaymentRequestQueryManager
    {
        private static readonly HashSet<string> SiteFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "siteNumber",
            "payGroup"
        };

        private static readonly HashSet<string> AccountCodingFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "accountCodingDisplay"
        };

        private static readonly HashSet<string> TagFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "paymentTags"
        };

        private static readonly HashSet<string> RequesterFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "paymentRequesterName"
        };

        private static readonly HashSet<string> ExpenseApprovalFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "l1ApproverName",
            "l1ApprovalDate",
            "l2ApproverName",
            "l2ApprovalDate",
            "l3ApproverName",
            "l3ApprovalDate"
        };

        private static readonly HashSet<string> ApplicantFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "applicantName"
        };

        public Task<int> GetPaymentRequestCountBySiteIdAsync(Guid siteId)
        {
            return paymentRequestRepository.GetPaymentRequestCountBySiteId(siteId);
        }

        public async Task<long> GetPaymentRequestCountAsync()
        {
            return await paymentRequestRepository.GetCountAsync();
        }

        public async Task<PaymentRequest?> GetPaymentRequestByIdAsync(Guid paymentRequestId)
        {
            return await paymentRequestRepository.GetAsync(paymentRequestId);
        }

        public async Task<List<PaymentRequest>> GetPaymentRequestsByIdsAsync(List<Guid> paymentRequestIds, bool includeDetails = false)
        {
            return await paymentRequestRepository.GetListAsync(x => paymentRequestIds.Contains(x.Id), includeDetails: includeDetails);
        }

        public async Task<List<PaymentRequest>> GetPagedPaymentRequestsWithIncludesAsync(int skipCount, int maxResultCount, string sorting, IReadOnlyList<string>? requestedFields = null)
        {
            var paymentsQueryable = await paymentRequestRepository.GetQueryableAsync();
            var includeSite = IncludesAny(requestedFields, SiteFields);
            var includeAccountCoding = IncludesAny(requestedFields, AccountCodingFields);
            var includeTags = IncludesAny(requestedFields, TagFields);
            var includeExpenseApprovals = IncludesAny(requestedFields, ExpenseApprovalFields);

            paymentsQueryable = paymentsQueryable.AsNoTracking();

            if (includeSite)
            {
                paymentsQueryable = paymentsQueryable.Include(pr => pr.Site);
            }

            if (includeAccountCoding)
            {
                paymentsQueryable = paymentsQueryable.Include(pr => pr.AccountCoding);
            }

            if (includeTags)
            {
                paymentsQueryable = paymentsQueryable
                    .Include(pr => pr.PaymentTags)
                    .ThenInclude(pt => pt.Tag);
            }

            if (includeExpenseApprovals)
            {
                paymentsQueryable = paymentsQueryable.Include(pr => pr.ExpenseApprovals);
            }

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            var paymentWithIncludes = await paymentsQueryable.ToListAsync();
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

            return paymentWithIncludes;
        }

        public async Task<PaymentRequest> InsertPaymentRequestAsync(PaymentRequest paymentRequest)
        {
            return await paymentRequestRepository.InsertAsync(paymentRequest);
        }

        public async Task<PaymentRequestDto> CreatePaymentRequestDtoAsync(Guid paymentRequestId)
        {
            var payment = await paymentRequestRepository.GetAsync(paymentRequestId);
            return new PaymentRequestDto
            {
                Id = payment.Id,
                InvoiceNumber = payment.InvoiceNumber,
                InvoiceStatus = payment.InvoiceStatus,
                Amount = payment.Amount,
                PayeeName = payment.PayeeName,
                SupplierNumber = payment.SupplierNumber,
                ContractNumber = payment.ContractNumber,
                CorrelationId = payment.CorrelationId,
                CorrelationProvider = payment.CorrelationProvider,
                Description = payment.Description,
                CreationTime = payment.CreationTime,
                Status = payment.Status,
                ReferenceNumber = payment.ReferenceNumber,
                SubmissionConfirmationCode = payment.SubmissionConfirmationCode,
                Note = payment.Note
            };
        }

        public async Task<List<PaymentDetailsDto>> GetListByApplicationIdsAsync(List<Guid> applicationIds)
        {
            var paymentsQueryable = await paymentRequestRepository.GetQueryableAsync();
            var filteredPayments = await paymentsQueryable
                .Include(pr => pr.Site)
                .Where(pr => applicationIds.Contains(pr.CorrelationId))
                .ToListAsync();

            return objectMapper.Map<List<PaymentRequest>, List<PaymentDetailsDto>>(filteredPayments);
        }

        public async Task<List<PaymentDetailsDto>> GetListByApplicationIdAsync(Guid applicationId)
        {
            var paymentsQueryable = await paymentRequestRepository.GetQueryableAsync();
            var filteredPayments = await paymentsQueryable
                .Include(pr => pr.Site)
                .Where(e => e.CorrelationId == applicationId)
                .ToListAsync();

            return objectMapper.Map<List<PaymentRequest>, List<PaymentDetailsDto>>(filteredPayments);
        }

        public async Task<List<PaymentDetailsDto>> GetListByPaymentIdsAsync(List<Guid> paymentIds)
        {
            var paymentsQueryable = await paymentRequestRepository.GetQueryableAsync();
            var payments = await paymentsQueryable
                .Where(e => paymentIds.Contains(e.Id))
                .Include(pr => pr.Site)
                .Include(x => x.ExpenseApprovals)
                .ToListAsync();

            return objectMapper.Map<List<PaymentRequest>, List<PaymentDetailsDto>>(payments);
        }

        public async Task<decimal> GetTotalPaymentRequestAmountByCorrelationIdAsync(Guid correlationId)
        {
            return await paymentRequestRepository.GetTotalPaymentRequestAmountByCorrelationIdAsync(correlationId);
        }

        public async Task<List<PaymentRequestDto>> MapToDtoAndLoadDetailsAsync(List<PaymentRequest> paymentsList, IReadOnlyList<string>? requestedFields = null)
        {
            var includeApplicant = IncludesAny(requestedFields, ApplicantFields);
            var includeRequester = IncludesAny(requestedFields, RequesterFields);
            var includeExpenseApprovals = IncludesAny(requestedFields, ExpenseApprovalFields);
            var includeAccountCoding = IncludesAny(requestedFields, AccountCodingFields);

            var paymentDtos = objectMapper.Map<List<PaymentRequest>, List<PaymentRequestDto>>(paymentsList);

            if (includeApplicant)
            {
                // Batch-fetch applicant IDs for all unique correlation IDs (application IDs)
                var applicationIds = paymentDtos
                    .Select(p => p.CorrelationId)
                    .Distinct()
                    .ToArray();

                var applications = await applicationRepository.GetListByIdsAsync(applicationIds) ?? [];
                var applicantIdByApplicationId = applications.ToDictionary(a => a.Id, a => a.ApplicantId);

                foreach (var paymentDto in paymentDtos)
                {
                    if (applicantIdByApplicationId.TryGetValue(paymentDto.CorrelationId, out var applicantId))
                    {
                        paymentDto.ApplicantId = applicantId;
                    }
                }
            }

            var userDictionary = new Dictionary<Guid, PaymentUserDto>();
            if (includeRequester || includeExpenseApprovals)
            {
                var paymentRequesterIds = includeRequester
                    ? paymentDtos
                        .Select(payment => payment.CreatorId)
                        .OfType<Guid>()
                        .Distinct()
                    : [];

                var expenseApprovalCreatorIds = includeExpenseApprovals
                    ? paymentDtos
                        .SelectMany(payment => payment.ExpenseApprovals ?? [])
                        .Where(expenseApproval => expenseApproval.Status != ExpenseApprovalStatus.Requested)
                        .Select(expenseApproval => expenseApproval.DecisionUserId)
                        .OfType<Guid>()
                        .Distinct()
                    : [];

                // Call external lookup for each distinct User Id and store in a dictionary.
                var allUserIds = paymentRequesterIds.Concat(expenseApprovalCreatorIds).Distinct();
                foreach (var userId in allUserIds)
                {
                    var userInfo = await externalUserLookupServiceProvider.FindByIdAsync(userId);
                    if (userInfo != null)
                    {
                        userDictionary[userId] = objectMapper.Map<IUserData, PaymentUserDto>(userInfo);
                    }
                }
            }

            // Map UserInfo details to each ExpenseApprovalDto
            foreach (var paymentRequestDto in paymentDtos)
            {
                if (includeRequester
                        && paymentRequestDto.CreatorId.HasValue
                        && userDictionary.TryGetValue(paymentRequestDto.CreatorId.Value, out var paymentRequestUserDto))
                {
                    paymentRequestDto.CreatorUser = paymentRequestUserDto;
                }

                if (includeAccountCoding && paymentRequestDto.AccountCoding != null)
                {
                    paymentRequestDto.AccountCodingDisplay = await GetAccountDistributionCodeAsync(paymentRequestDto.AccountCoding);
                }

                if (includeExpenseApprovals && paymentRequestDto.ExpenseApprovals != null)
                {
                    foreach (var expenseApproval in paymentRequestDto.ExpenseApprovals)
                    {
                        if (expenseApproval.DecisionUserId.HasValue
                            && userDictionary.TryGetValue(expenseApproval.DecisionUserId.Value, out var expenseApprovalUserDto))
                        {
                            expenseApproval.DecisionUser = expenseApprovalUserDto;
                        }
                    }
                }
            }

            return paymentDtos;
        }

        private static bool IncludesAny(IReadOnlyList<string>? requestedFields, IReadOnlySet<string> fieldsToCheck)
        {
            if (requestedFields == null || requestedFields.Count == 0)
            {
                return true;
            }

            return requestedFields.Any(fieldsToCheck.Contains);
        }

        public Task<string> GetAccountDistributionCodeAsync(AccountCodingDto? accountCoding)
        {
            return Task.FromResult(AccountCodingFormatter.Format(accountCoding));
        }

        public void ApplyErrorSummary(List<PaymentRequestDto> mappedPayments)
        {
            mappedPayments.ForEach(mappedPayment =>
            {
                if (!string.IsNullOrWhiteSpace(mappedPayment.CasResponse) &&
                    !mappedPayment.CasResponse.Equals("SUCCEEDED", StringComparison.OrdinalIgnoreCase))
                {
                    mappedPayment.ErrorSummary = mappedPayment.CasResponse;
                }
            });
        }

        public async Task ManuallyAddPaymentRequestsToReconciliationQueueAsync(List<Guid> paymentRequestIds)
        {
            List<PaymentRequestDto> paymentRequestDtos = [];
            foreach (var paymentRequestId in paymentRequestIds)
            {
                var paymentRequest = await paymentRequestRepository.GetAsync(paymentRequestId);
                if (paymentRequest != null)
                {
                    var paymentRequestDto = objectMapper.Map<PaymentRequest, PaymentRequestDto>(paymentRequest);
                    if (paymentRequest.SiteId.HasValue)
                    {
                        Site site = await siteRepository.GetAsync(paymentRequest.SiteId.Value);
                        paymentRequestDto.Site = objectMapper.Map<Site, SiteDto>(site);
                    }
                    paymentRequestDtos.Add(paymentRequestDto);
                }
            }
            await casPaymentRequestCoordinator.ManuallyAddPaymentRequestsToReconciliationQueue(paymentRequestDtos);
        }

        public async Task<List<PaymentRequestDto>> GetPaymentPendingListByCorrelationIdAsync(Guid applicationId)
        {
            var payments = await paymentRequestRepository.GetPaymentPendingListByCorrelationIdAsync(applicationId);
            return objectMapper.Map<List<PaymentRequest>, List<PaymentRequestDto>>(payments);
        }

        public async Task<List<PaymentRequestDto>> GetPaymentPendingListByCorrelationIdsAsync(IEnumerable<Guid> correlationIds)
        {
            var payments = await paymentRequestRepository.GetPaymentPendingListByCorrelationIdsAsync(correlationIds);
            return objectMapper.Map<List<PaymentRequest>, List<PaymentRequestDto>>(payments);
        }

        /// <summary>
        /// Retrieves a payment rollup for the specified application and its associated child applications.
        /// </summary>
        /// <remarks>This method combines payment information from both the main application and its child
        /// applications, providing an overall view of payment status. Use this method to obtain a single rollup when
        /// displaying applications with related child records.</remarks>
        /// <param name="applicationId">The unique identifier of the main application for which the payment rollup is requested.</param>
        /// <param name="childApplicationIds">A list of unique identifiers representing child applications whose payment data will be included in the
        /// rollup. Cannot be null.</param>
        /// <returns>An instance of <see cref="ApplicationPaymentRollupDto"/> containing the aggregated total paid and pending
        /// amounts for the main application and its child applications.</returns>
        public async Task<ApplicationPaymentRollupDto> GetApplicationPaymentRollupAsync(Guid applicationId, List<Guid> childApplicationIds)
        {
            var allCorrelationIds = new List<Guid> { applicationId };
            allCorrelationIds.AddRange(childApplicationIds);

            var batchRollup = await paymentRequestRepository.GetBatchPaymentRollupsByCorrelationIdsAsync(allCorrelationIds);

            return new ApplicationPaymentRollupDto
            {
                ApplicationId = applicationId,
                TotalPaid = batchRollup.Sum(s => s.TotalPaid),
                TotalPending = batchRollup.Sum(s => s.TotalPending)
            };
        }

        /// <summary>
        /// Retrieves batch payment rollup information for the specified application IDs, aggregating totals
        /// for both paid and pending amounts from parent and child applications.
        /// </summary>
        /// <remarks>This method performs a single database query to efficiently aggregate payment data
        /// for all specified parent and child applications. The results include all relevant payment information
        /// for each application ID provided.</remarks>
        /// <param name="applicationIds">A list of unique identifiers for the applications whose payment rollups are to be retrieved. Cannot be
        /// null or empty.</param>
        /// <param name="childApplicationIdsByParent">A dictionary that maps each parent application ID to a list of its child application IDs. Must not be null
        /// and should contain valid GUIDs.</param>
        /// <returns>A dictionary where each key is an application ID and the value is an <see cref="ApplicationPaymentRollupDto"/> containing
        /// the total paid and pending amounts for that application, including amounts from any child applications.</returns>
        public async Task<Dictionary<Guid, ApplicationPaymentRollupDto>> GetApplicationPaymentRollupBatchAsync(
            List<Guid> applicationIds,
            Dictionary<Guid, List<Guid>> childApplicationIdsByParent)
        {
            // Collect all unique correlation IDs (parents + all children) for a single DB query
            var allCorrelationIds = new HashSet<Guid>(applicationIds);
            foreach (var childIds in childApplicationIdsByParent.Values)
            {
                foreach (var childId in childIds)
                {
                    allCorrelationIds.Add(childId);
                }
            }

            var paymentRollups = await paymentRequestRepository.GetBatchPaymentRollupsByCorrelationIdsAsync(allCorrelationIds.ToList());
            var rollupLookup = paymentRollups.ToDictionary(s => s.ApplicationId);

            var result = new Dictionary<Guid, ApplicationPaymentRollupDto>();
            foreach (var applicationId in applicationIds)
            {
                decimal totalPaid = 0;
                decimal totalPending = 0;

                // Add the parent application's own amounts
                if (rollupLookup.TryGetValue(applicationId, out var parentRollup))
                {
                    totalPaid += parentRollup.TotalPaid;
                    totalPending += parentRollup.TotalPending;
                }

                // Add child application amounts
                if (childApplicationIdsByParent.TryGetValue(applicationId, out var childIds))
                {
                    foreach (var childId in childIds)
                    {
                        if (rollupLookup.TryGetValue(childId, out var childApplicationRollup))
                        {
                            totalPaid += childApplicationRollup.TotalPaid;
                            totalPending += childApplicationRollup.TotalPending;
                        }
                    }
                }

                result[applicationId] = new ApplicationPaymentRollupDto
                {
                    ApplicationId = applicationId,
                    TotalPaid = totalPaid,
                    TotalPending = totalPending
                };
            }

            return result;
        }
    }
}
