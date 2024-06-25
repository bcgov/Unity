using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.Flex.Worksheets;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.Flex.Web.Pages.WorksheetConfiguration;
public class UpsertCustomFieldModalModel(ICustomFieldAppService customFieldAppService) : FlexPageModel
{
    [BindProperty]
    public Guid Id { get; set; }

    [BindProperty]
    public Guid WorksheetId { get; set; }

    [BindProperty]
    public Guid SectionId { get; set; }

    [BindProperty]
    [MinLength(1)]
    [MaxLength(25)]
    [Required]
    public string? Name { get; set; }

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

    [BindProperty]
    public List<KeyValuePair<string, string>>? Definitions { get; set; }

    public async Task OnGetAsync(Guid worksheetId, Guid sectionId, Guid fieldId, string actionType)
    {
        WorksheetId = worksheetId;
        SectionId = sectionId;
        Definition = "{}";
        Definitions = GetAvailableDefinitions();
        FieldTypes = GetAvailableFieldTypes();
        UpsertAction = (WorksheetUpsertAction)Enum.Parse(typeof(WorksheetUpsertAction), actionType);

        if (UpsertAction == WorksheetUpsertAction.Update)
        {
            CustomFieldDto customField = await customFieldAppService.GetAsync(fieldId);
            Name = customField.Name;
            Label = customField.Label;
            Id = fieldId;
        }
    }

    private List<KeyValuePair<string, string>> GetAvailableDefinitions()
    {
        throw new NotImplementedException();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await Task.CompletedTask;

        return NoContent();
    }

    private static List<SelectListItem> GetAvailableFieldTypes()
    {
        // Tailored list in specific order of the available fields enum
        return [new SelectListItem("Text", "Text"),
            new SelectListItem("Number", "Numeric"),
            new SelectListItem("Date", "Date"),
            new SelectListItem("Currency", "Currency"),
            new SelectListItem("Yes/No Select", "YesNo"),
            new SelectListItem("Email", "Email"),
            new SelectListItem("Phone", "Phone"),
            new SelectListItem("Radio", "Radio"),
            new SelectListItem("Checkbox", "Checkbox"),
            new SelectListItem("Checkbox Group", "CheckboxGroup"),
            new SelectListItem("Select List", "SelectList"),
            new SelectListItem("BC Address", "BCAddress"),
        ];
    }
}
