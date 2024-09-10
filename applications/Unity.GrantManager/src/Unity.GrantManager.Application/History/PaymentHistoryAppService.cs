using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Domain.ChangeTracking;
using Volo.Abp.Identity;

namespace Unity.GrantManager.History
{
    public class PaymentHistoryAppService : GrantManagerAppService, IPaymentHistoryAppService
    {
        private readonly IEfCoreAuditLogRepository _auditLogRepository;
        private const string ExpenseApprovalObject = "Unity.Payments.Domain.PaymentRequests.ExpenseApproval";
        private const string PaymentRequestObject = "Unity.Payments.Domain.PaymentRequests.PaymentRequest";
        private readonly List<string> entityTypeFullNames = new()
            {
                ExpenseApprovalObject,
                PaymentRequestObject,
            };

        public PaymentHistoryAppService(
            IEfCoreAuditLogRepository auditLogRepository
        )
        {
            _auditLogRepository = auditLogRepository;
        }

        [DisableEntityChangeTracking]
        public async Task<List<HistoryDto>> GetPaymentHistoryList(Guid? entityId)
        {
            List<HistoryDto> historyList = new List<HistoryDto>();
            CancellationToken cancellationToken = default;
            var entityChanges = await _auditLogRepository.GetEntityChangeByTypeWithUsernameAsync(
                entityId,
                entityTypeFullNames,
                cancellationToken
            );

            foreach (var entityChange in entityChanges)
            {
                foreach (var propertyChange in entityChange.EntityChange.PropertyChanges)
                {
                    string origninalValue = CleanValue(propertyChange.OriginalValue);
                    string newValue = CleanValue(propertyChange.NewValue);
                    HistoryDto historyDto = new HistoryDto()
                    {
                        EntityName = GetShortEntityName(entityChange.EntityChange.EntityTypeFullName),
                        PropertyName = propertyChange.PropertyName, // The name of the property on the entity class.
                        OriginalValue = origninalValue,
                        NewValue = newValue,
                        ChangeTime = entityChange.EntityChange.ChangeTime,
                        UserName = entityChange.UserName
                    };
                    historyList.Add(historyDto);
                }
            }
            return historyList;
        }

        private static string GetShortEntityName(string fullEntityName)
        {
            string pattern = @"[^.]+$";
            string shortEntityName = "";
            Regex myRegex = new Regex(pattern, RegexOptions.IgnoreCase);

            Match m = myRegex.Match(fullEntityName);   // m is the first match
            if (m.Success)
            {
                shortEntityName = m.Value;
            }

            return shortEntityName;
        }

        private static string CleanValue(string? value)
        {
            return value?.Replace("\"", "") ?? "";
        }   
    }
}
