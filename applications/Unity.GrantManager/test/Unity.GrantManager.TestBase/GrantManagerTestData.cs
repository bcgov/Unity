using System;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Comments;
using Unity.GrantManager.Intakes;

namespace Unity.GrantManager;
public static class GrantManagerTestData
{
    public static readonly Guid User1_UserId = new("4dc19222-3ae7-478b-a3d8-3c8e769f5ef7");
    public static readonly string User1_UserName = "user-assessor1";
    public static readonly string User1_EmailAddress = "user_assessor1@gov.bc.ca.test";

    public static readonly Guid User2_UserId = new("7cb0124c-f0bd-462c-aa9a-281fbc734821");
    public static readonly string User2_UserName = "user-assessor2";
    public static readonly string User2_EmailAddress = "user_assessor2@gov.bc.ca.test";

    public static readonly Guid Application1_Id = new("260f5158-7421-11ee-b962-0242ac120002");
    public static readonly Guid Application2_Id = new("260f5400-7421-11ee-b962-0242ac120002");

    public static readonly Guid Applicant1_Id = new("260f552c-7421-11ee-b962-0242ac120002");
    public static readonly Guid Applicant2_Id = new("260f5658-7421-11ee-b962-0242ac120002");

    public static readonly Guid Intake1_Id = new("260f5824-7421-11ee-b962-0242ac120002");

    public static readonly Guid ApplicationForm1_Id = new("260f59dc-7421-11ee-b962-0242ac120002");

    public static readonly Guid ApplicationComment1_Id = new("260f5b30-7421-11ee-b962-0242ac120002");

    public static readonly Guid ApplicationAttachment1_Id = new("260f5b30-7421-22ff-b962-0242ac120002");

    public static readonly Guid Assessment1_Id = new("260f5b30-7321-22ff-c962-1242ac120002");

    public static readonly Guid AssessmentAttachment1_Id = new("260f5b20-6321-22ff-c962-1242ac120001");

    public static readonly Guid AssessmentComment1_Id = new("270a2c20-6321-22ff-c962-1242ac123101");
}

public class ApplicantSeed : Applicant
{
    public ApplicantSeed(Guid id)
    {
        Id = id;
    }
}

public class IntakeSeed : Intake
{
    public IntakeSeed(Guid id)
    {
        Id = id;
    }
}

public class ApplicationFormSeed : ApplicationForm
{
    public ApplicationFormSeed(Guid id)
    {
        Id = id;
    }
}

public class ApplicationSeed : Application
{
    public ApplicationSeed(Guid id)
    {
        Id = id;
    }
}

public class ApplicationCommentSeed : ApplicationComment
{
    public ApplicationCommentSeed(Guid id)
    {
        Id = id;
    }
}

public class ApplicationAttachmentSeed : ApplicationAttachment
{
    public ApplicationAttachmentSeed(Guid id)
    {
        Id = id;
    }
}

public class AssessmentAttachmentSeed : AssessmentAttachment
{
    public AssessmentAttachmentSeed(Guid id)
    {
        Id = id;
    }
}

public class AssessmentCommentSeed : AssessmentComment
{
    public AssessmentCommentSeed(Guid id)
    {
        Id = id;
    }
}