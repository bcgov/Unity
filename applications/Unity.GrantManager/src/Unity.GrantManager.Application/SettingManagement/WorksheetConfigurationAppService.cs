using System;
using System.Threading.Tasks;
using Unity.Flex.Worksheets;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.SettingManagement;

public class WorksheetConfigurationAppService(
    IWorksheetAppService worksheetAppService,
    IApplicationFormVersionRepository formVersionRepository,
    IApplicationFormRepository formRepository
) : GrantManagerAppService, IWorksheetConfigurationAppService
{
    public async Task<WorksheetDeletionCheckDto> GetDeletionCheckAsync(Guid worksheetId)
    {
        var linkedForms = await worksheetAppService.GetLinkedFormsAsync(worksheetId);
        var result = new WorksheetDeletionCheckDto();

        foreach (var formVersionId in linkedForms.FormVersionIdsWithInstances)
        {
            var formName = await ResolveFormNameAsync(formVersionId);
            result.BlockingFormNames.Add(formName);
        }

        foreach (var formVersionId in linkedForms.LinkedFormVersionIds)
        {
            var formName = await ResolveFormNameAsync(formVersionId);
            result.LinkedFormNames.Add(formName);
        }

        return result;
    }

    private async Task<string> ResolveFormNameAsync(Guid formVersionId)
    {
        var formVersion = await formVersionRepository.FindAsync(formVersionId);
        if (formVersion == null) return formVersionId.ToString();

        var form = await formRepository.FindAsync(formVersion.ApplicationFormId);
        if (form == null) return formVersionId.ToString();

        return formVersion.Version.HasValue
            ? $"{form.ApplicationFormName} – v{formVersion.Version}"
            : $"{form.ApplicationFormName}";
    }
}
