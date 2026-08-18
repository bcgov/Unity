using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.Applications;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Validation;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.ApplicationForms;

public class ExternalLinksConfigValidationTests : GrantManagerApplicationTestBase
{
    private readonly IApplicationFormAppService _applicationFormAppService;
    private readonly IRepository<ApplicationForm, Guid> _applicationFormRepository;

    public ExternalLinksConfigValidationTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _applicationFormAppService = GetRequiredService<IApplicationFormAppService>();
        _applicationFormRepository = GetRequiredService<IRepository<ApplicationForm, Guid>>();
    }

    [Fact]
    public async Task PatchExternalLinksConfigAsync_ShouldSaveValidRenewalLinkAndRelatedLinks()
    {
        await _applicationFormAppService.PatchExternalLinksConfigAsync(
            GrantManagerTestData.ApplicationForm1_Id,
            new ExternalLinksConfigDto
            {
                RenewalLink = new ExternalLinkConfigDto
                {
                    Uri = "https://chefs-test.apps.silver.devops.gov.bc.ca/app/form",
                    Title = "Renew Now",
                    Description = "Please renew before the deadline.",
                    Published = true,
                    ExternalLinkType = ExternalLinkType.Renewal
                },
                RelatedLinks =
                [
                    new ExternalLinkConfigDto
                    {
                        Uri = "https://chefs-test.apps.silver.devops.gov.bc.ca/app/related-1",
                        Title = "Related One",
                        Description = "First related link.",
                        Published = true,
                        ExternalLinkType = ExternalLinkType.Related
                    },
                    new ExternalLinkConfigDto
                    {
                        Uri = "https://chefs-test.apps.silver.devops.gov.bc.ca/app/related-2",
                        Title = "Related Two",
                        Description = "Second related link.",
                        Published = false,
                        ExternalLinkType = ExternalLinkType.Related
                    }
                ]
            });

        var form = await _applicationFormRepository.GetAsync(GrantManagerTestData.ApplicationForm1_Id);

        form.ExternalLinks.Count.ShouldBe(3);
        var renewalLink = form.ExternalLinks.Single(l => l.ExternalLinkType == ExternalLinkType.Renewal);
        renewalLink.Uri.ShouldBe("https://chefs-test.apps.silver.devops.gov.bc.ca/app/form");
        renewalLink.Title.ShouldBe("Renew Now");
        renewalLink.Description.ShouldBe("Please renew before the deadline.");
        renewalLink.Published.ShouldBeTrue();

        var relatedLinks = form.ExternalLinks
            .Where(l => l.ExternalLinkType == ExternalLinkType.Related)
            .OrderBy(l => l.Order)
            .ToList();
        relatedLinks.Count.ShouldBe(2);
        relatedLinks[0].Title.ShouldBe("Related One");
        relatedLinks[0].Order.ShouldBe(-1);
        relatedLinks[1].Title.ShouldBe("Related Two");
        relatedLinks[1].Order.ShouldBe(-1);
    }

    [Fact]
    public async Task PatchExternalLinksConfigAsync_ShouldReplaceRelatedLinks_OnSubsequentSave()
    {
        await _applicationFormAppService.PatchExternalLinksConfigAsync(
            GrantManagerTestData.ApplicationForm1_Id,
            new ExternalLinksConfigDto
            {
                RelatedLinks =
                [
                    new ExternalLinkConfigDto { Uri = "https://chefs-test.apps.silver.devops.gov.bc.ca/app/first" }
                ]
            });

        await _applicationFormAppService.PatchExternalLinksConfigAsync(
            GrantManagerTestData.ApplicationForm1_Id,
            new ExternalLinksConfigDto
            {
                RelatedLinks =
                [
                    new ExternalLinkConfigDto { Uri = "https://chefs-test.apps.silver.devops.gov.bc.ca/app/second" },
                    new ExternalLinkConfigDto { Uri = "https://chefs-test.apps.silver.devops.gov.bc.ca/app/third" }
                ]
            });

        var form = await _applicationFormRepository.GetAsync(GrantManagerTestData.ApplicationForm1_Id);
        var relatedLinks = form.ExternalLinks.Where(l => l.ExternalLinkType == ExternalLinkType.Related).ToList();

        relatedLinks.Count.ShouldBe(2);
        relatedLinks.ShouldNotContain(l => l.Uri.EndsWith("first", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PatchExternalLinksConfigAsync_ShouldReject_WhenRenewalLinkVisibleWithoutUri()
    {
        await Should.ThrowAsync<AbpValidationException>(
            _applicationFormAppService.PatchExternalLinksConfigAsync(
                GrantManagerTestData.ApplicationForm1_Id,
                new ExternalLinksConfigDto
                {
                    RenewalLink = new ExternalLinkConfigDto
                    {
                        Uri = string.Empty,
                        Published = true
                    }
                }));
    }

    [Fact]
    public async Task PatchExternalLinksConfigAsync_ShouldReject_WhenExceedingMaxRelatedLinks()
    {
        var relatedLinks = Enumerable.Range(1, ApplicationForm.MaxRelatedExternalLinks + 1)
            .Select(i => new ExternalLinkConfigDto { Uri = $"https://chefs-test.apps.silver.devops.gov.bc.ca/app/link-{i}" })
            .ToList();

        await Should.ThrowAsync<AbpValidationException>(
            _applicationFormAppService.PatchExternalLinksConfigAsync(
                GrantManagerTestData.ApplicationForm1_Id,
                new ExternalLinksConfigDto { RelatedLinks = relatedLinks }));
    }

    [Fact]
    public async Task PatchExternalLinksConfigAsync_ShouldReject_WhenUriIsScriptScheme()
    {
        var dto = new ExternalLinksConfigDto
        {
            RenewalLink = new ExternalLinkConfigDto
            {
                Uri = "javascript:alert(1)"
            }
        };

        await Should.ThrowAsync<AbpValidationException>(
            _applicationFormAppService.PatchExternalLinksConfigAsync(GrantManagerTestData.ApplicationForm1_Id, dto));
    }
}
