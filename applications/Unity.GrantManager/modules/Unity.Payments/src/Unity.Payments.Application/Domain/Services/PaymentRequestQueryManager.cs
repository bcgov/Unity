using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        IObjectMapper objectMapper) : DomainService, IPaymentRequestQueryManager
    {
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

        public async Task<List<PaymentRequest>> GetPagedPaymentRequestsWithIncludesAsync(int skipCount, int maxResultCount, string sorting)
        {
            await paymentRequestRepository.GetPagedListAsync(skipCount, maxResultCount, sorting, includeDetails: true);

            var paymentsQueryable = await paymentRequestRepository.GetQueryableAsync();
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
            var paymentWithIncludes = await paymentsQueryable
                .Include(pr => pr.AccountCoding)
                .Include(pr => pr.PaymentTags)
                    .ThenInclude(pt => pt.Tag)
                .ToListAsync();
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
            var payments = await paymentsQueryable.Include(pr => pr.Site).ToListAsync();
            var filteredPayments = payments.Where(pr => applicationIds.Contains(pr.CorrelationId)).ToList();

            return objectMapper.Map<List<PaymentRequest>, List<PaymentDetailsDto>>(filteredPayments);
        }

        public async Task<List<PaymentDetailsDto>> GetListByApplicationIdAsync(Guid applicationId)
        {
            var paymentsQueryable = await paymentRequestRepository.GetQueryableAsync();
            var payments = await paymentsQueryable.Include(pr => pr.Site).ToListAsync();
            var filteredPayments = payments.Where(e => e.CorrelationId == applicationId).ToList();

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

        public async Task<List<PaymentRequestDto>> MapToDtoAndLoadDetailsAsync(List<PaymentRequest> paymentsList)
        {
            var paymentDtos = objectMapper.Map<List<PaymentRequest>, List<PaymentRequestDto>>(paymentsList);

            // Flatten all DecisionUserIds from ExpenseApprovals across all PaymentRequestDtos
            List<Guid> paymentRequesterIds = [.. paymentDtos
                .Select(payment => payment.CreatorId)
                .OfType<Guid>()
                .Distinct()];

            List<Guid> expenseApprovalCreatorIds = [.. paymentDtos
                .SelectMany(payment => payment.ExpenseApprovals)
                .Where(expenseApproval => expenseApproval.Status != ExpenseApprovalStatus.Requested)
                .Select(expenseApproval => expenseApproval.DecisionUserId)
                .OfType<Guid>()
                .Distinct()];

            // Call external lookup for each distinct User Id and store in a dictionary.
            var userDictionary = new Dictionary<Guid, PaymentUserDto>();
            var allUserIds = paymentRequesterIds.Concat(expenseApprovalCreatorIds).Distinct();
            foreach (var userId in allUserIds)
            {
                var userInfo = await externalUserLookupServiceProvider.FindByIdAsync(userId);
                if (userInfo != null)
                {
                    userDictionary[userId] = objectMapper.Map<IUserData, PaymentUserDto>(userInfo);
                }
            }

            // Map UserInfo details to each ExpenseApprovalDto
            foreach (var paymentRequestDto in paymentDtos)
            {
                if (paymentRequestDto.CreatorId.HasValue
                        && userDictionary.TryGetValue(paymentRequestDto.CreatorId.Value, out var paymentRequestUserDto))
                {
                    paymentRequestDto.CreatorUser = paymentRequestUserDto;
                }

                if (paymentRequestDto.AccountCoding != null)
                {
                    paymentRequestDto.AccountCodingDisplay = await GetAccountDistributionCodeAsync(paymentRequestDto.AccountCoding);
                }

                if (paymentRequestDto.ExpenseApprovals != null)
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
                    Site site = await siteRepository.GetAsync(paymentRequest.SiteId);
                    paymentRequestDto.Site = objectMapper.Map<Site, SiteDto>(site);
                    paymentRequestDtos.Add(paymentRequestDto);
                }
            }
            await casPaymentRequestCoordinator.ManuallyAddPaymentRequestsToReconciliationQueue(paymentRequestDtos);
        }
    }
}
