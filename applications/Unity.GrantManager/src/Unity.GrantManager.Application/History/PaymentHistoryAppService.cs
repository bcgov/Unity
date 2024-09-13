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
            var entityChanges = await extendedAuditLogRepository.GetEntityChangeByTypeWithUsernameAsync(
                entityId,
                HistoryConsts.PaymentEntityTypeFullNames,
                cancellationToken
            );

            foreach (var entityChange in entityChanges)
            {
                foreach (var propertyChange in entityChange.EntityChange.PropertyChanges)
                {
                    string origninalValue = CleanValue(propertyChange.OriginalValue);
                    string newValue = CleanValue(propertyChange.NewValue);
                    HistoryDto historyDto = new()
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
