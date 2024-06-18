using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unity.Payments.Domain.Shared
{
    public class PermissionResult
    {
        private readonly Dictionary<string, bool> _permissions = new Dictionary<string, bool>();

        public void SetPermission(string permission, bool isGranted)
        {
            _permissions[permission] = isGranted;
        }

        public bool HasPermission(string permission)
        {
            return _permissions.TryGetValue(permission, out var isGranted) && isGranted;
        }

        public Dictionary<string, bool> GetPermissions()
        {
            return _permissions;
        }
    }
}
