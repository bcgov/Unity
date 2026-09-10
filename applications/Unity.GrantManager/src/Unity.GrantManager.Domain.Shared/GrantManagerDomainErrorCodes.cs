namespace Unity.GrantManager;

public static class GrantManagerDomainErrorCodes
{
    /* You can add your business exception error codes here, as constants */
    public const string OrganizationNameAlreadyExists = "GrantManager:OrganizationNameAlreadyExists";
    public const string GrantProgramNameAlreadyExists = "GrantManager:GrantProgramNameAlreadyExists";
    public const string UserNotFound = "GrantManager:UserNotFound";

    /* APPLICATIONS */
    public const string ApplicationNotFound = "GrantManager:ApplicationNotFound";

    /* ASSESSMENTS */
    public const string AssessmentNotFound = "GrantManager:AssessmentNotFound";
    public const string CannotModifyAiAssessment = "GrantManager:CannotModifyAiAssessment";
    public const string CannotCloneNonAiAssessment = "GrantManager:CannotCloneNonAiAssessment";
    public const string AssessmentUserAssignmentAlreadyExists = "GrantManager:AssessmentUserAssignmentAlreadyExists";
    public const string CantCreateAssessmentForClosedApplication = "GrantManager:CantCreateAssessmentForClosedApplication";
    public const string CantUpdateAssessmentForClosedApplication = "GrantManager:CantUpdateAssessmentForClosedApplication";
    public const string CantCreateAssessmentForFinalStateApplication = "GrantManager:CantCreateAssessmentForFinalStateApplication";

    /* COMMENTS */
    public const string NotCommentOwner = "GrantManager:NotCommentOwner";

    /* PAYMENT CONFIGURATION */
    public const string PayableFormRequiresHierarchy = "GrantManager:PayableFormRequiresHierarchy";
    public const string ChildFormRequiresParentForm = "GrantManager:ChildFormRequiresParentForm";
    public const string ChildFormCannotReferenceSelf = "GrantManager:ChildFormCannotReferenceSelf";

    /* APPLICANT PORTAL EXTERNAL LINKS */
    public const string RenewalLinkRequiredForVisibility = "GrantManager:RenewalLinkRequiredForVisibility";
    public const string RenewalLinkInvalidUri = "GrantManager:RenewalLinkInvalidUri";
    public const string RelatedLinkInvalidUri = "GrantManager:RelatedLinkInvalidUri";
    public const string TooManyRelatedLinks = "GrantManager:TooManyRelatedLinks";

    /* APPLICANT MERGE */
    public const string ApplicantMergeSameApplicant = "GrantManager:ApplicantMergeSameApplicant";
    public const string ApplicantMergeApplicantUnavailable = "GrantManager:ApplicantMergeApplicantUnavailable";
    public const string ApplicantMergeInvalidSelection = "GrantManager:ApplicantMergeInvalidSelection";
    public const string ApplicantMergeInvalidSupplier = "GrantManager:ApplicantMergeInvalidSupplier";
    public const string ApplicantMergeInvalidHistory = "GrantManager:ApplicantMergeInvalidHistory";
    public const string ApplicantMergeAlreadyReversed = "GrantManager:ApplicantMergeAlreadyReversed";
    public const string ApplicantMergeNotLatest = "GrantManager:ApplicantMergeNotLatest";
    public const string ApplicantMergeStateChanged = "GrantManager:ApplicantMergeStateChanged";
    public const string ApplicantMergeRelatedRecordsChanged = "GrantManager:ApplicantMergeRelatedRecordsChanged";
    public const string ApplicantMergePendingPayments = "GrantManager:ApplicantMergePendingPayments";
}
