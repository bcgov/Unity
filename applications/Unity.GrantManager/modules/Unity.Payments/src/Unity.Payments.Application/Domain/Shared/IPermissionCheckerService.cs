using System.Threading.Tasks;

namespace Unity.Payments.Domain.Shared
{
    public interface IPermissionCheckerService
    {
        Task<PermissionResult> CheckPermissionsAsync(string[] permissions);
        Task<bool> IsGrantedAsync(string permission);
    }
}
