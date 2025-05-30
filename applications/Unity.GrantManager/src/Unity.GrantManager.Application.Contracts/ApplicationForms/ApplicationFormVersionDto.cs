using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.ApplicationForms
{
    public class ApplicationFormVersionDto : EntityDto<Guid>
    {
        public Guid ApplicationFormId { get; set; }
        public string? ChefsApplicationFormGuid { get; set; }
        public string? ChefsFormVersionGuid { get; set; }
        public string? SubmissionHeaderMapping { get; set; }
        public string? AvailableChefsFields { get; set; } = "{}";
        public int? Version { get; set; }
        public bool Published { get; set; }
        public string ReportColumns { get; set; } = string.Empty;
        public string ReportKeys { get; set; } = string.Empty;
        public string ReportViewName { get; set; } = string.Empty;
        [Column(TypeName = "jsonb")]
        public string? FormSchema { get; set; } = string.Empty;
    }
}
