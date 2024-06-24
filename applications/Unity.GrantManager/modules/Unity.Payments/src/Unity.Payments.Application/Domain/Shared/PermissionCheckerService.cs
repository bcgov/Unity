using System.Threading.Tasks;
using Volo.Abp.Authorization.Permissions;

namespace Unity.Payments.Domain.Shared
{
    public class PermissionCheckerService : PaymentsAppService, IPermissionCheckerService
    {
        private readonly IPermissionChecker _permissionChecker;
        public PermissionCheckerService(IPermissionChecker permissionChecker)
        {
            _permissionChecker = permissionChecker;
        }


        public virtual async Task<PermissionResult> CheckPermissionsAsync(params string[] permissions)
        {
            var permissionResult = new PermissionResult();


            foreach (var permission in permissions)
            {
                bool isGranted = await _permissionChecker.IsGrantedAsync(permission);
                permissionResult.SetPermission(permission, isGranted);
            }

            return permissionResult;
        }
    }
}
