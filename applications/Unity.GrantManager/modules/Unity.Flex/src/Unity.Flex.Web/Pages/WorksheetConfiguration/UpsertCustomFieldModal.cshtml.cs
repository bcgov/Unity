using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.Flex.Worksheets;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.Flex.Web.Pages.WorksheetConfiguration;
public class UpsertCustomFieldModalModel(ICustomFieldAppService customFieldAppService,
    IWorksheetSectionAppService worksheetSectionAppService) : FlexPageModel
{
    [BindProperty]
    public Guid Id { get; set; }

    [BindProperty]
    public Guid WorksheetId { get; set; }

    [BindProperty]
    public Guid SectionId { get; set; }

    [DisplayName("Name")]
    [BindProperty]
    [MinLength(1)]
    [MaxLength(25)]
    [Required]
    public string? Field { get; set; }

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
    public WorksheetUpsertAction UpsertAction { get; set; }

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

        if (UpsertAction == WorksheetUpsertAction.Update)
        {
            CustomFieldDto customField = await customFieldAppService.GetAsync(fieldId);
            Field = customField.Field;
            Label = customField.Label;
            Id = fieldId;
        }
    }

    public async Task<IActionResult> OnPostAsync()
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

        return NoContent();
    }

    private async Task<CustomFieldDto> InsertCustomField()
    {
        return await worksheetSectionAppService.CreateCustomFieldAsync(SectionId, new CreateCustomFieldDto()
        {
            Definition = null, // use default definition
            Label = Label!,
            Field = Field!,
            Type = (CustomFieldType)Enum.Parse(typeof(CustomFieldType), FieldType!)
        });
    }

    private async Task<CustomFieldDto> UpdateCustomField()
    {
        return await customFieldAppService.EditAsync(Id, new EditCustomFieldDto()
        {
            Definition = null, // use default definition
            Label = Label!,
            Field = Field!,
            Type = (CustomFieldType)Enum.Parse(typeof(CustomFieldType), FieldType!)
        });
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
            new SelectListItem("Number", "Numeric"),
            new SelectListItem("Currency", "Currency"),
            new SelectListItem("Date", "Date"),
            new SelectListItem("Email", "Email"),
            new SelectListItem("Phone", "Phone"),
            new SelectListItem("Checkbox", "Checkbox"),
            new SelectListItem("Yes/No Select", "YesNo"),
            new SelectListItem("BC Address", "BCAddress")];

        //new SelectListItem("Date", "Date"),
        //new SelectListItem("Yes/No Select", "YesNo"),
        //new SelectListItem("Email", "Email"),
        //new SelectListItem("Phone", "Phone"),
        //new SelectListItem("Radio", "Radio"),
        //new SelectListItem("Checkbox", "Checkbox"),
        //new SelectListItem("Checkbox Group", "CheckboxGroup"),
        //new SelectListItem("Select List", "SelectList"),
        //new SelectListItem("BC Address", "BCAddress"),        
    }

    public class ModalResponse : CustomFieldDto
    {
        public Guid WorksheetId { get; set; }
        public Guid CustomFieldId { get; set; }
    }
}
