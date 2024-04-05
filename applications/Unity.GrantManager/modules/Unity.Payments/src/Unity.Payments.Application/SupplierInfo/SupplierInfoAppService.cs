using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Payments.Enums;
using Unity.Payments.Settings;
using Volo.Abp.Features;
using Volo.Abp.Users;

namespace Unity.Payments.SupplierInfo
{
    public class SupplierInfoAppService : PaymentsAppService, ISupplierInfoAppService
    {
        private readonly ICurrentUser _currentUser;

        public SupplierInfoAppService(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public Task<List<SiteDto>> GetSitesAsync(Guid applicationId)
        {
            List<SiteDto> sites = new List<SiteDto>();
            sites.Add(new SiteDto { AddressLine1 = "777 Hockley Avenue", AddressLine2 = "Unit 201", City="Langford", Province="British Columbia", PostalCode="", Number = "12345245", PayGroup = PaymentGroupDto.EFT.ToString() });
            sites.Add(new SiteDto { AddressLine1 = "33 B.M.A Avenue", AddressLine2 = "Tatalon", City = "Quezon City", PostalCode = "4102", Number = "12343465", PayGroup = PaymentGroupDto.Cheque.ToString() });
            return Task.FromResult(sites);
        }
    }
}
