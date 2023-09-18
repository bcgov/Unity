using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace Unity.GrantManager.GrantApplications;

public class ApplicationStatusConsts
{
    public const int MaxNameLength = 64;

    public const string IN_PROGRESS = "IN_PROGRESS";
    public const string SUBMITTED = "SUBMITTED";
    public const string ASSIGNED = "ASSIGNED";
    public const string WITHDRAWN = "WITHDRAWN";
    public const string CLOSED = "CLOSED";
    public const string UNDER_INITIAL_REVIEW = "UNDER_INITIAL_REVIEW";
    public const string INITITAL_REVIEW_COMPLETED = "INITITAL_REVIEW_COMPLETED";
    public const string UNDER_ADJUDICATION = "UNDER_ADJUDICATION";
    public const string ADJUDICATION_COMPLETED = "ADJUDICATION_COMPLETED";
    public const string GRANT_APPROVED = "GRANT_APPROVED";
    public const string GRANT_NOT_APPROVED = "GRANT_NOT_APPROVED";
}
