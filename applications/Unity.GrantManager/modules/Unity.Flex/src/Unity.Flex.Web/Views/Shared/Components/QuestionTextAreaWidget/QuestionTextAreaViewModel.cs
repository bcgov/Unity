﻿using System;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionTextAreaWidget
{
    public class QuestionTextAreaViewModel : RequiredFieldViewModel
    {
        public Guid QuestionId { get; set; }
        public bool IsDisabled { get; set; }
        public string Answer { get; set; } = string.Empty;
        public string? MinLength { get; set; }
        public string? MaxLength { get; set; }
        public uint? Rows { get; set; } = 1;
    }
}
