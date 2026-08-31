using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Intakes;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Unity.GrantManager.Applications;

public class ApplicantMergeManagerTests : GrantManagerEntityFrameworkCoreTestBase
{
    private readonly ApplicantMergeManager _mergeManager;
    private readonly IApplicantRepository _applicantRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationFormSubmissionRepository _formSubmissionRepository;
    private readonly IApplicantAgentRepository _applicantAgentRepository;
    private readonly IApplicantAddressRepository _applicantAddressRepository;
    private readonly IApplicantMergeOperationRepository _mergeOperationRepository;
    private readonly IRepository<Application, Guid> _applicationEntityRepository;
    private readonly IRepository<ApplicationForm, Guid> _applicationFormRepository;
    private readonly IRepository<ApplicationStatus, Guid> _applicationStatusRepository;
    private readonly IRepository<Intake, Guid> _intakeRepository;

    public ApplicantMergeManagerTests()
    {
        _mergeManager = GetRequiredService<ApplicantMergeManager>();
        _applicantRepository = GetRequiredService<IApplicantRepository>();
        _applicationRepository = GetRequiredService<IApplicationRepository>();
        _formSubmissionRepository = GetRequiredService<IApplicationFormSubmissionRepository>();
        _applicantAgentRepository = GetRequiredService<IApplicantAgentRepository>();
        _applicantAddressRepository = GetRequiredService<IApplicantAddressRepository>();
        _mergeOperationRepository = GetRequiredService<IApplicantMergeOperationRepository>();
        _applicationEntityRepository = GetRequiredService<IRepository<Application, Guid>>();
        _applicationFormRepository = GetRequiredService<IRepository<ApplicationForm, Guid>>();
        _applicationStatusRepository = GetRequiredService<IRepository<ApplicationStatus, Guid>>();
        _intakeRepository = GetRequiredService<IRepository<Intake, Guid>>();
    }

    [Theory]
    [InlineData(ApplicantMergeSource.ApplicantList)]
    [InlineData(ApplicantMergeSource.ApplicationDetails)]
    public async Task Merge_and_unmerge_should_round_trip_applicant_and_related_records(
        ApplicantMergeSource source)
    {
        var fixture = await CreateMergeFixtureAsync();

        var operationId = await WithUnitOfWorkAsync(async () =>
        {
            var operation = await _mergeManager.MergeAsync(
                fixture.PrincipalApplicantId,
                fixture.SecondaryApplicantId,
                new ApplicantMergeValues
                {
                    ApplicantName = fixture.SecondaryApplicantName,
                    UnityApplicantId = fixture.PrincipalUnityApplicantId,
                    OrgName = fixture.PrincipalOrgName,
                    FiscalDay = fixture.SecondaryFiscalDay
                },
                fixture.SecondarySupplierId,
                source,
                tenantId: null,
                mergedById: null);

            return operation.Id;
        });

        await AssertMergedStateAsync(fixture, operationId, source);

        await WithUnitOfWorkAsync(async () =>
        {
            await _mergeManager.UnmergeAsync(
                operationId,
                reversedById: null,
                reason: "Merged in error");
        });

        await AssertRestoredStateAsync(fixture, operationId);
    }

