using System;
using System.Collections.Generic;

namespace Unity.GrantManager.Intakes
{
    public class DashboardIntakeDto 
    {
        public Guid IntakeId { get; set; }

        public string IntakeName { get; set; } = string.Empty;
        public List<string>? Categories { get; set; }            
    }
}
