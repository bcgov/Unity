using System;
using System.Threading.Tasks;
using Shouldly;
using Unity.GrantManager.ApplicantPortal;
using Unity.GrantManager.Settings;
using Volo.Abp;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Volo.Abp.Validation;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.ApplicantPortalTests;

public class MultipleIdentitiesMessageTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    private readonly Guid tenantId = Guid.NewGuid();

    [Fact]
    public async Task UnconfiguredTenant_UsesDefaultAndResolvesEveryEmailParameter()
    {
        using (GetRequiredService<ICurrentTenant>().Change(tenantId))
        {
            var messageService = GetRequiredService<ApplicantPortalMessageService>();
            var result = await messageService.GetAsync();
            result.UseDefaultMessage.ShouldBeTrue();
            result.MessageHtml.ShouldBe(MultipleIdentitiesMessageDefaults.MessageHtml);
            result.CustomMessageHtml.ShouldBeNull();
            var rendered = await messageService.RenderAsync("NoReply@gov.bc.ca");
            rendered.ShouldContain("NoReply@gov.bc.ca");
            rendered.ShouldNotContain("{inboxEmail}");
        }
    }

    [Fact]
    public async Task CustomMessage_IsSavedPerTenant_AndSurvivesSavingDefaultMode()
    {
        var currentTenant = GetRequiredService<ICurrentTenant>();
        var messageService = GetRequiredService<ApplicantPortalMessageService>();
        using (currentTenant.Change(tenantId))
        {
            await WithUnitOfWorkAsync(() => messageService.UpdateAsync(new()
            {
                UseDefaultMessage = false,
                MessageHtml = "<p><strong>Contact us</strong> at {inboxEmail}.</p>"
            }));
            var saved = await messageService.GetAsync();
            saved.UseDefaultMessage.ShouldBeFalse();
            saved.MessageHtml.ShouldContain("<strong>Contact us</strong>");
        }

        using (currentTenant.Change(Guid.NewGuid()))
        {
            (await messageService.GetAsync()).UseDefaultMessage.ShouldBeTrue();
        }

        using (currentTenant.Change(tenantId))
        {
            await WithUnitOfWorkAsync(() => messageService.UpdateAsync(new() { UseDefaultMessage = true }));
            var restored = await messageService.GetAsync();
            restored.UseDefaultMessage.ShouldBeTrue();
            restored.MessageHtml.ShouldBe(MultipleIdentitiesMessageDefaults.MessageHtml);
            var custom = await GetRequiredService<ISettingManager>().GetOrNullForCurrentTenantAsync(
                SettingsConstants.ApplicantPortal.MultipleIdentitiesMessageHtml);
            custom.ShouldBe("<p><strong>Contact us</strong> at {inboxEmail}.</p>");
            restored.CustomMessageHtml.ShouldBe(custom);
            (await messageService.RenderAsync("contact@example.test")).ShouldNotContain("<strong>Contact us</strong>");

            await WithUnitOfWorkAsync(() => messageService.UpdateAsync(new()
            {
                UseDefaultMessage = false,
                MessageHtml = restored.CustomMessageHtml!
            }));
            (await messageService.GetAsync()).MessageHtml.ShouldBe(custom);
            (await messageService.RenderAsync("contact@example.test")).ShouldContain("<strong>Contact us</strong>");
        }
    }

    [Fact]
    public async Task SavingDefaultMode_CanRetainAnEditedCustomDraft_WithoutDisplayingIt()
    {
        using (GetRequiredService<ICurrentTenant>().Change(tenantId))
        {
            var messageService = GetRequiredService<ApplicantPortalMessageService>();
            var result = await WithUnitOfWorkAsync(() => messageService.UpdateAsync(new()
            {
                UseDefaultMessage = true,
                MessageHtml = MultipleIdentitiesMessageDefaults.MessageHtml,
                CustomMessageHtml = "<p><strong>Edited custom</strong></p>"
            }));
            result.UseDefaultMessage.ShouldBeTrue();
            result.MessageHtml.ShouldBe(MultipleIdentitiesMessageDefaults.MessageHtml);
            result.CustomMessageHtml.ShouldBe("<p><strong>Edited custom</strong></p>");

            var reloaded = await messageService.GetAsync();
            reloaded.UseDefaultMessage.ShouldBeTrue();
            reloaded.MessageHtml.ShouldBe(MultipleIdentitiesMessageDefaults.MessageHtml);
            reloaded.CustomMessageHtml.ShouldBe(result.CustomMessageHtml);
            (await messageService.RenderAsync("contact@example.test")).ShouldNotContain("Edited custom");
        }
    }

    [Fact]
    public async Task SavingDefaultMode_WithoutACustomTemplate_KeepsItUnconfigured()
    {
        using (GetRequiredService<ICurrentTenant>().Change(tenantId))
        {
            var messageService = GetRequiredService<ApplicantPortalMessageService>();
            await WithUnitOfWorkAsync(() => messageService.UpdateAsync(new() { UseDefaultMessage = true }));
            var result = await messageService.GetAsync();
            result.CustomMessageHtml.ShouldBeNull();
            result.MessageHtml.ShouldBe(MultipleIdentitiesMessageDefaults.MessageHtml);
        }
    }

    [Fact]
    public async Task RetainedCustomTemplate_IsSanitized_AndMustBeValidEvenInDefaultMode()
    {
        using (GetRequiredService<ICurrentTenant>().Change(tenantId))
        {
            var messageService = GetRequiredService<ApplicantPortalMessageService>();
            var result = await WithUnitOfWorkAsync(() => messageService.UpdateAsync(new()
            {
                UseDefaultMessage = true,
                CustomMessageHtml = "<p onclick=\"alert(1)\">Custom</p><script>alert(1)</script>"
            }));
            result.CustomMessageHtml.ShouldBe("<p>Custom</p>");
            await Should.ThrowAsync<AbpValidationException>(() => messageService.UpdateAsync(new()
            {
                UseDefaultMessage = true,
                CustomMessageHtml = "<p><br></p>"
            }));
            await Should.ThrowAsync<AbpValidationException>(() => messageService.UpdateAsync(new()
            {
                UseDefaultMessage = true,
                CustomMessageHtml = new string('a', MultipleIdentitiesMessageDefaults.MaxHtmlLength + 1)
            }));
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<p><br></p>")]
    [InlineData("<p>&nbsp;\u200B</p>")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    public async Task CustomMessage_RequiresVisibleText(string html)
    {
        using (GetRequiredService<ICurrentTenant>().Change(tenantId))
        {
            var messageService = GetRequiredService<ApplicantPortalMessageService>();
            await Should.ThrowAsync<AbpValidationException>(() => messageService.UpdateAsync(new()
            {
                UseDefaultMessage = false, MessageHtml = html
            }));
        }
    }

    [Fact]
    public async Task CustomMessage_EnforcesHtmlLengthBoundary()
    {
        using (GetRequiredService<ICurrentTenant>().Change(tenantId))
        {
            var messageService = GetRequiredService<ApplicantPortalMessageService>();
            var html = "<p>" + new string('a', MultipleIdentitiesMessageDefaults.MaxHtmlLength - 7) + "</p>";
            var saved = await WithUnitOfWorkAsync(() => messageService.UpdateAsync(new()
            {
                UseDefaultMessage = false, MessageHtml = html
            }));
            saved.MessageHtml.Length.ShouldBe(MultipleIdentitiesMessageDefaults.MaxHtmlLength);
            await Should.ThrowAsync<AbpValidationException>(() => messageService.UpdateAsync(new()
            {
                UseDefaultMessage = false, MessageHtml = html + "a"
            }));
        }
    }

    [Fact]
    public async Task CustomMessage_SupportsRepeatedParametersAndMailtoLinks()
    {
        using (GetRequiredService<ICurrentTenant>().Change(tenantId))
        {
            var messageService = GetRequiredService<ApplicantPortalMessageService>();
            await WithUnitOfWorkAsync(() => messageService.UpdateAsync(new()
            {
                UseDefaultMessage = false,
                MessageHtml = "<p>{inboxEmail}</p><p><a href=\"mailto:{inboxEmail}\">{inboxEmail}</a></p>"
            }));
            var html = await messageService.RenderAsync("a&b@example.test");
            html.ShouldNotContain("{inboxEmail}");
            html.ShouldContain("mailto:a&amp;b@example.test");
            html.ShouldContain(">a&amp;b@example.test</a>");
        }
    }

    [Fact]
    public async Task CustomMessage_DoesNotRequireEmailParameter()
    {
        using (GetRequiredService<ICurrentTenant>().Change(tenantId))
        {
            var messageService = GetRequiredService<ApplicantPortalMessageService>();
            await WithUnitOfWorkAsync(() => messageService.UpdateAsync(new()
            {
                UseDefaultMessage = false, MessageHtml = "<p>Please phone our office.</p>"
            }));
            (await messageService.RenderAsync("contact@example.test")).ShouldBe("<p>Please phone our office.</p>");
        }
    }

    [Fact]
    public async Task HostContext_CannotReadOrWriteTenantMessages()
    {
        using (GetRequiredService<ICurrentTenant>().Change(null))
        {
            var messageService = GetRequiredService<ApplicantPortalMessageService>();
            await Should.ThrowAsync<AbpException>(() => messageService.GetAsync());
            await Should.ThrowAsync<AbpException>(() => messageService.UpdateAsync(new()));
        }
    }

    [Fact]
    public void Sanitizer_PreservesBasicFormatting_AndRemovesActiveContent()
    {
        var html = MultipleIdentitiesMessageContent.Sanitize(
            "<p onclick=\"alert(1)\" style=\"color:red\"><strong>Bold</strong> <em>Italic</em> <u>Underline</u></p>" +
            "<ul><li>One</li></ul><ol><li>Two</li></ol><a href=\"https://example.test\">Help</a>" +
            "<a href=\"javascript:alert(1)\">Bad</a><script>alert(1)</script><iframe src=\"https://example.test\"></iframe>");
        html.ShouldContain("<strong>Bold</strong>");
        html.ShouldContain("<em>Italic</em>");
        html.ShouldContain("<u>Underline</u>");
        html.ShouldContain("<ul><li>One</li></ul>");
        html.ShouldContain("<ol><li>Two</li></ol>");
        html.ShouldContain("href=\"https://example.test\"");
        html.ShouldNotContain("javascript:");
        html.ShouldNotContain("onclick");
        html.ShouldNotContain("style=");
        html.ShouldNotContain("<script");
        html.ShouldNotContain("<iframe");
    }
}
