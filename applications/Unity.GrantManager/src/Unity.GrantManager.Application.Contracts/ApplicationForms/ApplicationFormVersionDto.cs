﻿using System;
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
    }
}