    [Fact]
    public async Task Earlier_merge_should_be_blocked_until_later_merge_is_reversed()
    {
        var applicantIds = await WithUnitOfWorkAsync(async () =>
        {
            var first = await _applicantRepository.InsertAsync(
                new Applicant { ApplicantName = "Applicant A" }, autoSave: true);
            var second = await _applicantRepository.InsertAsync(
                new Applicant { ApplicantName = "Applicant B" }, autoSave: true);
            var third = await _applicantRepository.InsertAsync(
                new Applicant { ApplicantName = "Applicant C" }, autoSave: true);

            return (First: first.Id, Second: second.Id, Third: third.Id);
        });

        var firstOperationId = await WithUnitOfWorkAsync(async () =>
        {
            var operation = await _mergeManager.MergeAsync(
                applicantIds.First,
                applicantIds.Second,
                new ApplicantMergeValues { ApplicantName = "Applicant A" },
                selectedSupplierId: null,
                source: ApplicantMergeSource.ApplicantList,
                tenantId: null,
                mergedById: null);
            return operation.Id;
        });

        // MergedAt defines the reversal order. Keep the test independent of the
        // platform clock's minimum resolution when creating consecutive operations.
        await Task.Delay(TimeSpan.FromMilliseconds(20));

        var secondOperationId = await WithUnitOfWorkAsync(async () =>
        {
            var operation = await _mergeManager.MergeAsync(
                applicantIds.First,
                applicantIds.Third,
                new ApplicantMergeValues { ApplicantName = "Applicant A" },
                selectedSupplierId: null,
                source: ApplicantMergeSource.ApplicationDetails,
                tenantId: null,
                mergedById: null);
            return operation.Id;
        });

        var firstReversibility = await WithUnitOfWorkAsync(
            () => _mergeManager.GetReversibilityAsync(firstOperationId));
        var secondReversibility = await WithUnitOfWorkAsync(
            () => _mergeManager.GetReversibilityAsync(secondOperationId));

        firstReversibility.CanReverse.ShouldBeFalse();
        firstReversibility.ErrorCode.ShouldBe(GrantManagerDomainErrorCodes.ApplicantMergeNotLatest);
        secondReversibility.CanReverse.ShouldBeTrue();

        var blockedException = await Should.ThrowAsync<BusinessException>(() =>
            WithUnitOfWorkAsync(async () =>
            {
                await _mergeManager.UnmergeAsync(
                    firstOperationId,
                    reversedById: null,
                    reason: "Attempt out-of-order reversal");
            }));
        blockedException.Code.ShouldBe(GrantManagerDomainErrorCodes.ApplicantMergeNotLatest);

        await WithUnitOfWorkAsync(async () =>
        {
            await _mergeManager.UnmergeAsync(
                secondOperationId,
                reversedById: null,
                reason: "Reverse latest merge");
        });

        var firstAfterLatestWasReversed = await WithUnitOfWorkAsync(
            () => _mergeManager.GetReversibilityAsync(firstOperationId));

        firstAfterLatestWasReversed.CanReverse.ShouldBeTrue();
        firstAfterLatestWasReversed.ErrorCode.ShouldBeNull();

        await WithUnitOfWorkAsync(async () =>
        {
            await _mergeManager.UnmergeAsync(
                firstOperationId,
                reversedById: null,
                reason: "Reverse original merge");
        });

        var firstOperation = await WithUnitOfWorkAsync(
            () => _mergeOperationRepository.GetWithChangesAsync(firstOperationId));
        firstOperation.Status.ShouldBe(ApplicantMergeStatus.Reversed);
    }

