using System;

namespace Unity.GrantManager.ApplicationForms
{
    [Serializable]
    public class FormPaymentConfigurationDto 
    {
        public Guid ApplicationFormId { get; set; }
        public Guid? AccountCodingId { get; set; }
        public bool PreventPayment { get; set; }
        public bool Payable { get; set; }
        public decimal? PaymentApprovalThreshold { get; set; }
        public FormHierarchyType? FormHierarchy { get; set; }
        public Guid? ParentFormId { get; set; }
        public Guid? ParentFormVersionId { get; set; }
    }
}
