using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.Auditing;
using Volo.Abp.AuditLogging;
using Unity.GrantManager.History;
using System.Threading;
using Unity.GrantManager.Applications;
using Volo.Abp.Identity;

namespace Unity.GrantManager.Web.Views.Shared.Components.HistoryWidget
{
    [Widget(
        RefreshUrl = "Widgets/History/RefreshHistory",
        ScriptTypes = [typeof(HistoryWidgetScriptBundleContributor)],
        StyleTypes = [typeof(HistoryWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class HistoryWidgetViewComponent : AbpViewComponent
    {  
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IdentityUserAppService _identityUserAppService;
        private readonly IApplicationStatusRepository _applicationStatusRepository;

        public HistoryWidgetViewComponent(IAuditLogRepository auditLogRepository,
                                          IdentityUserAppService identityUserAppService,
                                          IApplicationStatusRepository applicationStatusRepository)
        {
            _identityUserAppService = identityUserAppService;
            _applicationStatusRepository = applicationStatusRepository;
            _auditLogRepository = auditLogRepository;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {
            string? entityId = applicationId.ToString();
            Dictionary<string, string> applicationStatusDict = await GetApplicationStatusDict();

            HistoryWidgetViewModel model = new()
            {       
                ApplicationStatusHistoryList = await GetHistoryList(entityId, "ApplicationStatusId", applicationStatusDict),
            };                 

            return View(model);
        }

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

        public async Task<Dictionary<string, string>> GetApplicationStatusDict() {
            List<ApplicationStatus> applicationStatusList = await _applicationStatusRepository.GetListAsync();
            Dictionary<string, string> applicationStatusDict = new Dictionary<string, string>();
            foreach (var applicationStatus in applicationStatusList)
            {
                applicationStatusDict.Add(applicationStatus.Id.ToString(), applicationStatus.InternalStatus);
            }
            return applicationStatusDict;
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

    public class HistoryWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
            .AddIfNotContains("/Views/Shared/Components/HistoryWidget/Default.css");
        }
    }

    public class HistoryWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
            .AddIfNotContains("/Views/Shared/Components/HistoryWidget/Default.js");
            context.Files
            .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}


