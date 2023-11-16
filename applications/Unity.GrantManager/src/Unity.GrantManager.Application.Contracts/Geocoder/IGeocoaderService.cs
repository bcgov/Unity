using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Geocoder;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Geocoader
{
    public interface IGeocoaderService : IApplicationService
    {
        Task<dynamic?> GetAddressDetails(string value);
    }
}
