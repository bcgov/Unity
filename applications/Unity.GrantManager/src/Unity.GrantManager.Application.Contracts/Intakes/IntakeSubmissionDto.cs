using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Intakes
{
    public class IntakeSubmissionDto
    {
        [Required]
        public Guid FormId { get; set; }
        [Required]
        public Guid SubmissionId { get; set; }
        [Required]
        public string? SubscriptionEvent { get; set; }
    }
}