using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Domain.ChangeTracking;

namespace Unity.GrantManager.History
{
    public class PaymentHistoryAppService(IExtendedAuditLogRepository extendedAuditLogRepository) : GrantManagerAppService, IPaymentHistoryAppService
    {
        [DisableEntityChangeTracking]
        public async Task<List<HistoryDto>> GetPaymentHistoryList(Guid? entityId)
        {
            List<HistoryDto> historyList = [];
            CancellationToken cancellationToken = default;
            
            if (entityId == null || entityId == Guid.Empty)
            {
                return historyList;
            }
            
            var entityChanges = await extendedAuditLogRepository.GetEntityChangeByTypeWithUsernameAsync(
                entityId,
                HistoryConsts.PaymentEntityTypeFullNames,
                cancellationToken
            );

            foreach (var entityChange in entityChanges)
            {
                // Add explicit filter to ensure only matching entityId records
                if (entityChange.EntityChange.EntityId != entityId.ToString())
                {
                    continue;
                }
                
                foreach (var propertyChange in entityChange.EntityChange.PropertyChanges)
                {
                    string origninalValue = CleanValue(propertyChange.OriginalValue);
                    string newValue = CleanValue(propertyChange.NewValue);
                    string displayNewValue = MapFsbToDisplayText(newValue);

                    // Don't display history if both original and new values are empty, as it doesn't provide useful information and may clutter the history with irrelevant entries.    
                    if (string.IsNullOrEmpty(origninalValue) && string.IsNullOrEmpty(newValue))
                    {
                        continue;
                    }
                    int changeType = (int)entityChange.EntityChange.ChangeType; 
                    DateTime utcDateTime = DateTime.SpecifyKind(entityChange.EntityChange.ChangeTime, DateTimeKind.Utc);
                    HistoryDto historyDto = new()
                    {
                        EntityName = GetShortEntityName(entityChange.EntityChange.EntityTypeFullName),
                        PropertyName = propertyChange.PropertyName,
                        OriginalValue = origninalValue,
                        NewValue = displayNewValue,
                        ChangeTime = utcDateTime.ToLocalTime(),
                        UserName = entityChange.UserName,
                        ChangeType = changeType 
                    };
                    historyList.Add(historyDto);
                }
            }
            return historyList;
        }

        private static string MapFsbToDisplayText(string value)
        {
            return value == "FSB" ? "Sent to Account Payable" : value;
        }

        private static string GetShortEntityName(string fullEntityName)
        {
            string pattern = @"[^.]+$";
            string shortEntityName = "";
            Regex myRegex = new(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(30));

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
