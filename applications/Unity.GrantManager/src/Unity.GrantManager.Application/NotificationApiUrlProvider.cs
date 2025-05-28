using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager;


public class NotificationApiUrlProvider : INotificationApiUrlProvider
{
    private readonly IDynamicUrlRepository _repository;
    private readonly ILogger<NotificationApiUrlProvider> _logger;
    private string? _cachedUrl;

    public NotificationApiUrlProvider(IDynamicUrlRepository repository, ILogger<NotificationApiUrlProvider> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<string> GetBaseUrlAsync()
    {
        if (_cachedUrl != null)
            return _cachedUrl;

        try
        {
            var record = await _repository.FirstOrDefaultAsync(x => x.KeyName == "NOTIFICATION_API_BASE");
            _cachedUrl = record?.Url ?? "https://submit.digital.gov.bc.ca/app/api/v1";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch NOTIFICATION_API_BASE from database.");
            _cachedUrl = "https://submit.digital.gov.bc.ca/app/api/v1";
        }

        return _cachedUrl;
    }
}
