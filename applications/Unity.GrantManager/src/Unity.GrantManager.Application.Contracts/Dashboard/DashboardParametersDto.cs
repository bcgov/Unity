using System;

namespace Unity.GrantManager.Dashboard
{
    public class DashboardParametersDto
    {
        public Guid[] IntakeIds { get; set; } = Array.Empty<Guid>();
        public string[] Categories { get; set; } = Array.Empty<string>();
        public string[] StatusCodes { get; set; } = Array.Empty<string>();
        public string?[] Substatus { get; set; } = Array.Empty<string>();
        public string?[] Tags { get; set; } = Array.Empty<string>();
        public string[] Assignees { get; set; } = Array.Empty<string>();
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}
