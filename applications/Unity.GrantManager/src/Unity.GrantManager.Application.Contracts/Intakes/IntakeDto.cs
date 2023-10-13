using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Intakes
{
    public class IntakeDto : AuditedEntityDto<Guid>
    {
        public decimal Budget { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string IntakeName { get; set; } = string.Empty;
    }
}
