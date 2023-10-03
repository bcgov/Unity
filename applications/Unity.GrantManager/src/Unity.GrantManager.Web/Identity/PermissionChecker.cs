using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Claims;
using Volo.Abp.SimpleStateChecking;

namespace Unity.GrantManager.Web.Identity
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(PermissionChecker), typeof(IPermissionChecker))]
    public class PermissionChecker : IPermissionChecker, ITransientDependency
    {
        protected IPermissionDefinitionManager PermissionDefinitionManager { get; }
        protected ICurrentPrincipalAccessor PrincipalAccessor { get; }
        protected ICurrentTenant CurrentTenant { get; }
        protected IPermissionValueProviderManager PermissionValueProviderManager { get; }
        protected ISimpleStateCheckerManager<PermissionDefinition> StateCheckerManager { get; }

        public PermissionChecker(
            ICurrentPrincipalAccessor principalAccessor,
            IPermissionDefinitionManager permissionDefinitionManager,
            ICurrentTenant currentTenant,
            IPermissionValueProviderManager permissionValueProviderManager,
            ISimpleStateCheckerManager<PermissionDefinition> stateCheckerManager)
        {
            PrincipalAccessor = principalAccessor;
            PermissionDefinitionManager = permissionDefinitionManager;
            CurrentTenant = currentTenant;
            PermissionValueProviderManager = permissionValueProviderManager;
            StateCheckerManager = stateCheckerManager;
        }

        public virtual async Task<bool> IsGrantedAsync(string name)
        {
            return await IsGrantedAsync(PrincipalAccessor.Principal, name);
        }

        public virtual async Task<bool> IsGrantedAsync(
            ClaimsPrincipal? claimsPrincipal,
            string name)
        {
            Check.NotNull(name, nameof(name));

            var permission = await PermissionDefinitionManager.GetOrNullAsync(name);
            if (permission == null)
            {
                return false;
            }

            if (!permission.IsEnabled)
            {
                return false;
            }

            if (!await StateCheckerManager.IsEnabledAsync(permission))
            {
                return false;
            }

            var multiTenancySide = claimsPrincipal?.GetMultiTenancySide()
                                   ?? CurrentTenant.GetMultiTenancySide();

            if (!permission.MultiTenancySide.HasFlag(multiTenancySide))
            {
                return false;
            }

            var isGranted = false;

            if (claimsPrincipal != null
                && claimsPrincipal.Claims.Any(s => s.Type == "Permission" && s.Value == name))
            {
                isGranted = true;
            }

            return isGranted;
        }

        public async Task<MultiplePermissionGrantResult> IsGrantedAsync(string[] names)
        {
            return await IsGrantedAsync(PrincipalAccessor.Principal, names);
        }

        public async Task<MultiplePermissionGrantResult> IsGrantedAsync(ClaimsPrincipal? claimsPrincipal, string[] names)
        {
            Check.NotNull(names, nameof(names));

            var result = new MultiplePermissionGrantResult();
            if (!names.Any())
            {
                return result;
            }

            if (claimsPrincipal != null)
            {
                var permissions = claimsPrincipal.Claims.Where(s => s.Type == "Permission");
                foreach (var name in names)
                {
                    if (permissions.Select(s => s.Value).Contains(name))
                    {
                        result.Result.Add(name, PermissionGrantResult.Granted);
                    } 
                    else
                    {
                        result.Result.Add(name, PermissionGrantResult.Prohibited);
                    }
                }
            }       

            return await Task.FromResult(result);
        }
    }
}