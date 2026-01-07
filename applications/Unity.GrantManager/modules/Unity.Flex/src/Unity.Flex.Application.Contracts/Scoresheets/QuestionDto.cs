using System;
using System.Text.Json;
using Unity.Flex.Scoresheets.Enums;
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
        public virtual QuestionType Type { get; set; }
        public virtual uint Order { get; set; }

        public virtual Guid SectionId { get; }

        public virtual string? Answer { get; set; }
        public virtual string? Definition { get; set; } = "{}";
        public virtual bool IsHumanConfirmed { get; set; } = true;
        public virtual string? AICitation { get; set; }
        public virtual int? AIConfidence { get; set; }

        public string? GetMin()
        {
            try
            {
                var def = JsonSerializer.Deserialize<NumericDefinition>(Definition ?? "{}");
                if (def?.Min == null)
                    return null;
                // Only allow Int64 values
                if (long.TryParse(def.Min.ToString(), out var minValue))
                    return minValue.ToString();
                return null;
            }
            catch
            {
                return null;
            }
        }

        public string? GetMax()
        {
            try
            {
                var def = JsonSerializer.Deserialize<NumericDefinition>(Definition ?? "{}");
                if (def?.Max == null)
                    return null;
                // Only allow Int64 values
                if (long.TryParse(def.Max.ToString(), out var maxValue))
                    return maxValue.ToString();
                return null;
            }
            catch
            {
                return null;
            }
        }

        public string? GetMinLength()
        {
            try
            {
                var def = JsonSerializer.Deserialize<TextDefinition>(Definition ?? "{}");
                if (def?.MinLength == null)
                    return null;
                // Only allow Int64 values
                if (long.TryParse(def.MinLength.ToString(), out var minLength))
                    return minLength.ToString();
                return null;
            }
            catch
            {
                return null;
            }
        }

        public string? GetMaxLength()
        {
            try
            {
                var def = JsonSerializer.Deserialize<TextDefinition>(Definition ?? "{}");
                if (def?.MaxLength == null)
                    return null;
                // Only allow Int64 values
                if (long.TryParse(def.MaxLength.ToString(), out var maxLength))
                    return maxLength.ToString();
                return null;
            }
            catch
            {
                return null;
            }
        }

        public string? GetYesValue()
        {
            return JsonSerializer.Deserialize<QuestionYesNoDefinition>(Definition ?? "{}")?.YesValue.ToString();
        }

        public string? GetNoValue()
        {
            return JsonSerializer.Deserialize<QuestionYesNoDefinition>(Definition ?? "{}")?.NoValue.ToString();
        }

        public string? GetIsRequiredValue()
        {
            return JsonSerializer.Deserialize<CustomFieldDefinition>(Definition ?? "{}")?.Required.ToString();
        }

        public uint? GetRowsValue()
        {
            return JsonSerializer.Deserialize<TextAreaDefinition>(Definition ?? "{}")?.Rows;
        }
    }
}