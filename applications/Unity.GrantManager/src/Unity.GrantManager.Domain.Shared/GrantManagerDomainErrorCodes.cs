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
    public const string AssessmentUserAssignmentAlreadyExists = "GrantManager:AssessmentUserAssignmentAlreadyExists";
    public const string CantCreateAssessmentForClosedApplication = "GrantManager:CantCreateAssessmentForClosedApplication";
    public const string CantUpdateAssessmentForClosedApplication = "GrantManager:CantUpdateAssessmentForClosedApplication";
    public const string CantCreateAssessmentForFinalStateApplication = "GrantManager:CantCreateAssessmentForFinalStateApplication";

    /* PAYMENT CONFIGURATION */
    public const string PayableFormRequiresHierarchy = "GrantManager:PayableFormRequiresHierarchy";
    public const string ChildFormRequiresParentForm = "GrantManager:ChildFormRequiresParentForm";
    public const string ChildFormCannotReferenceSelf = "GrantManager:ChildFormCannotReferenceSelf";
}
