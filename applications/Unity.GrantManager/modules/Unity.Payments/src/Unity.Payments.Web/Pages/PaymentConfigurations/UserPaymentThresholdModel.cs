using System;

namespace Unity.Payments.Web.Pages.PaymentApprovals
{
  public class PaymentThresholdModel
  {
      public Guid Id { get; set; }
      public Guid? UserId { get; set; }
      public decimal? PaymentThreshold { get; set; }
      public string? Description { get; set; }
      public string? UserName { get; set; }            
  }
}
