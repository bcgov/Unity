using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Unity.Flex;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Definitions;
using Unity.Flex.Domain.Worksheets;
using Unity.GrantManager.ApplicationForms.Mapping;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Intakes.Mapping;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public interface IApplicationFormVersionMappingReadService
{
    Task<ApplicationFormMappingReadModelDto> GetAsync(Guid formVersionId);
}

public class ApplicationFormVersionMappingReadService(
    IRepository<ApplicationFormVersion, Guid> applicationFormVersionRepository,
    IWorksheetListRepository worksheetListRepository,
    IFeatureChecker featureChecker) : IApplicationFormVersionMappingReadService, ITransientDependency
{
    private static readonly HashSet<string> ExcludedMappingFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(IntakeMapping.ConfirmationId),
        nameof(IntakeMapping.SubmissionDate),
        nameof(IntakeMapping.SubmissionId)
    };

    public async Task<ApplicationFormMappingReadModelDto> GetAsync(Guid formVersionId)
    {
        var formVersion = await applicationFormVersionRepository.GetAsync(formVersionId);

        var model = new ApplicationFormMappingReadModelDto
        {
            ApplicationFormVersionId = formVersion.Id,
            ApplicationFormId = formVersion.ApplicationFormId,
            ChefsApplicationFormGuid = formVersion.ChefsApplicationFormGuid,
            ChefsFormVersionGuid = formVersion.ChefsFormVersionGuid,
            ExistingMapping = formVersion.SubmissionHeaderMapping,
            ChefsFields = BuildChefsFields(formVersion.AvailableChefsFields),
            UnityCoreFields = BuildUnityCoreFields()
        };

        if (await featureChecker.IsEnabledAsync("Unity.Flex"))
        {
            var worksheets = await worksheetListRepository.GetListByCorrelationAsync(formVersionId, CorrelationConsts.FormVersion, includeDetails: true);
            model.Worksheets = worksheets.Select((Worksheet worksheet) => MapWorksheet(worksheet)).ToList();
        }

        return model;
    }

    private static List<MappingFieldDto> BuildChefsFields(string? availableChefsFields)
    {
        if (string.IsNullOrWhiteSpace(availableChefsFields))
        {
            return [];
        }

        var jObject = JObject.Parse(availableChefsFields);
        return jObject.Properties()
            .Where(property => !ExcludedMappingFieldNames.Contains(property.Name))
            .Select(property =>
            {
                var fieldConfig = JObject.Parse(property.Value.ToString());
                return new MappingFieldDto
                {
                    Name = property.Name,
                    Type = fieldConfig["type"]?.ToString() ?? "String",
                    IsCustom = false,
                    Label = fieldConfig["label"]?.ToString() ?? property.Name
                };
            })
            .OrderBy(field => field.Label)
            .ToList();
    }

    private static List<MappingFieldDto> BuildUnityCoreFields()
    {
        var intakeMapping = new IntakeMapping();
        return intakeMapping.GetType()
            .GetProperties()
            .Select(property => new
            {
                Property = property,
                Browsable = property.GetCustomAttributes(typeof(BrowsableAttribute), true).Cast<BrowsableAttribute>().SingleOrDefault(),
                DisplayName = property.GetCustomAttributes(typeof(DisplayNameAttribute), true).Cast<DisplayNameAttribute>().SingleOrDefault(),
                FieldType = property.GetCustomAttributes(typeof(MapFieldTypeAttribute), true).Cast<MapFieldTypeAttribute>().SingleOrDefault()
            })
            .Where(item => item.Browsable?.IsDefaultAttribute() == true)
            .Where(item => !ExcludedMappingFieldNames.Contains(item.Property.Name))
            .Select(item => new MappingFieldDto
            {
                Name = item.Property.Name,
                Type = item.FieldType?.Type ?? "String",
                IsCustom = false,
                Label = item.DisplayName?.DisplayName ?? item.Property.Name
            })
            .OrderBy(field => field.Label)
            .ToList();
    }

    private static WorksheetMappingFieldsDto MapWorksheet(Worksheet worksheet)
    {
        return new WorksheetMappingFieldsDto
        {
            WorksheetId = worksheet.Id,
            WorksheetName = worksheet.Name,
            Fields = worksheet.Sections
                .SelectMany(section => section.Fields)
                .Where(field => IsMappable(field))
                .Select(field => new MappingFieldDto
                {
                    Name = string.IsNullOrWhiteSpace(field.Key) ? field.Name : field.Key,
                    Type = ConvertCustomType(field.Type),
                    IsCustom = true,
                    Label = $"{field.Label} ({worksheet.Name})"
                })
                .OrderBy(field => field.Label)
                .ToList()
        };
    }

    private static bool IsMappable(CustomField? field)
    {
        if (field == null)
        {
            return false;
        }

        return field.Type switch
        {
            CustomFieldType.DataGrid => IsDataGridMappable(field),
            _ => true
        };
    }

    private static bool IsDataGridMappable(CustomField field)
    {
        if (string.IsNullOrWhiteSpace(field.Definition))
        {
            return true;
        }

        var definition = (DataGridDefinition?)field.Definition.ConvertDefinition(CustomFieldType.DataGrid);
        return definition?.Dynamic ?? true;
    }

    private static string ConvertCustomType(CustomFieldType type) => type switch
    {
        CustomFieldType.Text => "String",
        CustomFieldType.Date => "Date",
        CustomFieldType.Email => "Email",
        CustomFieldType.Phone => "Phone",
        CustomFieldType.DateTime => "Date",
        CustomFieldType.YesNo => "YesNo",
        CustomFieldType.Currency => "Currency",
        CustomFieldType.Numeric => "Number",
        CustomFieldType.Radio => "Radio",
        CustomFieldType.Checkbox => "Checkbox",
        CustomFieldType.CheckboxGroup => "CheckboxGroup",
        CustomFieldType.SelectList => "SelectList",
        CustomFieldType.BCAddress => "BCAddress",
        CustomFieldType.TextArea => "TextArea",
        CustomFieldType.DataGrid => "DataGrid",
        _ => string.Empty
    };
}
