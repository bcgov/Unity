using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Events
{
    public class EventSubscriptionDto
    {
        [Required]
        public Guid FormId { get; set; }
        [Required]
        public Guid SubmissionId { get; set; }
        [Required]
        public string? SubscriptionEvent { get; set; }
    }
}