    private async Task<MergeFixture> CreateMergeFixtureAsync()
    {
        return await WithUnitOfWorkAsync(async () =>
        {
            var intake = await _intakeRepository.InsertAsync(new Intake
            {
                IntakeName = $"Merge test {Guid.NewGuid():N}",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1)
            }, autoSave: true);

            var applicationForm = await _applicationFormRepository.InsertAsync(new ApplicationForm
            {
                IntakeId = intake.Id,
                ApplicationFormName = $"Merge test form {Guid.NewGuid():N}",
                ChefsApplicationFormGuid = Guid.NewGuid().ToString()
            }, autoSave: true);

            var applicationStatus = await _applicationStatusRepository.FirstOrDefaultAsync(
                item => item.StatusCode == GrantApplicationState.SUBMITTED);
            if (applicationStatus == null)
            {
                applicationStatus = await _applicationStatusRepository.InsertAsync(new ApplicationStatus
                {
                    StatusCode = GrantApplicationState.SUBMITTED,
                    ExternalStatus = "Submitted",
                    InternalStatus = "Submitted"
                }, autoSave: true);
            }

            var principalSupplierId = Guid.NewGuid();
            var secondarySupplierId = Guid.NewGuid();
            var principal = await _applicantRepository.InsertAsync(new Applicant
            {
                ApplicantName = "Principal before merge",
                UnityApplicantId = "PRINCIPAL-ID",
                OrgName = "Principal organization",
                FiscalDay = 15,
                SupplierId = principalSupplierId,
                IsDuplicated = true
            }, autoSave: true);
            var secondary = await _applicantRepository.InsertAsync(new Applicant
            {
                ApplicantName = "Secondary before merge",
                UnityApplicantId = "SECONDARY-ID",
                OrgName = "Secondary organization",
                FiscalDay = 28,
                SupplierId = secondarySupplierId,
                IsDuplicated = false
            }, autoSave: true);

            var principalDefaultSiteId = Guid.NewGuid();
            var secondaryDefaultSiteId = Guid.NewGuid();
            var principalApplication = await _applicationEntityRepository.InsertAsync(new Application
            {
                ApplicantId = principal.Id,
                ApplicationFormId = applicationForm.Id,
                ApplicationStatusId = applicationStatus.Id,
                ProjectName = "Principal application",
                ReferenceNo = $"P-{Guid.NewGuid():N}",
                DefaultSiteId = principalDefaultSiteId
            }, autoSave: true);
            var secondaryApplication = await _applicationEntityRepository.InsertAsync(new Application
            {
                ApplicantId = secondary.Id,
                ApplicationFormId = applicationForm.Id,
                ApplicationStatusId = applicationStatus.Id,
                ProjectName = "Secondary application",
                ReferenceNo = $"S-{Guid.NewGuid():N}",
                DefaultSiteId = secondaryDefaultSiteId
            }, autoSave: true);

            var formSubmission = await _formSubmissionRepository.InsertAsync(new ApplicationFormSubmission
            {
                ApplicantId = secondary.Id,
                ApplicationId = secondaryApplication.Id,
                ApplicationFormId = applicationForm.Id,
                OidcSub = "merge-test-user",
                ChefsSubmissionGuid = Guid.NewGuid().ToString(),
                Submission = "{}"
            }, autoSave: true);
            var applicantAgent = await _applicantAgentRepository.InsertAsync(new ApplicantAgent
            {
                ApplicantId = secondary.Id,
                ApplicationId = secondaryApplication.Id,
                Name = "Merge Test Agent",
                Email = "merge.test@example.com"
            }, autoSave: true);
            var physicalAddress = await _applicantAddressRepository.InsertAsync(new ApplicantAddress
            {
                ApplicantId = secondary.Id,
                ApplicationId = secondaryApplication.Id,
                AddressType = AddressType.PhysicalAddress,
                Street = "100 Test Street",
                City = "Victoria"
            }, autoSave: true);
            var mailingAddress = await _applicantAddressRepository.InsertAsync(new ApplicantAddress
            {
                ApplicantId = secondary.Id,
                ApplicationId = secondaryApplication.Id,
                AddressType = AddressType.MailingAddress,
                Street = "PO Box 100",
                City = "Victoria"
            }, autoSave: true);

            return new MergeFixture(
                principal.Id,
                secondary.Id,
                principalApplication.Id,
                secondaryApplication.Id,
                formSubmission.Id,
                applicantAgent.Id,
                physicalAddress.Id,
                mailingAddress.Id,
                principalSupplierId,
                secondarySupplierId,
                principalDefaultSiteId,
                secondaryDefaultSiteId,
                principal.ApplicantName!,
                secondary.ApplicantName!,
                principal.UnityApplicantId!,
                principal.OrgName!,
                principal.FiscalDay,
                secondary.FiscalDay);
        });
    }

    private async Task AssertMergedStateAsync(
        MergeFixture fixture,
        Guid operationId,
        ApplicantMergeSource source)
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var principal = await _applicantRepository.GetAsync(fixture.PrincipalApplicantId);
            var secondary = await _applicantRepository.GetAsync(fixture.SecondaryApplicantId);
            var principalApplication = await _applicationRepository.GetAsync(fixture.PrincipalApplicationId);
            var secondaryApplication = await _applicationRepository.GetAsync(fixture.SecondaryApplicationId);
            var formSubmission = await _formSubmissionRepository.GetAsync(fixture.FormSubmissionId);
            var applicantAgent = await _applicantAgentRepository.GetAsync(fixture.ApplicantAgentId);
            var physicalAddress = await _applicantAddressRepository.GetAsync(fixture.PhysicalAddressId);
            var mailingAddress = await _applicantAddressRepository.GetAsync(fixture.MailingAddressId);
            var operation = await _mergeOperationRepository.GetWithChangesAsync(operationId);

            principal.ApplicantName.ShouldBe(fixture.SecondaryApplicantName);
            principal.UnityApplicantId.ShouldBe(fixture.PrincipalUnityApplicantId);
            principal.OrgName.ShouldBe(fixture.PrincipalOrgName);
            principal.FiscalDay.ShouldBe(fixture.SecondaryFiscalDay);
            principal.SupplierId.ShouldBe(fixture.SecondarySupplierId);
            principal.IsDuplicated.ShouldBeFalse();
            secondary.SupplierId.ShouldBe(fixture.SecondarySupplierId);
            secondary.IsDuplicated.ShouldBeTrue();

            principalApplication.ApplicantId.ShouldBe(fixture.PrincipalApplicantId);
            principalApplication.DefaultSiteId.ShouldBeNull();
            secondaryApplication.ApplicantId.ShouldBe(fixture.PrincipalApplicantId);
            secondaryApplication.DefaultSiteId.ShouldBeNull();
            formSubmission.ApplicantId.ShouldBe(fixture.PrincipalApplicantId);
            applicantAgent.ApplicantId.ShouldBe(fixture.PrincipalApplicantId);
            physicalAddress.ApplicantId.ShouldBe(fixture.PrincipalApplicantId);
            mailingAddress.ApplicantId.ShouldBe(fixture.PrincipalApplicantId);

            operation.Status.ShouldBe(ApplicantMergeStatus.Completed);
            operation.Source.ShouldBe(source);
            operation.ApplicationChanges.Count.ShouldBe(2);
            operation.ApplicationChanges.Count(item => item.WasTransferred).ShouldBe(1);
            operation.ApplicationChanges.Single(item => item.WasTransferred).ApplicationId
                .ShouldBe(fixture.SecondaryApplicationId);
        });
    }

    private async Task AssertRestoredStateAsync(MergeFixture fixture, Guid operationId)
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var principal = await _applicantRepository.GetAsync(fixture.PrincipalApplicantId);
            var secondary = await _applicantRepository.GetAsync(fixture.SecondaryApplicantId);
            var principalApplication = await _applicationRepository.GetAsync(fixture.PrincipalApplicationId);
            var secondaryApplication = await _applicationRepository.GetAsync(fixture.SecondaryApplicationId);
            var formSubmission = await _formSubmissionRepository.GetAsync(fixture.FormSubmissionId);
            var applicantAgent = await _applicantAgentRepository.GetAsync(fixture.ApplicantAgentId);
            var physicalAddress = await _applicantAddressRepository.GetAsync(fixture.PhysicalAddressId);
            var mailingAddress = await _applicantAddressRepository.GetAsync(fixture.MailingAddressId);
            var operation = await _mergeOperationRepository.GetWithChangesAsync(operationId);

            principal.ApplicantName.ShouldBe(fixture.PrincipalApplicantName);
            principal.UnityApplicantId.ShouldBe(fixture.PrincipalUnityApplicantId);
            principal.OrgName.ShouldBe(fixture.PrincipalOrgName);
            principal.FiscalDay.ShouldBe(fixture.PrincipalFiscalDay);
            principal.SupplierId.ShouldBe(fixture.PrincipalSupplierId);
            principal.IsDuplicated.ShouldBeTrue();
            secondary.ApplicantName.ShouldBe(fixture.SecondaryApplicantName);
            secondary.SupplierId.ShouldBe(fixture.SecondarySupplierId);
            secondary.IsDuplicated.ShouldBeFalse();

            principalApplication.ApplicantId.ShouldBe(fixture.PrincipalApplicantId);
            principalApplication.DefaultSiteId.ShouldBe(fixture.PrincipalDefaultSiteId);
            secondaryApplication.ApplicantId.ShouldBe(fixture.SecondaryApplicantId);
            secondaryApplication.DefaultSiteId.ShouldBe(fixture.SecondaryDefaultSiteId);
            formSubmission.ApplicantId.ShouldBe(fixture.SecondaryApplicantId);
            applicantAgent.ApplicantId.ShouldBe(fixture.SecondaryApplicantId);
            physicalAddress.ApplicantId.ShouldBe(fixture.SecondaryApplicantId);
            mailingAddress.ApplicantId.ShouldBe(fixture.SecondaryApplicantId);

            operation.Status.ShouldBe(ApplicantMergeStatus.Reversed);
            operation.ReversalReason.ShouldBe("Merged in error");
            operation.ReversedAt.ShouldNotBeNull();
        });
    }

    private sealed record MergeFixture(
        Guid PrincipalApplicantId,
        Guid SecondaryApplicantId,
        Guid PrincipalApplicationId,
        Guid SecondaryApplicationId,
        Guid FormSubmissionId,
        Guid ApplicantAgentId,
        Guid PhysicalAddressId,
        Guid MailingAddressId,
        Guid PrincipalSupplierId,
        Guid SecondarySupplierId,
        Guid PrincipalDefaultSiteId,
        Guid SecondaryDefaultSiteId,
        string PrincipalApplicantName,
        string SecondaryApplicantName,
        string PrincipalUnityApplicantId,
        string PrincipalOrgName,
        int? PrincipalFiscalDay,
        int? SecondaryFiscalDay);
}
