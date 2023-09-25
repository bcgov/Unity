using System;

namespace Unity.GrantManager.GrantApplications
{
    public class GrantApplicationAssigneeDto
    {
        public Guid Id { get; set; }
        public string AssigneeDisplayName { get; set; }
        public string OidcSub { get; set; }
    }
}
