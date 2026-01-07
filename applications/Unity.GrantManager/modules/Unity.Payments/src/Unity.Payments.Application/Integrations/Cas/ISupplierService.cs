using System;
using System.Threading.Tasks;

using Volo.Abp.Application.Services;

namespace Unity.Payments.Integrations.Cas
{
    public interface ISupplierService : IApplicationService
    {
        Task<dynamic> GetCasSupplierInformationByBn9Async(string? bn9);
        Task<dynamic> GetCasSupplierInformationAsync(string? supplierNumber);
        Task UpdateApplicantSupplierInfo(string? supplierNumber, Guid applicantId, Guid? applicationId = null);
        Task<dynamic> UpdateApplicantSupplierInfoByBn9(string? bn9, Guid applicantId);
        Task UpdateSupplierInfo(dynamic casSupplierResponse, Guid applicantId, Guid? applicationId = null);
    }
}
