using System;

namespace Unity.GrantManager.Events
{
    public record EventSubscriptionConfirmationDto
	{
        public Guid? ConfirmationId { get; set; }
        public string? ExceptionMessage { get; set; }
    }
}
