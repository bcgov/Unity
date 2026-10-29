using System;
using Shouldly;
using Unity.GrantManager.GrantsPortal.Notifications;
using Xunit;

namespace Unity.GrantManager.GrantsPortal;

public class ApplicantUpdateEmailRendererTests
{
    [Fact]
    public void ShouldEncodeAllDynamicContentAndShowOldAndNewValues()
    {
        var applicantId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var details = new ApplicantUpdateDetails("Applicant Information",
            [new("Organization <name>", "<script>changed</script>", "Old & name"), new("Status", null, "Active")], true);

        var html = ApplicantUpdateEmailRenderer.Render("https://unity.example.test/", tenantId, applicantId,
            "APP<&>", "Applicant <name>", new DateTime(2025, 7, 1, 12, 0, 0, DateTimeKind.Utc), details);

        html.ShouldContain("Applicant &lt;name&gt;");
        html.ShouldContain("Organization &lt;name&gt;");
        html.ShouldContain("Old &amp; name");
        html.ShouldContain("&lt;script&gt;changed&lt;/script&gt;");
        html.ShouldNotContain("<script>");
        html.ShouldContain("Previous value");
        html.ShouldContain("New value");
        html.ShouldContain("<td>Active</td><td>Not provided</td>");
        html.ShouldContain($"href=\"https://unity.example.test/GrantApplicants/Details?ApplicantId={applicantId}&amp;TenantId={tenantId}\">APP&lt;&amp;&gt;</a>");
    }

}
