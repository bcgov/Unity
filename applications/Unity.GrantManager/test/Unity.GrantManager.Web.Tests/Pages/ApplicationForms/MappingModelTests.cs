using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Unity.GrantManager.ApplicationForms.Mapping;
using Unity.GrantManager.Web.Pages.ApplicationForms;
using Xunit;

namespace Unity.GrantManager.Pages.ApplicationForms;

public class MappingModelTests
{
    [Fact]
    public void BuildMappingFields_Should_Use_Unity_Targets_And_Exclude_Chefs_Sources()
    {
        var readModel = new ApplicationFormMappingReadModelDto
        {
            ChefsFields =
            [
                new MappingFieldDto { Name = "chefsProjectName", Label = "CHEFS Project Name", Type = "textfield" }
            ],
            UnityCoreFields =
            [
                new MappingFieldDto { Name = "ProjectName", Label = "Project Name", Type = "String" }
            ],
            Worksheets =
            [
                new WorksheetMappingFieldsDto
                {
                    Fields =
                    [
                        new MappingFieldDto { Name = "CustomField2.Text", Label = "Custom Field", Type = "String", IsCustom = true }
                    ]
                }
            ]
        };

        var fields = MappingModel.BuildMappingFields(readModel);

        fields.Select(field => field.Name).ShouldBe(["CustomField2.Text", "ProjectName"]);
    }
}
