using Shouldly;
using System;
using Volo.Abp;
using Xunit;

namespace Unity.GrantManager.Applications;

public class ApplicantMergeTests
{
    [Fact]
    public void Merge_values_must_be_selected_from_one_of_the_applicants()
    {
        var principal = new Applicant
        {
            ApplicantName = "Principal",
            OrgName = null,
            FiscalDay = 31
        };
        var secondary = new Applicant
        {
            ApplicantName = "Secondary",
            OrgName = "Secondary Org",
            FiscalDay = 30
        };

        new ApplicantMergeValues
        {
            ApplicantName = "Secondary",
            OrgName = string.Empty,
            FiscalDay = 31
        }.IsComposedFrom(principal, secondary).ShouldBeTrue();

        new ApplicantMergeValues
        {
            ApplicantName = "An unrelated value",
            OrgName = "Secondary Org",
            FiscalDay = 30
        }.IsComposedFrom(principal, secondary).ShouldBeFalse();
    }

    [Fact]
    public void Applicant_snapshot_restores_merge_managed_fields()
    {
        var supplierId = Guid.NewGuid();
        var applicant = new Applicant
        {
            ApplicantName = "Before",
            OrgName = "Before Org",
            FiscalDay = 15,
            SupplierId = supplierId,
            IsDuplicated = false
        };
        var snapshot = ApplicantMergeApplicantSnapshot.FromApplicant(applicant);

        applicant.ApplicantName = "After";
        applicant.OrgName = "After Org";
        applicant.FiscalDay = 31;
        applicant.SupplierId = Guid.NewGuid();
        applicant.IsDuplicated = true;

        snapshot.Restore(applicant);

        applicant.ApplicantName.ShouldBe("Before");
        applicant.OrgName.ShouldBe("Before Org");
        applicant.FiscalDay.ShouldBe(15);
        applicant.SupplierId.ShouldBe(supplierId);
        applicant.IsDuplicated.ShouldBeFalse();
    }

    [Fact]
    public void Merge_operation_can_only_be_reversed_once()
    {
        var operation = CreateOperation();
        var reversedBy = Guid.NewGuid();
        var reversedAt = DateTime.UtcNow;

        operation.MarkReversed(reversedBy, reversedAt, "Merged in error");

        operation.Status.ShouldBe(ApplicantMergeStatus.Reversed);
        operation.ReversedById.ShouldBe(reversedBy);
        operation.ReversedAt.ShouldBe(reversedAt);
        operation.ReversalReason.ShouldBe("Merged in error");

        var exception = Should.Throw<BusinessException>(() =>
            operation.MarkReversed(reversedBy, reversedAt, "Try again"));
        exception.Code.ShouldBe(GrantManagerDomainErrorCodes.ApplicantMergeAlreadyReversed);
    }

    [Fact]
    public void Application_change_must_belong_to_its_merge_operation()
    {
        var operation = CreateOperation();
        var change = new ApplicantMergeApplicationChange(
            Guid.NewGuid(),
            tenantId: null,
            applicantMergeOperationId: Guid.NewGuid(),
            applicationId: Guid.NewGuid(),
            wasTransferred: true,
            applicantIdBefore: operation.SecondaryApplicantId,
            applicantIdAfter: operation.PrincipalApplicantId,
            defaultSiteIdBefore: null,
            defaultSiteIdAfter: null,
            relatedRecordsSnapshot: "{}");

        var exception = Should.Throw<BusinessException>(() => operation.AddApplicationChange(change));
        exception.Code.ShouldBe(GrantManagerDomainErrorCodes.ApplicantMergeInvalidHistory);
    }

    private static ApplicantMergeOperation CreateOperation()
    {
        return new ApplicantMergeOperation(
            Guid.NewGuid(),
            tenantId: null,
            principalApplicantId: Guid.NewGuid(),
            secondaryApplicantId: Guid.NewGuid(),
            source: ApplicantMergeSource.ApplicantList,
            principalStateBefore: "{}",
            principalStateAfter: "{}",
            secondaryStateBefore: "{}",
            secondaryStateAfter: "{}",
            mergedAt: DateTime.UtcNow,
            mergedById: Guid.NewGuid());
    }
}
