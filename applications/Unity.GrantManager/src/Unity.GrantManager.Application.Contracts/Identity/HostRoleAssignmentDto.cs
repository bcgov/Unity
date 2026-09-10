using System;
using System.Collections.Generic;

namespace Unity.GrantManager.Identity
{
    public class HostRoleAssignmentDto
    {
        public Guid Id { get; set; }
        public string? Username { get; set; }
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public List<string> RoleNames { get; set; } = [];
    }
}
