using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Integrations.Css;
using Unity.TenantManagement;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Identity;

[RemoteService(false)]
[ExposeServices(typeof(IOnboardingUserLookup))]
public class CssOnboardingUserLookup(ICssUsersApiService cssUsersApiService)
    : IOnboardingUserLookup, ITransientDependency
{
    public async Task<string?> FindUserGuidByEmailAsync(string email)
    {
        var result = await cssUsersApiService.SearchUsersAsync("idir", email: email);
        if (!result.Success || result.Data == null || result.Data.Length == 0)
            return null;

        var guid = result.Data[0].Attributes?.IdirUserGuid?.FirstOrDefault();
        return string.IsNullOrWhiteSpace(guid) ? null : guid;
    }
}
