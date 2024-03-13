using Microsoft.Extensions.Diagnostics.HealthChecks;
using RestSharp;
using RestSharp.Authenticators;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Attachments;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Encryption;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.HealthChecks;

public class UnityHealthChecks(ITenantRepository tenantRepository,
    ICurrentTenant currentTenant,
    IApplicationFormRepository applicationFormRepository,
    RestClient intakeClient,
    IStringEncryptionService stringEncryptionService,
    IApplicationAttachmentRepository applicationAttachmentRepository,
    IFileAppService fileAppService) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return await CheckConnectivities();
    }

    private async Task<HealthCheckResult> CheckConnectivities()
    {
        var tenants = await tenantRepository.GetListAsync();

        foreach (var tenant in tenants)
        {
            using (currentTenant.Change(tenant.Id))
            {
                ApplicationForm? appForm = await applicationFormRepository.FirstOrDefaultAsync();
                if(!await CheckChefsConnectivityAsync(appForm))
                {
                    return HealthCheckResult.Unhealthy("Problem with Chefs connectivity.");
                }
                ApplicationAttachment? applicationAttachment = await applicationAttachmentRepository.FirstOrDefaultAsync();
                if(!await CheckS3ConnectivityAsync(applicationAttachment))
                {
                    return HealthCheckResult.Unhealthy("Problem with S3 connectivity.");
                }
            }
        }

        return HealthCheckResult.Healthy("Unity Server is healthy.");
    }

    private async Task<bool> CheckS3ConnectivityAsync(ApplicationAttachment? applicationAttachment)
    {
        var fileDto = await fileAppService.GetBlobAsync(new GetBlobRequestDto { S3ObjectKey = applicationAttachment?.S3ObjectKey, Name = applicationAttachment?.FileName });
        if(fileDto == null)
        {
            return false;
        }
        return true;
    }

    private async Task<bool> CheckChefsConnectivityAsync(ApplicationForm? applicationForm)
    {
        string requestUrl = $"/forms/{applicationForm?.ChefsApplicationFormGuid}/submissions";
        
        var restRequest = new RestRequest(requestUrl, Method.Get)
        {
            Authenticator = new HttpBasicAuthenticator(applicationForm?.ChefsApplicationFormGuid!, stringEncryptionService.Decrypt(applicationForm.ApiKey!) ?? string.Empty)
        };

        restRequest.AddParameter("deleted", "false");
        restRequest.AddParameter("filterformSubmissionStatusCode", "true");
        
        var response = await intakeClient.GetAsync(restRequest);
        string errorMessageBase = "Error calling ListFormSubmissions: ";
        string errorMessage = (int)response.StatusCode switch
        {
            >= 400 => errorMessageBase + response.Content,
            0 => errorMessageBase + response.ErrorMessage,
            _ => ""
        };
        if (!string.IsNullOrEmpty(errorMessage))
        {
            return false;
        }

        return true;
    }
}
