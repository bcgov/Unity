using System;
namespace Unity.GrantManager.Models
{
	public class IntakeSubmission
	{
        public required string formId { get; set; }
        public required string formVersion { get; set; }
        public required string submissionId { get; set; }
        public required string subscriptionEvent { get; set; }

        public IntakeSubmission(string formId, string formVersion, string submissionId, string subscriptionEvent)
        {
            this.formId = formId;
            this.formVersion = formVersion;
            this.submissionId = submissionId;
            this.subscriptionEvent = subscriptionEvent;
        }
    }
}

