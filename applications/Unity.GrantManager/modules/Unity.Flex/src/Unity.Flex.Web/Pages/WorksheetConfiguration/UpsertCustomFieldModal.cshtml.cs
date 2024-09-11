using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.CustomFieldDefinitionWidget;
using Unity.Flex.Worksheets;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.Flex.Web.Pages.WorksheetConfiguration;
public class UpsertCustomFieldModalModel(ICustomFieldAppService customFieldAppService,
    IWorksheetSectionAppService worksheetSectionAppService,
    IWorksheetListAppService worksheetListAppService) : FlexPageModel
{
    [BindProperty]
    public Guid WorksheetId { get; set; }

    [BindProperty]
    public Guid SectionId { get; set; }

    [BindProperty]
    public Guid? FieldId { get; set; }

    [DisplayName("Name (Key)")]
    [BindProperty]
    [MinLength(1)]
    [MaxLength(25)]
    [RegularExpression("^([a-zA-Z0-9]*)$", ErrorMessage = "Illegal character found in name. Please enter a valid name")]
    [Required]
    public string? Key { get; set; }

    [BindProperty]
    [MinLength(1)]
    [MaxLength(25)]
    [Required]
    public string? Label { get; set; }

    [BindProperty]
    public uint Order { get; set; }

    [BindProperty]
    public string? Definition { get; set; }

    [BindProperty]
    public bool Published { get; set; }

    [BindProperty]
    public WorksheetUpsertAction UpsertAction { get; set; }

    [BindProperty]
    public bool IsDelete { get; set; }

    [SelectItems(nameof(FieldTypes))]
    [Required]
    [BindProperty]
    public string? FieldType { get; set; }
    public List<SelectListItem>? FieldTypes { get; set; }

    public async Task OnGetAsync(Guid worksheetId, Guid sectionId, Guid fieldId, string actionType)
    {
        WorksheetId = worksheetId;
        SectionId = sectionId;
        FieldTypes = GetAvailableFieldTypes();
        UpsertAction = (WorksheetUpsertAction)Enum.Parse(typeof(WorksheetUpsertAction), actionType);
        FieldType = "Text";

        if (UpsertAction == WorksheetUpsertAction.Update)
        {
            CustomFieldDto customField = await customFieldAppService.GetAsync(fieldId);
            var worksheet = await worksheetListAppService.GetAsync(worksheetId);

            Key = customField.Key;
            Label = customField.Label;
            FieldId = fieldId;
            Published = worksheet.Published;
            FieldType = customField.Type.ToString();
            Definition = customField.Definition;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var delete = Request.Form["deleteCustomFieldBtn"];
        var save = Request.Form["saveCustomFieldBtn"];

        if (delete == "delete" || IsDelete)
        {
            await customFieldAppService.DeleteAsync(FieldId!.Value);
            return new OkObjectResult(new ModalResponse()
            {
                CustomFieldId = FieldId!.Value,
                WorksheetId = WorksheetId,
                Action = "Delete"
            });
        }
        else if (save == "save")
        {
            switch (UpsertAction)
            {
                case WorksheetUpsertAction.Insert:
                    return MapModalResponse(await InsertCustomField());
                case WorksheetUpsertAction.Update:
                    return MapModalResponse(await UpdateCustomField());
                default:
                    break;
            }
        }

        return NoContent();
    }

    private async Task<CustomFieldDto> InsertCustomField()
    {
        return await worksheetSectionAppService.CreateCustomFieldAsync(SectionId, new CreateCustomFieldDto()
        {
            Definition = ExtractDefinition(),
            Label = Label!,
            Key = Key!,
            Type = (CustomFieldType)Enum.Parse(typeof(CustomFieldType), FieldType!)
        });
    }

    private async Task<CustomFieldDto> UpdateCustomField()
    {
        return await customFieldAppService.EditAsync(FieldId!.Value, new EditCustomFieldDto()
        {
            Definition = ExtractDefinition(),
            Label = Label!,
            Key = Key!,
            Type = (CustomFieldType)Enum.Parse(typeof(CustomFieldType), FieldType!)
        });
    }

    private object? ExtractDefinition()
    {
        var fieldType = Enum.TryParse(FieldType, out CustomFieldType type);
        if (!fieldType) return null;
        return CustomFieldDefinitionWidget.ParseFormValues(type, Request.Form);
    }

    private OkObjectResult MapModalResponse(CustomFieldDto customFieldDto)
    {
        return new OkObjectResult(new ModalResponse()
        {
            CustomFieldId = customFieldDto.Id,
            WorksheetId = WorksheetId
        });
    }

    private static List<SelectListItem> GetAvailableFieldTypes()
    {
        // Tailored list in specific order of the available fields enum
        return [new SelectListItem("Text", "Text"),
            new SelectListItem("Text Area", "TextArea"),
            new SelectListItem("Number", "Numeric"),
            new SelectListItem("Currency", "Currency"),
            new SelectListItem("Date", "Date"),
            new SelectListItem("Email", "Email"),
            new SelectListItem("Phone", "Phone"),
            new SelectListItem("Checkbox", "Checkbox"),
            new SelectListItem("Checkbox Group", "CheckboxGroup"),
            new SelectListItem("Select List", "SelectList"),
            new SelectListItem("Radio", "Radio"),
            new SelectListItem("Yes/No Select", "YesNo"),
            new SelectListItem("BC Address", "BCAddress")];
    }

    public class ModalResponse : CustomFieldDto
    {
        public Guid WorksheetId { get; set; }
        public Guid CustomFieldId { get; set; }
        public string Action { get; set; } = string.Empty;
    }
}
