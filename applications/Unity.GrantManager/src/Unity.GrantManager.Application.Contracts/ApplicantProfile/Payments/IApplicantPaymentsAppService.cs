using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.ApplicantProfile;

public interface IApplicantPaymentsAppService : IApplicationService
{
    Task<ApplicantPaymentSummaryDto> GetPaymentSummaryByApplicantIdAsync(Guid applicantId);
    Task<List<ApplicantPaymentDetailsDto>> GetPaymentListByApplicantIdAsync(Guid applicantId);
}
