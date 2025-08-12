using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Payments.PaymentThresholds;

namespace Unity.GrantManager.Payments;

public interface IPaymentSettingsAppService
{
    Task<List<PaymentThresholdDto>> GetL2ApproversThresholds();
    Task<Guid?> GetAccountCodingIdByApplicationIdAsync(Guid applicationId);
}
