using System;

namespace Unity.GrantManager.Intakes
{
    public record IntakeSubmissionConfirmationDto
	{
        public Guid ConfirmationId { get; set; }
    }
}