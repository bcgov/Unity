﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Auditing;
using Volo.Abp.AuditLogging;
using Volo.Abp.Domain.ChangeTracking;
using Volo.Abp.Identity;

namespace Unity.GrantManager.History
{
    public class HistoryAppService : GrantManagerAppService, IHistoryAppService
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IdentityUserAppService _identityUserAppService;


        public HistoryAppService(IAuditLogRepository auditLogRepository,
                                 IdentityUserAppService identityUserAppService)
        {
            _identityUserAppService = identityUserAppService;
            _auditLogRepository = auditLogRepository;
        }


        [DisableEntityChangeTracking]
        public async Task<List<HistoryDto>> GetHistoryList(string? entityId, 
                                                           string filterPropertyName, 
                                                           Dictionary<string, string>? lookupDictionary) {

            List<HistoryDto> historyList = new List<HistoryDto>();
            string? sorting = null;
            int maxResultCount = 50;
            int skipCount = 0;
            DateTime? startTime = null;
            DateTime? endTime = null;
            bool includeDetails = true;
            Guid? auditLogId = null;
            EntityChangeType? changeType = null;
            string? entityTypeFullName = null;
            CancellationToken cancellationToken = default;

            var entityChanges = await _auditLogRepository.GetEntityChangeListAsync(
                sorting,
                maxResultCount,
                skipCount,
                auditLogId,
                startTime, endTime,
                changeType,
                entityId,
                entityTypeFullName,
                includeDetails,
                cancellationToken);

            foreach (var entityChange in entityChanges)
            {
                foreach (var propertyChange in entityChange.PropertyChanges)
                {
                    if (propertyChange.PropertyName == filterPropertyName)
                    {
                        string origninalValue = CleanValue(propertyChange.OriginalValue);
                        string newValue = CleanValue(propertyChange.NewValue);
                        HistoryDto historyDto = new HistoryDto()
                        {
                            OriginalValue = GetLookupValue(origninalValue, lookupDictionary),
                            NewValue = GetLookupValue(newValue, lookupDictionary),
                            ChangeTime = entityChange.ChangeTime,
                            UserName = await LookupUserName(entityChange.AuditLogId)
                        };
                        historyList.Add(historyDto);
                    }
                }
            }
            return historyList;
        }

        private static string CleanValue(string? value)
        {
            return value?.Replace("\"", "") ?? "";
        }

        private static string GetLookupValue(string value, Dictionary<string, string>? lookupDictionary)
        {
            return lookupDictionary != null && lookupDictionary.TryGetValue(value, out var lookupValue)
                ? lookupValue
                : value;
        }

        public async Task<string> LookupUserName(Guid auditLogId)
        {
            var auditLog = await _auditLogRepository.GetAsync(auditLogId);
            if (auditLog?.UserId == null || auditLog.UserId == Guid.Empty)
            {
                return string.Empty;
            }

            var userId = auditLog.UserId.Value;
            var user = await _identityUserAppService.GetAsync(userId);

            return user != null ? $"{user.Name} {user.Surname}" : string.Empty;
        }
    }
}