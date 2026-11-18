using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Integrations.Chefs;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Encryption;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.ApplicationForms;

/// <summary>
/// Covers how a Unity form is linked to a CHEFS form: the link must be validated against
/// CHEFS, and one CHEFS form may back only one visible Unity form.
/// </summary>
public class ApplicationFormChefsRegistrationTests : GrantManagerApplicationTestBase
{
    private const string ChefsFormName = "CHEFS Form Name";

    private IFormsApiService _formsApiService = null!;
    private readonly IApplicationFormAppService _service;
    private readonly IApplicationFormRepository _forms;

    public ApplicationFormChefsRegistrationTests(ITestOutputHelper output) : base(output)
    {
        _service = GetRequiredService<IApplicationFormAppService>();
        _forms = GetRequiredService<IApplicationFormRepository>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        _formsApiService = Substitute.For<IFormsApiService>();
        _formsApiService
            .GetForm(Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new JObject { ["name"] = ChefsFormName });
        services.AddSingleton(_formsApiService);
    }

    [Fact]
    public async Task UpdateAsync_Should_Reject_ChefsFormId_Linked_To_Another_Visible_Form()
    {
        var chefsFormId = Guid.NewGuid().ToString();
        var original = await CreateFormAsync("Original form", chefsFormId);
        var other = await CreateFormAsync("Other form", Guid.NewGuid().ToString());

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            _service.UpdateAsync(other.Id, UpdateInput(other, chefsFormId.ToUpperInvariant(), "other-key")));

