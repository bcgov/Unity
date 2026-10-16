using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;
using Volo.Abp.ObjectMapping;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Security.Encryption;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.ApplicationForms;

public class ApplicationFormDeletionTests : GrantManagerApplicationTestBase
{
    private readonly IApplicationFormAppService _service;
    private readonly IApplicationFormRepository _forms;

    public ApplicationFormDeletionTests(ITestOutputHelper output) : base(output)
    {
        _service = GetRequiredService<IApplicationFormAppService>();
        _forms = GetRequiredService<IApplicationFormRepository>();
    }

    [Fact]
    public async Task Should_HideFormFromList_WhileKeepingNormalReads()
    {
        var form = await CreateFormAsync("Form to hide");
        var before = await _service.GetListAsync(new PagedAndSortedResultRequestDto());

        await _service.DeleteAsync(form.Id);

        var after = await _service.GetListAsync(new PagedAndSortedResultRequestDto());
        after.TotalCount.ShouldBe(before.TotalCount - 1);
        after.Items.ShouldNotContain(item => item.Id == form.Id);
        var persisted = await _forms.GetAsync(form.Id);
        persisted.IsHiddenFromList.ShouldBeTrue();
        persisted.IsDeleted.ShouldBeFalse();
        persisted.DeletionTime.ShouldBeNull();
        persisted.DeleterId.ShouldBeNull();
        persisted.ApplicationFormName.ShouldBe(form.ApplicationFormName);
        persisted.PaymentApprovalThreshold.ShouldBe(5000m);
        var dto = await _service.GetAsync(form.Id);
        dto.IsHiddenFromList.ShouldBeTrue();
        dto.ApiKey.ShouldBe("test-key");
    }

    [Fact]
    public async Task Should_PreserveApplicationsVersionsAndSubmissions_WhenFormIsHidden()
    {
        var form = await CreateFormAsync("Form with existing application");
        var applications = GetRequiredService<IApplicationRepository>();
        var application = await applications.GetAsync(GrantManagerTestData.Application1_Id);
        application.ApplicationFormId = form.Id;
        await applications.UpdateAsync(application, autoSave: true);
        var versions = GetRequiredService<IApplicationFormVersionRepository>();
        var version = await versions.InsertAsync(new ApplicationFormVersion
        {
            ApplicationFormId = form.Id,
            Version = 3,
            Published = true,
            SubmissionHeaderMapping = "{\"field\":\"ApplicantName\"}"
        }, autoSave: true);
        var submissions = GetRequiredService<IApplicationFormSubmissionRepository>();
        var submission = await submissions.InsertAsync(new ApplicationFormSubmission
        {
            ApplicationId = application.Id,
            ApplicantId = application.ApplicantId,
            ApplicationFormId = form.Id,
            ApplicationFormVersionId = version.Id,
            Submission = "{\"field\":\"original value\"}"
        }, autoSave: true);

        await _service.DeleteAsync(form.Id);

        var loaded = await applications.WithBasicDetailsAsync(application.Id);
        loaded.ApplicationForm.Id.ShouldBe(form.Id);
        loaded.IsDeleted.ShouldBeFalse();
        (await versions.GetAsync(version.Id)).SubmissionHeaderMapping.ShouldBe(version.SubmissionHeaderMapping);
        (await submissions.GetAsync(submission.Id)).Submission.ShouldBe(submission.Submission);
        var details = await _service.GetFormDetailsByApplicationIdAsync(application.Id);
        details.ApplicationFormName.ShouldBe(form.ApplicationFormName);
        details.ApplicationFormVersion.ShouldBe(3);
        (await _service.GetFormPaymentApprovalThresholdByApplicationIdAsync(application.Id)).ShouldBe(5000m);
    }

    [Fact]
    public async Task Should_KeepHiddenFormsAvailableForSynchronization()
    {
        var form = await CreateFormAsync("Connected hidden form");
        await _service.DeleteAsync(form.Id);

        var synchronization = GetRequiredService<IApplicationFormSycnronizationService>();
        var connected = await synchronization.GetConnectedApplicationFormsAsync();
        connected.ShouldContain(item => item.Id == form.Id);
    }

