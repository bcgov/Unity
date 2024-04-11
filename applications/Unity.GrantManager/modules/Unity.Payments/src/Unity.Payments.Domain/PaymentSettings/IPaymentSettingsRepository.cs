using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.PaymentSettings;

public interface IPaymentSettingsRepository : IRepository<PaymentSetting, Guid>
{

}