        exception.Code.ShouldBe(GrantManagerDomainErrorCodes.ChefsFormAlreadyRegistered);
        exception.Data["ApplicationFormName"].ShouldBe(original.ApplicationFormName);
        (await _forms.GetAsync(other.Id)).ChefsApplicationFormGuid.ShouldNotBe(chefsFormId);
    }

    [Fact]
    public async Task UpdateAsync_Should_Reject_New_ApiKey_On_A_Duplicate_Form()
    {
        // A form that already shares its CHEFS Form ID with an older form cannot be
        // completed into a second working copy of it.
        var chefsFormId = Guid.NewGuid().ToString();
        await CreateFormAsync("Original form", chefsFormId);
        var duplicate = await CreateFormAsync("Duplicate form", chefsFormId, apiKey: null);

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            _service.UpdateAsync(duplicate.Id, UpdateInput(duplicate, chefsFormId, "new-key")));

        exception.Code.ShouldBe(GrantManagerDomainErrorCodes.ChefsFormAlreadyRegistered);
    }

    [Fact]
    public async Task UpdateAsync_Should_Allow_ChefsFormId_Of_A_Deleted_Form()
    {
        var chefsFormId = Guid.NewGuid().ToString();
        var hidden = await CreateFormAsync("Deleted form", chefsFormId);
        await _service.DeleteAsync(hidden.Id);
        var form = await CreateFormAsync("Replacement form", Guid.NewGuid().ToString());

        await _service.UpdateAsync(form.Id, UpdateInput(form, chefsFormId, "test-key"));

        var persisted = await _forms.GetAsync(form.Id);
        persisted.ChefsApplicationFormGuid.ShouldBe(chefsFormId);
        persisted.ApplicationFormName.ShouldBe(ChefsFormName);
    }

    [Fact]
    public async Task UpdateAsync_Should_Fail_When_Chefs_Rejects_The_Credentials()
    {
        var form = await CreateFormAsync("Unverified form", Guid.NewGuid().ToString());
        var newChefsFormId = Guid.NewGuid().ToString();
        _formsApiService
            .GetForm(Arg.Any<Guid?>(), newChefsFormId, Arg.Any<string>())
            .Returns(new JObject { ["detail"] = "Unauthorized" });

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            _service.UpdateAsync(form.Id, UpdateInput(form, newChefsFormId, "wrong-key")));

        exception.Code.ShouldBe(GrantManagerDomainErrorCodes.ChefsFormInitializationFailed);
        (await _forms.GetAsync(form.Id)).ChefsApplicationFormGuid.ShouldBe(form.ChefsApplicationFormGuid);
    }

    [Fact]
    public async Task UpdateAsync_Should_Fail_When_Chefs_Is_Unreachable()
    {
        var form = await CreateFormAsync("Unreachable form", Guid.NewGuid().ToString());
        var newChefsFormId = Guid.NewGuid().ToString();
        _formsApiService
            .GetForm(Arg.Any<Guid?>(), newChefsFormId, Arg.Any<string>())
            .ThrowsAsync(new HttpRequestException("CHEFS unavailable"));

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            _service.UpdateAsync(form.Id, UpdateInput(form, newChefsFormId, "test-key")));

        exception.Code.ShouldBe(GrantManagerDomainErrorCodes.ChefsFormInitializationFailed);
    }

    [Fact]
    public async Task UpdateAsync_Should_Fail_When_ApiKey_Is_Missing()
    {
        var form = await CreateFormAsync("Form without key", Guid.NewGuid().ToString(), apiKey: null);

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            _service.UpdateAsync(form.Id, UpdateInput(form, Guid.NewGuid().ToString(), apiKey: null)));

        exception.Code.ShouldBe(GrantManagerDomainErrorCodes.ChefsFormInitializationFailed);
    }

    [Fact]
    public async Task UpdateAsync_Should_Save_Chefs_Form_Name_When_Credentials_Are_Valid()
    {
        var form = await CreateFormAsync("Valid form", Guid.NewGuid().ToString());
        var newChefsFormId = Guid.NewGuid().ToString();

        var result = await _service.UpdateAsync(form.Id, UpdateInput(form, newChefsFormId, "new-key"));

        result.ApplicationFormName.ShouldBe(ChefsFormName);
        var persisted = await _forms.GetAsync(form.Id);
        persisted.ChefsApplicationFormGuid.ShouldBe(newChefsFormId);
        persisted.ApplicationFormName.ShouldBe(ChefsFormName);
    }

    [Fact]
    public async Task CreateAsync_Should_Reject_ChefsFormId_Linked_To_Another_Visible_Form()
    {
        var chefsFormId = Guid.NewGuid().ToString();
        await CreateFormAsync("Original form", chefsFormId);

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            _service.CreateAsync(new CreateUpdateApplicationFormDto
            {
                IntakeId = GrantManagerTestData.Intake1_Id,
                ApplicationFormName = string.Empty,
                ChefsApplicationFormGuid = chefsFormId,
                ApiKey = "test-key"
            }));

        exception.Code.ShouldBe(GrantManagerDomainErrorCodes.ChefsFormAlreadyRegistered);
        (await WithUnitOfWorkAsync(() => _forms.CountAsync(s => s.ChefsApplicationFormGuid == chefsFormId)))
            .ShouldBe(1);
    }

    [Fact]
    public async Task CreateAsync_Should_Not_Save_Form_When_Chefs_Does_Not_Recognise_It()
    {
        // e.g. a CHEFS Test form ID entered in an environment that talks to CHEFS Prod
        var unknownChefsFormId = Guid.NewGuid().ToString();
        _formsApiService
            .GetForm(Arg.Any<Guid?>(), unknownChefsFormId, Arg.Any<string>())
            .Returns(new JObject { ["status"] = 401, ["detail"] = "Unauthorized" });

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            _service.CreateAsync(new CreateUpdateApplicationFormDto
            {
                IntakeId = GrantManagerTestData.Intake1_Id,
                ApplicationFormName = string.Empty,
                ChefsApplicationFormGuid = unknownChefsFormId,
                ApiKey = "test-environment-key"
            }));

        exception.Code.ShouldBe(GrantManagerDomainErrorCodes.ChefsFormInitializationFailed);
        (await WithUnitOfWorkAsync(() => _forms.CountAsync(s => s.ChefsApplicationFormGuid == unknownChefsFormId)))
            .ShouldBe(0);
    }

    private static CreateUpdateApplicationFormDto UpdateInput(ApplicationForm form, string chefsFormId, string? apiKey) => new()
    {
        IntakeId = form.IntakeId,
        ApplicationFormName = form.ApplicationFormName ?? string.Empty,
        ChefsApplicationFormGuid = chefsFormId,
        ApiKey = apiKey
    };

    private async Task<ApplicationForm> CreateFormAsync(string name, string chefsFormId, string? apiKey = "test-key")
    {
        return await _forms.InsertAsync(new ApplicationForm
        {
            IntakeId = GrantManagerTestData.Intake1_Id,
            ApplicationFormName = name,
            ChefsApplicationFormGuid = chefsFormId,
            ApiKey = GetRequiredService<IStringEncryptionService>().Encrypt(apiKey),
            TenantId = GetRequiredService<ICurrentTenant>().Id
        }, autoSave: true);
    }
}