    [Fact]
    public async Task Should_ExcludeHiddenParentChoices_BeforeCountingAndPaging()
    {
        var hidden = await CreateFormAsync("Visibility lookup A");
        var visible = await CreateFormAsync("Visibility lookup B");
        await _service.DeleteAsync(hidden.Id);

        var result = await _service.GetParentFormLookupAsync(new ParentFormLookupRequestDto
        {
            Filter = "Visibility lookup",
            MaxResultCount = 1
        });

        result.TotalCount.ShouldBe(1);
        result.Items.Single().ApplicationFormId.ShouldBe(visible.Id);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Should_RejectNewHiddenParentAssignment(bool payable)
    {
        var parent = await CreateFormAsync("Hidden parent");
        var child = await CreateFormAsync("Child");
        await _service.DeleteAsync(parent.Id);

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            _service.SavePaymentConfiguration(new FormPaymentConfigurationDto
            {
                ApplicationFormId = child.Id,
                Payable = payable,
                FormHierarchy = FormHierarchyType.Child,
                ParentFormId = parent.Id
            }));

        exception.Code.ShouldBe(GrantManagerDomainErrorCodes.ParentFormUnavailable);
        (await _forms.GetAsync(child.Id)).ParentFormId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_PreserveExistingHiddenParent_WhenSavingChildConfiguration()
    {
        var parent = await CreateFormAsync("Existing parent");
        var child = await CreateFormAsync("Existing child");
        var configuration = new FormPaymentConfigurationDto
        {
            ApplicationFormId = child.Id,
            Payable = true,
            FormHierarchy = FormHierarchyType.Child,
            ParentFormId = parent.Id,
            PaymentApprovalThreshold = 2500m
        };
        await _service.SavePaymentConfiguration(configuration);
        await _service.DeleteAsync(parent.Id);

        await _service.SavePaymentConfiguration(configuration);

        var persisted = await _forms.GetAsync(child.Id);
        persisted.ParentFormId.ShouldBe(parent.Id);
        persisted.PaymentApprovalThreshold.ShouldBe(2500m);
        (await _service.GetAsync(parent.Id)).ApplicationFormName.ShouldBe(parent.ApplicationFormName);
    }

    [Fact]
    public async Task Should_RejectConfigurationChanges_FromStaleHiddenFormScreens()
    {
        var form = await CreateFormAsync("Stale configuration");
        await _service.DeleteAsync(form.Id);

        await Should.ThrowAsync<EntityNotFoundException>(() =>
            _service.UpdateAsync(form.Id, new CreateUpdateApplicationFormDto
            {
                IntakeId = form.IntakeId,
                ApplicationFormName = "Changed"
            }));
        await Should.ThrowAsync<EntityNotFoundException>(() =>
            _service.PatchAiConfig(form.Id, new AIConfigDto { AutomaticallyGenerateAIAnalysis = true }));
        await Should.ThrowAsync<EntityNotFoundException>(() =>
            _service.SavePaymentConfiguration(new FormPaymentConfigurationDto { ApplicationFormId = form.Id }));
        (await _forms.GetAsync(form.Id)).IsHiddenFromList.ShouldBeTrue();
        (await _forms.GetAsync(form.Id)).ApplicationFormName.ShouldBe(form.ApplicationFormName);
    }

    [Fact]
    public async Task Should_KeepVisibilityServerManaged_WhenMappingOrdinaryUpdates()
    {
        var form = await CreateFormAsync("Mapper preservation");
        form.HideFromList();
        var mapper = GetRequiredService<IObjectMapper>();

        mapper.Map(new CreateUpdateApplicationFormDto { ApplicationFormName = "Updated" }, form);
        form.IsHiddenFromList.ShouldBeTrue();
        mapper.Map(new ApplicationFormDto { ApplicationFormName = "Another update", IsHiddenFromList = false }, form);
        form.IsHiddenFromList.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_AllowRepeatedDeletion()
    {
        var form = await CreateFormAsync("Repeated deletion");

        await _service.DeleteAsync(form.Id);
        await _service.DeleteAsync(form.Id);

        var persisted = await _forms.GetAsync(form.Id);
        persisted.IsHiddenFromList.ShouldBeTrue();
        persisted.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_GrantDeleteFormsOnlyToProgramManagerByDefault()
    {
        var grants = GetRequiredService<IPermissionGrantRepository>();
        foreach (var role in UnityRoles.DefinedRoles)
        {
            var permissions = await grants.GetListAsync(
                [GrantManagerPermissions.ApplicationForms.Delete], RolePermissionValueProvider.ProviderName, role);

            permissions.Count.ShouldBe(role == UnityRoles.ProgramManager ? 1 : 0);
        }
    }

    [Fact]
    public async Task Should_RespectTenantIsolation_WhenDeleting()
    {
        var tenant = GetRequiredService<ICurrentTenant>();
        var otherTenantId = GetRequiredService<IGuidGenerator>().Create();
        ApplicationForm form;
        using (tenant.Change(otherTenantId))
        {
            form = await CreateFormAsync("Other tenant form");
        }

        await Should.ThrowAsync<EntityNotFoundException>(() => _service.DeleteAsync(form.Id));

        using (tenant.Change(otherTenantId))
        {
            (await _forms.GetAsync(form.Id)).IsHiddenFromList.ShouldBeFalse();
        }
    }

    private async Task<ApplicationForm> CreateFormAsync(string name)
    {
        return await _forms.InsertAsync(new ApplicationForm
        {
            IntakeId = GrantManagerTestData.Intake1_Id,
            ApplicationFormName = name,
            ApiKey = GetRequiredService<IStringEncryptionService>().Encrypt("test-key"),
            Payable = true,
            FormHierarchy = FormHierarchyType.Parent,
            PaymentApprovalThreshold = 5000m,
            TenantId = GetRequiredService<ICurrentTenant>().Id
        }, autoSave: true);
    }
}
