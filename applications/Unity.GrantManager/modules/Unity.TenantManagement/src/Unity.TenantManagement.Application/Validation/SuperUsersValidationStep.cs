using System;
using System.Linq;
using System.Threading.Tasks;
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

    internal static string[] ParseEmails(string superUsers) =>
        [.. superUsers.Split([',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(e => e.Contains('@'))];
}
