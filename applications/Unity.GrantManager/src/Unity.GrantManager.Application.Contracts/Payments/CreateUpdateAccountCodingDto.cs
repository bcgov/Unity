using System.ComponentModel;

namespace Unity.GrantManager.Payments
{
    public class CreateUpdateAccountCodingDto
    {        
        [DisplayName("Ministry Client")]
        public string? MinistryClient { get; private set; } = string.Empty;

        [DisplayName("Responsibility")]
        public string? Responsibility { get; private set; } = string.Empty;

        [DisplayName("Service Line")]
        public string? ServiceLine { get; private set; } = string.Empty;
        [DisplayName("Stob")]
        public string? Stob { get; private set; } = string.Empty;
        [DisplayName("Project Number")]
        public string? ProjectNumber { get; private set; } = string.Empty;
    }
}
