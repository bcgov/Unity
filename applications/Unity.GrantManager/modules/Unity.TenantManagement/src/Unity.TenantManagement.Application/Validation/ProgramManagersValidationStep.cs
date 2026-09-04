using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.Worksheets.Values;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Unity.TenantManagement.Validation;

[RemoteService(false)]
[ExposeServices(typeof(IOnboardingValidationStep))]
public class ProgramManagersValidationStep(IOnboardingUserLookup userLookup)
    : IOnboardingValidationStep, ITransientDependency
{
    public int Order => 20;
    public string StepName => "Program Managers";

    public async Task<OnboardingValidationStepResult> ValidateAsync(OnboardingRequestDto request)
    {
        var emails = ParseEmails(request.ProgramManagers);
        if (emails.Length == 0)
            return OnboardingValidationStepResult.Failure("No program manager email addresses specified.");

        foreach (var email in emails)
        {
            var guid = await userLookup.FindUserGuidByEmailAsync(email);
            if (!string.IsNullOrWhiteSpace(guid))
                return OnboardingValidationStepResult.Success();
        }

        return OnboardingValidationStepResult.Failure(
            "None of the specified program manager email addresses could be found in the directory.");
    }

    internal static string[] ParseEmails(string programManagers)
    {
        var dataGridEmails = ParseDataGridEmails(programManagers);
        if (dataGridEmails.Length > 0)
            return dataGridEmails;

        return [.. programManagers.Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(e => e.Contains('@'))];
    }

    // Formio/CHEFS "Program Managers" fields are submitted as a DataGrid: one row per program
    // manager, with columns such as name/email/title. The email column's key varies per worksheet
    // (e.g. "s03_SuperUserEmail" on forms authored before this terminology changed), so it's
    // matched by name rather than a fixed key.
    private static string[] ParseDataGridEmails(string programManagers)
    {
        DataGridRowsValue grid;
        try
        {
            grid = JsonSerializer.Deserialize<DataGridRowsValue>(programManagers);
        }
        catch (JsonException)
        {
            return [];
        }

        if (grid?.Rows is not { Count: > 0 }) return [];

        return [.. grid.Rows
            .Select(r => r.Cells.FirstOrDefault(c => c.Key.Contains("email", StringComparison.OrdinalIgnoreCase))?.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v) && v.Contains('@'))
            .Select(v => v!.Trim())];
    }
}
