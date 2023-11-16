using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Events
{
    public class EventSubscriptionDto
    {
        [Required]
        public Guid FormId { get; set; }

        public Guid FormVersion { get; set; }

        public Guid SubmissionId { get; set; }

        [Required]
        public string? SubscriptionEvent { get; set; }
    }
}