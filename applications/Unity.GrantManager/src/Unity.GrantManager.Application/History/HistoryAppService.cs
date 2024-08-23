using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
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
        private readonly IApplicationStatusRepository _applicationStatusRepository;

        public HistoryAppService(IAuditLogRepository auditLogRepository,
                                          IdentityUserAppService identityUserAppService,
                                          IApplicationStatusRepository applicationStatusRepository)
        {
            _identityUserAppService = identityUserAppService;
            _applicationStatusRepository = applicationStatusRepository;
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
                        string origninalValue = propertyChange.OriginalValue != null ? propertyChange.OriginalValue.Replace("\"", "") : "";
                        string newValue = propertyChange.NewValue != null ? propertyChange.NewValue.Replace("\"", "") : "";
                        HistoryDto historyDto = new HistoryDto()
                        {
                            OriginalValue = lookupDictionary != null ? lookupDictionary[origninalValue] : origninalValue,
                            NewValue = lookupDictionary != null ? lookupDictionary[newValue] : newValue,
                            ChangeTime = entityChange.ChangeTime,
                            UserName = await LookupUserName(entityChange.AuditLogId)
                        };
                        historyList.Add(historyDto);
                    }
                }
            }
            return historyList;
        }     

        public async Task<string> LookupUserName(Guid auditLogId)
        {
            string userName = "";
            AuditLog auditLog = await _auditLogRepository.GetAsync(auditLogId);
            if (auditLog != null && auditLog.UserId != null && auditLog.UserId != Guid.Empty)
            {       
                string? userIdStr = auditLog.UserId.ToString();
                if(Guid.TryParse(userIdStr, out Guid userId)) {
                    var user = await _identityUserAppService.GetAsync(userId);
                    userName = $"{user.Name} {user.Surname}";
                }
            }
            
            return userName;
        }   
    }
}
