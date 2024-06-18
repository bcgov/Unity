using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unity.Payments.Domain.Shared
{
    public interface IPermissionCheckerService
    {
        Task<PermissionResult> CheckPermissionsAsync(string[] permissions);
    }
}
