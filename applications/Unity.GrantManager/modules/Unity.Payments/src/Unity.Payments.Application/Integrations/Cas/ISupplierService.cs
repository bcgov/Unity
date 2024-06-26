using System.Threading.Tasks;

using Volo.Abp.Application.Services;

namespace Unity.Payments.Integrations.Cas
{
    public interface ISupplierService : IApplicationService
    {
        Task<dynamic> GetCasSupplierInformationAsync(string? supplierNumber);
    }
}
