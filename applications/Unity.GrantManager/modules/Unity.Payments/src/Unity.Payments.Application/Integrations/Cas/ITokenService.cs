﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Payments.Integrations.Cas
{
    public interface ITokenService : IApplicationService
    {
        Task<Dictionary<string, string>> GetAuthHeadersAsync();
    }
}