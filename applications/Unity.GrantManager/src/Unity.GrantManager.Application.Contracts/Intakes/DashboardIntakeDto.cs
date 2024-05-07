using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Intakes
{
    public class DashboardIntakeDto 
    {
        public Guid IntakeId { get; set; }

        public string IntakeName { get; set; } = string.Empty;
        public List<string>? Categories { get; set; }            
    }
}
