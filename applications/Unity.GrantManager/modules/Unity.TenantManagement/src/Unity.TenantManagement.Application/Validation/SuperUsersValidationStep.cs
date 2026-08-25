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
public class SuperUsersValidationStep(IOnboardingUserLookup userLookup)
    : IOnboardingValidationStep, ITransientDependency
{
    public int Order => 20;
    public string StepName => "Super Users";

    public async Task<OnboardingValidationStepResult> ValidateAsync(OnboardingRequestDto request)
    {
        var emails = ParseEmails(request.SuperUsers);
        if (emails.Length == 0)
            return OnboardingValidationStepResult.Failure("No super user email addresses specified.");

        foreach (var email in emails)
        {
            var guid = await userLookup.FindUserGuidByEmailAsync(email);
            if (!string.IsNullOrWhiteSpace(guid))
                return OnboardingValidationStepResult.Success();
        }

        return OnboardingValidationStepResult.Failure(
            "None of the specified super user email addresses could be found in the directory.");
    }

    internal static string[] ParseEmails(string superUsers)
    {
        var dataGridEmails = ParseDataGridEmails(superUsers);
        if (dataGridEmails.Length > 0)
            return dataGridEmails;

        return [.. superUsers.Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(e => e.Contains('@'))];
    }

    // Formio/CHEFS "Super Users" fields are submitted as a DataGrid: one row per super user, with
    // columns such as name/email/title. The email column's key varies per worksheet (e.g.
    // "s03_SuperUserEmail"), so it's matched by name rather than a fixed key.
    private static string[] ParseDataGridEmails(string superUsers)
    {
        DataGridRowsValue grid;
        try
        {
            grid = JsonSerializer.Deserialize<DataGridRowsValue>(superUsers);
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
