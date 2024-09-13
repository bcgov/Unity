using System;
using System.Text.Json;
using Unity.Flex.Scoresheets;

namespace Unity.Flex.Worksheets.Definitions
{
    public static class DefinitionResolver
    {
        public static string Resolve(CustomFieldType type, object? definition)
        {
            if (definition == null)
            {
                return type switch
                {
                    CustomFieldType.Undefined => "{}",
                    CustomFieldType.BCAddress => "{}",
                    CustomFieldType.Numeric => JsonSerializer.Serialize(new NumericDefinition()),
                    CustomFieldType.Text => JsonSerializer.Serialize(new TextDefinition()),
                    CustomFieldType.Date => JsonSerializer.Serialize(new DateDefinition()),
                    CustomFieldType.DateTime => JsonSerializer.Serialize(new DateTimeDefinition()),
                    CustomFieldType.Currency => JsonSerializer.Serialize(new CurrencyDefinition()),
                    CustomFieldType.YesNo => JsonSerializer.Serialize(new YesNoDefinition()),
                    CustomFieldType.Email => JsonSerializer.Serialize(new EmailDefinition()),
                    CustomFieldType.Phone => JsonSerializer.Serialize(new PhoneDefinition()),
                    CustomFieldType.Radio => JsonSerializer.Serialize(new RadioDefinition()),
                    CustomFieldType.Checkbox => JsonSerializer.Serialize(new CheckboxDefinition()),
                    CustomFieldType.CheckboxGroup => JsonSerializer.Serialize(new CheckboxGroupDefinition()),
                    CustomFieldType.SelectList => JsonSerializer.Serialize(new SelectListDefinition()),
                    CustomFieldType.TextArea => JsonSerializer.Serialize(new TextAreaDefinition()),
                    _ => throw new NotImplementedException(),
                };
            }
            else if (definition is CustomFieldDefinition)
            {
                return type switch
                {
                    CustomFieldType.Undefined => "{}",
                    CustomFieldType.BCAddress => "{}",
                    CustomFieldType.Numeric => JsonSerializer.Serialize((NumericDefinition)definition),
                    CustomFieldType.Text => JsonSerializer.Serialize((TextDefinition)definition),
                    CustomFieldType.Date => JsonSerializer.Serialize((DateDefinition)definition),
                    CustomFieldType.DateTime => JsonSerializer.Serialize((DateTimeDefinition)definition),
                    CustomFieldType.Currency => JsonSerializer.Serialize((CurrencyDefinition)definition),
                    CustomFieldType.YesNo => JsonSerializer.Serialize((YesNoDefinition)definition),
                    CustomFieldType.Email => JsonSerializer.Serialize((EmailDefinition)definition),
                    CustomFieldType.Phone => JsonSerializer.Serialize((PhoneDefinition)definition),
                    CustomFieldType.Radio => JsonSerializer.Serialize((RadioDefinition)definition),
                    CustomFieldType.Checkbox => JsonSerializer.Serialize((CheckboxDefinition)definition),
                    CustomFieldType.CheckboxGroup => JsonSerializer.Serialize((CheckboxGroupDefinition)definition),
                    CustomFieldType.SelectList => JsonSerializer.Serialize((SelectListDefinition)definition),
                    CustomFieldType.TextArea => JsonSerializer.Serialize((TextAreaDefinition)definition),
                    _ => throw new NotImplementedException(),
                };
            }
            else if (definition is JsonElement element)
            {
                return type switch
                {
                    CustomFieldType.Undefined => "{}",
                    CustomFieldType.BCAddress => "{}",
                    CustomFieldType.Numeric => JsonSerializer.Serialize(element.ToString()),
                    CustomFieldType.Text => JsonSerializer.Serialize(element.ToString()),
                    CustomFieldType.Date => JsonSerializer.Serialize(element.ToString()),
                    CustomFieldType.DateTime => JsonSerializer.Serialize(element.ToString()),
                    CustomFieldType.Currency => JsonSerializer.Serialize(element.ToString()),
                    CustomFieldType.YesNo => JsonSerializer.Serialize(element.ToString()),
                    CustomFieldType.Email => JsonSerializer.Serialize(element.ToString()),
                    CustomFieldType.Phone => JsonSerializer.Serialize(element.ToString()),
                    CustomFieldType.Radio => JsonSerializer.Serialize(element.ToString()),
                    CustomFieldType.Checkbox => JsonSerializer.Serialize(element.ToString()),
                    CustomFieldType.CheckboxGroup => JsonSerializer.Serialize(element.ToString()),
                    CustomFieldType.SelectList => JsonSerializer.Serialize(element.ToString()),
                    CustomFieldType.TextArea => JsonSerializer.Serialize(element.ToString()),
                    _ => throw new NotImplementedException(),
                };
            }

            throw new NotImplementedException(); // we should not get here
        }

