﻿using System;
using System.Threading.Tasks;
using Unity.Modules.Shared;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications
{
    public interface IApplicationApplicantAppService : IApplicationService
    {
        Task<ApplicationApplicantInfoDto> GetByApplicationIdAsync(Guid applicationId);
        Task<ApplicantInfoDto> GetApplicantInfoTabAsync(Guid applicationId);
        Task<GrantApplicationDto> UpdatePartialApplicantInfoAsync(Guid applicationId, PartialUpdateDto<UpdateApplicantInfoDto> input);
        Task<bool> GetSupplierNameMatchesCheck(Guid applicantId, string? supplierName);
    }
}
