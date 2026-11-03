using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;

public class UserPreferencesAppService(
            ISettingManager settingManager,
            ISettingProvider settingProvider) : ApplicationService
{
    public const string EmailBlockCommentTagging = "GrantManager.UserEmailSettings.EmailBlockCommentTagging";
    public const string EmailBlockAssigned = "GrantManager.UserEmailSettings.EmailBlockAssigned";

    public async Task<UserPreferencesDto> GetAsync()
    {
        return new UserPreferencesDto
        {
            EmailBlockCommentTagging = bool.TryParse(await settingProvider.GetOrNullAsync(
               EmailBlockCommentTagging), out var emailBlockCommentTagging) ? emailBlockCommentTagging : false,

            EmailBlockAssigned = bool.TryParse(await settingProvider.GetOrNullAsync(
               EmailBlockAssigned), out var emailBlockAssigned) ? emailBlockAssigned : false
        };
    }

    public async Task UpdateAsync(UserPreferencesDto input)
    {
        await settingManager.SetForCurrentUserAsync(
            EmailBlockCommentTagging,
            input.EmailBlockCommentTagging.ToString());

        await settingManager.SetForCurrentUserAsync(
            EmailBlockAssigned,
            input.EmailBlockAssigned.ToString());
    }
}