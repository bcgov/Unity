using System;
using System.ComponentModel;

namespace Unity.GrantManager.Intakes
{
    public class CreateUpdateIntakeDto
    {
        public decimal Budget { get; set; }

        [DisplayName("Common:StartDate")]        
        public DateTime StartDate { get; set; }

        [DisplayName("Common:EndDate")]
        public DateTime EndDate { get; set; }

        [DisplayName("Common:Name")]
        public string IntakeName { get; set; } = string.Empty;
    }
}
