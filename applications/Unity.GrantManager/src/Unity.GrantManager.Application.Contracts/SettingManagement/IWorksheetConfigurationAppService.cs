using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.SettingManagement;

public interface IWorksheetConfigurationAppService : IApplicationService
{
    Task<WorksheetDeletionCheckDto> GetDeletionCheckAsync(Guid worksheetId);
}
