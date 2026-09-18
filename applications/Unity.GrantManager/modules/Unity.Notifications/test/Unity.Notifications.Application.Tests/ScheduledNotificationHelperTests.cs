using System;
using System.Collections.Generic;
using Shouldly;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Events;
using Unity.Notifications.Templates;
using Xunit;

namespace Unity.Notifications.Events;

public class ScheduledNotificationHelperTests
{
    [Fact]
    public void BuildTokenValues_ForApplicantTemplate_ContainsOnlyApplicantScopedValues()
    {
        var applicant = new Applicant
        {
            ApplicantName = "Northwind Society",
            UnityApplicantId = "APP-123",
            OrgName = "Northwind Society",
            Sector = "Technology"
        };
        var application = new Application
        {
            Applicant = applicant,
            ProjectName = "Application-only project"
        };

        var values = ScheduledNotificationHelper.BuildTokenValues(application, applicantAgent: null, TemplateTypes.Applicant);

        values["applicant_name"].ShouldBe("Northwind Society");
        values["applicant_id"].ShouldBe("APP-123");
        values["sector"].ShouldBe("Technology");
        values.ShouldNotContainKey("project_name");
        values.ShouldNotContainKey("submission_number");
        values.ShouldNotContainKey("approved_amount");
    }

    [Fact]
    public void BuildTokenValues_ForApplicationTemplate_PreservesApplicationValues()
    {
        var application = new Application
        {
            Applicant = new Applicant { ApplicantName = "Northwind Society" },
            ProjectName = "Community technology project",
            ReferenceNo = "APP-456",
            RequestedAmount = 1250m,
            UnityApplicationId = "UNITY-456"
        };

        var values = ScheduledNotificationHelper.BuildTokenValues(application, applicantAgent: null);

        values["applicant_name"].ShouldBe("Northwind Society");
        values["project_name"].ShouldBe("Community technology project");
        values["submission_number"].ShouldBe("APP-456");
        values["requested_amount"].ShouldBe("$1,250.00");
        values["unity_application_id"].ShouldBe("UNITY-456");
    }

    [Fact]
    public void BuildTokenValues_ForApplicantTemplate_RendersMissingOptionalValuesAsEmpty()
    {
        var application = new Application
        {
            Applicant = new Applicant()
        };

        var values = ScheduledNotificationHelper.BuildTokenValues(application, applicantAgent: null, TemplateTypes.Applicant);

        values["organization_name"].ShouldBeEmpty();
        values["fiscal_year_end"].ShouldBeEmpty();
        values["started_operating_date"].ShouldBeEmpty();
    }

    [Fact]
    public void RenderTemplate_ReplacesKnownTokensAndLeavesUnknownTokensUntouched()
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["applicant_name"] = "Northwind Society"
        };

        var rendered = ScheduledNotificationHelper.RenderTemplate(
            "Hello {{applicant_name}}. Reference: {{unknown_token}}.",
            values);

        rendered.ShouldBe("Hello Northwind Society. Reference: {{unknown_token}}.");
    }

    [Fact]
    public void EmailTemplate_DefaultsToApplicantType()
    {
        var template = new EmailTemplate(
            Guid.NewGuid(),
            "Applicant welcome",
            string.Empty,
            "Welcome",
            string.Empty,
            "<p>Welcome</p>",
            "notifications@example.com");

        template.TemplateType.ShouldBe(TemplateTypes.Applicant);
    }
}