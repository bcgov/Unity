using Shouldly;
using System;
using Unity.AI.Operations;
using Unity.GrantManager.Applications;
using Volo.Abp.Domain.Entities;
using Xunit;

namespace Unity.GrantManager.AI.Operations;

public class ApplicationToAIApplicationPromptDataDtoMapperTests
{
    [Fact]
    public void Map_Copies_Application_Data_Into_Prompt_Snapshot()
    {
        var applicationId = Guid.NewGuid();
        var formId = Guid.NewGuid();

        var mapper = new ApplicationToAIApplicationPromptDataDtoMapper();
        var application = new Application
        {
            ApplicationFormId = formId,
            ProjectName = "Project Alpha",
            ReferenceNo = "REF-001",
            RequestedAmount = 15000m,
            TotalProjectBudget = 40000m,
            EconomicRegion = "Vancouver Island",
            City = "Victoria",
            SubmissionDate = new DateTime(2026, 6, 12),
            ProjectSummary = "Project summary",
            ProjectStartDate = new DateTime(2026, 7, 1),
            ProjectEndDate = new DateTime(2026, 12, 31),
            Community = "Community name"
        };
        EntityHelper.TrySetId(application, () => applicationId);

        var dto = mapper.Map(application);

        dto.ApplicationId.ShouldBe(applicationId);
        dto.ApplicationFormId.ShouldBe(formId);
        dto.ProjectName.ShouldBe("Project Alpha");
        dto.ReferenceNo.ShouldBe("REF-001");
        dto.RequestedAmount.ShouldBe(15000m);
        dto.TotalProjectBudget.ShouldBe(40000m);
        dto.EconomicRegion.ShouldBe("Vancouver Island");
        dto.City.ShouldBe("Victoria");
        dto.SubmissionDate.ShouldBe(new DateTime(2026, 6, 12));
        dto.ProjectSummary.ShouldBe("Project summary");
        dto.ProjectStartDate.ShouldBe(new DateTime(2026, 7, 1));
        dto.ProjectEndDate.ShouldBe(new DateTime(2026, 12, 31));
        dto.Community.ShouldBe("Community name");
    }
}
