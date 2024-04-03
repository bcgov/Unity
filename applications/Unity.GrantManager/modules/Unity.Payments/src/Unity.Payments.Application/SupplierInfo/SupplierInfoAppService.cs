using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            sites.Add(new SiteDto { MailingAddress = "777 Hockley Avenue", Number = "12345245" });
            sites.Add(new SiteDto { MailingAddress = "33 B.M.A Avenue", Number = "12343465" });
            return Task.FromResult(sites);
        }
    }
}
