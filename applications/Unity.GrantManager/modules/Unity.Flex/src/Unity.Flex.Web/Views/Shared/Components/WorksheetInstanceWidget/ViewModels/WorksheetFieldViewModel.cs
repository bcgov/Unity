using System;
using Unity.Flex.Worksheets;
using System.ComponentModel.DataAnnotations;

namespace Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels
{
    public class WorksheetFieldViewModel
    {
        [Required]
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public CustomFieldType Type { get; set; } = CustomFieldType.Undefined;
        public uint? Order { get; set; }
        public bool Enabled { get; set; } = true;
        public string? Definition { get; set; } = "{}";
        public string? CurrentValue { get; set; }
        public string UiAnchor { get; set; } = string.Empty;
    }
}