using System;

namespace Unity.GrantManager.GrantApplications
{
    public class GrantApplicationAssigneeDto
    {
        // TODO: flesh this out to the user tables and the datamodel and entities etc..        
        public Guid UserId { get; set; }
        public string Username { get; set; }
    }
}