        public static string Resolve(QuestionType type, object? definition)
        {
            if (definition == null)
            {
                return type switch
                {
                    QuestionType.Number => JsonSerializer.Serialize(new NumericDefinition()),
                    QuestionType.Text => JsonSerializer.Serialize(new TextDefinition()),                    
                    QuestionType.YesNo => JsonSerializer.Serialize(new QuestionYesNoDefinition()),
                    QuestionType.SelectList => JsonSerializer.Serialize(new QuestionSelectListDefinition()),
                    _ => throw new NotImplementedException(),
                };
            }
            else if (definition is CustomFieldDefinition)
            {
                return type switch
                {
                    QuestionType.Number => JsonSerializer.Serialize((NumericDefinition)definition),
                    QuestionType.Text => JsonSerializer.Serialize((TextDefinition)definition),
                    QuestionType.YesNo => JsonSerializer.Serialize((QuestionYesNoDefinition)definition), 
                    QuestionType.SelectList => JsonSerializer.Serialize((QuestionSelectListDefinition)definition),
                    _ => throw new NotImplementedException(),
                };
            }
            else if (definition is JsonElement element)
            {
                return type switch
                {
                    QuestionType.Number => JsonSerializer.Serialize(element.ToString()),
                    QuestionType.Text => JsonSerializer.Serialize(element.ToString()),                    
                    QuestionType.YesNo => JsonSerializer.Serialize(element.ToString()),
                    QuestionType.SelectList => JsonSerializer.Serialize(element.ToString()),
                    _ => throw new NotImplementedException(),
                };
            } else if (definition is string)
            {
                return type switch
                {
                    QuestionType.Number => JsonSerializer.Serialize(definition),
                    QuestionType.Text => JsonSerializer.Serialize(definition),
                    QuestionType.YesNo => JsonSerializer.Serialize(definition),
                    QuestionType.SelectList => JsonSerializer.Serialize(definition),
                    _ => throw new NotImplementedException(),
                };
            }

            throw new NotImplementedException(); // we should not get here
        }

        public static string? ResolveMin(CustomFieldDefinition field)
        {
            return field switch
            {                
                NumericDefinition => ((NumericDefinition)field).Min.ToString(),
                CurrencyDefinition => ((CurrencyDefinition)field).Min.ToString(),
                _ => null,
            };
        }

        public static string? ResolveYesValue(CustomFieldDefinition field)
        {
            return field switch
            {
                QuestionYesNoDefinition yesNoField => yesNoField.YesValue.ToString(),
                _ => null,
            };
        }

        public static string? ResolveNoValue(CustomFieldDefinition field)
        {
            return field switch
            {
                QuestionYesNoDefinition yesNoField => yesNoField.NoValue.ToString(),
                _ => null,
            };
        }

        public static string? ResolveMax(CustomFieldDefinition field)
        {
            return field switch
            {
                NumericDefinition => ((NumericDefinition)field).Max.ToString(),
                CurrencyDefinition => ((CurrencyDefinition)field).Max.ToString(),
                _ => null,
            };
        }

        public static string? ResolveMinLength(CustomFieldDefinition field)
        {
            return field switch
            {
                TextDefinition => ((TextDefinition)field).MinLength.ToString(),
                TextAreaDefinition => ((TextAreaDefinition)field).MinLength.ToString(),
                _ => null,
            };
        }

        public static string? ResolveMaxLength(CustomFieldDefinition field)
        {
            return field switch
            {
                TextDefinition => ((TextDefinition)field).MaxLength.ToString(),
                TextAreaDefinition => ((TextAreaDefinition)field).MaxLength.ToString(),
                _ => null,
            };
        }
    }
}