using System.Threading.Tasks;
using Unity.Payments.Suppliers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Unity.Payments.Handlers
{
    public class UpsertSupplierHandler : ILocalEventHandler<UpsertSupplierEto>, ITransientDependency
    {
        private readonly ISupplierAppService _supplierAppService;

        public UpsertSupplierHandler(ISupplierAppService supplierAppService)
        {
            _supplierAppService = supplierAppService;
        }

        public async Task HandleEventAsync(UpsertSupplierEto eventData)
        {
            var existing = await _supplierAppService.GetByCorrelationAsync(new GetSupplierByCorrelationDto()
            {
                CorrelationId = eventData.CorrelationId,
                CorrelationProvider = eventData.CorrelationProvider,                
            });

            // This is subject to some business rules and a domain implmentation
            if (existing != null)
            {
                existing.Number = eventData.SupplierNumber;
                await _supplierAppService.UpdateAsync(existing.Id, new UpdateSupplierDto()
                {
                    Number = eventData.SupplierNumber,
                    MailingAddress = existing.MailingAddress,
                    Name = existing.Name,
                    PostalCode = existing.PostalCode,
                    Province = existing.Province,
                    City = existing.City
                });
            }
            else
            {
                await _supplierAppService.CreateAsync(new CreateSupplierDto()
                {
                    Number = eventData.SupplierNumber,
                    CorrelationId = eventData.CorrelationId,
                    CorrelationProvider = eventData.CorrelationProvider,
                });
            }
        }
    }
}
