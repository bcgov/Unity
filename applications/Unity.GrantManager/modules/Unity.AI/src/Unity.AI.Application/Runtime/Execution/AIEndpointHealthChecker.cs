using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Setup;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime.Execution;

/// <summary>
/// Calls the Azure OpenAI endpoint's /healthz from the server. UGM runs inside the cluster, so
/// this works for the internal LLM proxy service (http://unity-azure-llm-proxy/) that browsers
/// can't reach; the proxy answers with the Azure account it forwards to. No API key is sent.
/// </summary>
public class AIEndpointHealthChecker(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : ITransientDependency
{
    public const string HttpClientName = "AIEndpointHealth";
    private const int MaxMessageLength = 300;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public virtual async Task<AIEndpointStatusDto> CheckAsync(CancellationToken cancellationToken = default)
    {
        var endpoint = configuration[$"Azure:{AISetupConsts.OpenAIProvider}:Endpoint"]?.Trim();
        var status = new AIEndpointStatusDto { Endpoint = endpoint };
        if (string.IsNullOrWhiteSpace(endpoint) || !Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            status.Status = AIEndpointStatusDto.NotConfigured;
            return status;
        }

        var healthUri = new Uri(endpointUri, "/healthz");
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(Timeout);
            using var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(healthUri, timeout.Token);
            var body = (await response.Content.ReadAsStringAsync(timeout.Token)).Trim();

            status.HttpStatusCode = (int)response.StatusCode;
            status.Message = body.Length > MaxMessageLength ? body[..MaxMessageLength] : body;
            // Any answer means the endpoint is reachable; only a 2xx is a health report.
            status.Status = response.IsSuccessStatusCode ? AIEndpointStatusDto.Healthy : AIEndpointStatusDto.NoHealthCheck;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            status.Status = AIEndpointStatusDto.Unreachable;
            status.Message = ex is TaskCanceledException && !cancellationToken.IsCancellationRequested
                ? $"No answer within {Timeout.TotalSeconds:0} seconds."
                : ex.Message;
        }

        return status;
    }
}
