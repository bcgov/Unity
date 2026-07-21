using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Notifications;
using Volo.Abp.AspNetCore.Mvc;
using Unity.GrantManager.Notifications.Logs;

namespace Unity.GrantManager.Web.Controllers.Monitoring;

[ApiController]
[Route("api/monitoring")]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class AlertWebhookController(
    INotificationsAppService notificationsAppService,
    ILogger<AlertWebhookController> logger) : AbpController
{
    /// <summary>
    /// Receives Alertmanager webhook payloads and forwards a concise summary to Teams.
    /// </summary>
    [HttpPost("alert")]
    public async Task<IActionResult> ProcessAlert([FromBody] AlertManagerPayload? payload)
    {
        if (payload is null || !ModelState.IsValid || payload.Alerts.Count == 0)
        {
            return BadRequest();
        }

        try
        {
            var firing = payload.Alerts
                .Where(a => a is not null && string.Equals(a.Status, "firing", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (firing.Count == 0)
            {
                return Ok();
            }

            // Pick the most severe alert as the headline (critical > error > warning > info > unknown)
            var lead = firing
                .OrderBy(a => SeverityOrder(a.Labels.GetValueOrDefault("severity", "unknown")))
                .First();
            string alertName = lead.Labels.GetValueOrDefault("alertname", "Unknown Alert");
            string severity = lead.Labels.GetValueOrDefault("severity", "unknown");
            string summary = lead.Annotations.GetValueOrDefault("summary", alertName);
            string description = lead.Annotations.GetValueOrDefault("description", string.Empty);
            string @namespace = lead.Labels.GetValueOrDefault("kubernetes_namespace_name",
                                lead.Labels.GetValueOrDefault("namespace", string.Empty));
            string endpoint = lead.Labels.GetValueOrDefault("handler",
                              lead.Labels.GetValueOrDefault("endpoint", string.Empty));
            string? envInfo = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            string activityTitle = $"[{severity.ToUpperInvariant()}] {summary}";
            string activitySubtitle = $"Environment: {envInfo} | Namespace: {@namespace}";

            var facts = new List<Fact>();

            if (!string.IsNullOrEmpty(description))
            {
                facts.Add(new Fact { Name = "Description", Value = description });
            }

            if (firing.Count > 1)
            {
                facts.Add(new Fact { Name = "Firing alerts", Value = firing.Count.ToString() });
            }

            if (!string.IsNullOrEmpty(endpoint))
            {
                facts.Add(new Fact { Name = "Affected endpoint", Value = endpoint });
            }

            facts.Add(new Fact { Name = "First seen", Value = lead.StartsAt.ToString("u") });

            if (!string.IsNullOrEmpty(lead.GeneratorURL))
            {
                facts.Add(new Fact { Name = "Source", Value = lead.GeneratorURL });
            }

            await notificationsAppService.PostToNotificationsAsync(activityTitle, activitySubtitle, facts);

            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to forward alert {AlertName} to Teams", 
                payload.Alerts.FirstOrDefault()?.Labels?.GetValueOrDefault("alertname"));
            return StatusCode(500);
        }
    }

    private static int SeverityOrder(string? severity) => severity?.ToLowerInvariant() switch
    {
        "critical" => 0,
        "error"    => 1,
        "warning"  => 2,
        "info"     => 3,
        _          => 4
    };
}
