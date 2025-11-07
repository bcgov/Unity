using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.ApplicationForms;

public class PaymentConfigurationValidationTests : GrantManagerApplicationTestBase
{
    private readonly IApplicationFormAppService _applicationFormAppService;
    private readonly IApplicationFormRepository _applicationFormRepository;
    private readonly IApplicationFormVersionRepository _applicationFormVersionRepository;

    public PaymentConfigurationValidationTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _applicationFormAppService = GetRequiredService<IApplicationFormAppService>();
        _applicationFormRepository = GetRequiredService<IApplicationFormRepository>();
        _applicationFormVersionRepository = GetRequiredService<IApplicationFormVersionRepository>();
    }

    [Fact]
    public async Task SavePaymentConfiguration_ShouldRequireHierarchy_WhenPayable()
    {
        var exception = await Should.ThrowAsync<BusinessException>(
            _applicationFormAppService.SavePaymentConfiguration(new FormPaymentConfigurationDto
            {
                ApplicationFormId = GrantManagerTestData.ApplicationForm1_Id,
                Payable = true
            }));

        exception.Code.ShouldBe(GrantManagerDomainErrorCodes.PayableFormRequiresHierarchy);
    }

    [Fact]
    public async Task SavePaymentConfiguration_ShouldRequireParent_WhenHierarchyIsChild()
    {
        var exception = await Should.ThrowAsync<BusinessException>(
            _applicationFormAppService.SavePaymentConfiguration(new FormPaymentConfigurationDto
            {
                ApplicationFormId = GrantManagerTestData.ApplicationForm1_Id,
                Payable = true,
                FormHierarchy = FormHierarchyType.Child
            }));

        exception.Code.ShouldBe(GrantManagerDomainErrorCodes.ChildFormRequiresParentForm);
    }

    [Fact]
    public async Task SavePaymentConfiguration_ShouldRejectSelfReference()
    {
        var exception = await Should.ThrowAsync<BusinessException>(
            _applicationFormAppService.SavePaymentConfiguration(new FormPaymentConfigurationDto
            {
                ApplicationFormId = GrantManagerTestData.ApplicationForm1_Id,
                Payable = true,
                FormHierarchy = FormHierarchyType.Child,
                ParentFormId = GrantManagerTestData.ApplicationForm1_Id,
                ParentFormVersionId = Guid.NewGuid()
            }));

        exception.Code.ShouldBe(GrantManagerDomainErrorCodes.ChildFormCannotReferenceSelf);
    }

    [Fact]
    public async Task SavePaymentConfiguration_ShouldValidateParentVersion()
    {
        var parentForm = await _applicationFormRepository.InsertAsync(new ApplicationForm
        {
            IntakeId = GrantManagerTestData.Intake1_Id,
            ApplicationFormName = "Parent Form For Validation",
            Payable = true
        }, autoSave: true);

        var parentVersion = await _applicationFormVersionRepository.InsertAsync(new ApplicationFormVersion
        {
            ApplicationFormId = parentForm.Id,
            Version = 1,
            Published = true
        }, autoSave: true);

        var mismatchException = await Should.ThrowAsync<BusinessException>(
            _applicationFormAppService.SavePaymentConfiguration(new FormPaymentConfigurationDto
            {
                ApplicationFormId = GrantManagerTestData.ApplicationForm1_Id,
                Payable = true,
                FormHierarchy = FormHierarchyType.Child,
                ParentFormId = parentForm.Id,
                ParentFormVersionId = Guid.NewGuid()
            }));

        mismatchException.Code.ShouldBe(GrantManagerDomainErrorCodes.ParentFormVersionMismatch);

        await _applicationFormAppService.SavePaymentConfiguration(new FormPaymentConfigurationDto
        {
            ApplicationFormId = GrantManagerTestData.ApplicationForm1_Id,
            Payable = true,
            FormHierarchy = FormHierarchyType.Child,
            ParentFormId = parentForm.Id,
            ParentFormVersionId = parentVersion.Id
        });
    }
}
