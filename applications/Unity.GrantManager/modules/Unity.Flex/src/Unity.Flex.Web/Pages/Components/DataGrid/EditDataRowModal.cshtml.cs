using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.CustomFieldDefinitionWidget;
using Unity.Flex.Worksheets;

namespace Unity.Flex.Web.Pages.Flex;

public class EditDataRowModalModel : FlexPageModel
{
    [BindProperty]
    public Guid FieldId { get; set; }

    [BindProperty]
    public int Row { get; set; }

    public async Task OnGetAsync(Guid fieldId, int row)
    {
        Row = row;
        FieldId = fieldId;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return NoContent();
    }

    private OkObjectResult MapModalResponse(CustomFieldDto customFieldDto)
    {
        return new OkObjectResult(new ModalResponse()
        {
            Row = Row,
            FieldId = FieldId
        });
    }

    public class ModalResponse : CustomFieldDto
    {
        public int Row { get; set; }
        public Guid FieldId { get; set; }
    }
}
