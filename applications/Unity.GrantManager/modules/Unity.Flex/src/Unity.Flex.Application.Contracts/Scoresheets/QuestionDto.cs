using System;
using System.Text.Json;
using Unity.Flex.Worksheets.Definitions;
using Volo.Abp.Application.Dtos;

namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class QuestionDto : ExtensibleEntityDto<Guid>
    {
        public virtual string Name { get; set; } = string.Empty;
        public virtual string Label { get; set; } = string.Empty;
        public virtual string? Description { get; set; }
        public virtual bool Enabled { get; private set; }
        public virtual QuestionType Type { get; set; }
        public virtual uint Order { get; set; }

        public virtual Guid SectionId { get; }

        public virtual string? Answer { get; set; }
        public virtual string? Definition { get; set; } = "{}";

        public string? GetMin()
        {
            return JsonSerializer.Deserialize<NumericDefinition>(Definition ?? "{}")?.Min.ToString();
        }

        public string? GetMax()
        {
            return JsonSerializer.Deserialize<NumericDefinition>(Definition ?? "{}")?.Max.ToString();
        }

        public string? GetMinLength()
        {
            return JsonSerializer.Deserialize<TextDefinition>(Definition ?? "{}")?.MinLength.ToString();
        }

        public string? GetMaxLength()
        {
            return JsonSerializer.Deserialize<TextDefinition>(Definition ?? "{}")?.MaxLength.ToString();
        }

        public string? GetYesValue()
        {
            return JsonSerializer.Deserialize<QuestionYesNoDefinition>(Definition ?? "{}")?.YesValue.ToString();
        }

        public string? GetNoValue()
        {
            return JsonSerializer.Deserialize<QuestionYesNoDefinition>(Definition ?? "{}")?.NoValue.ToString();
        }
    }
}