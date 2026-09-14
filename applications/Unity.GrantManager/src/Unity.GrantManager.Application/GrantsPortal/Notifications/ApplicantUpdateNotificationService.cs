using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Applications;
using Unity.Notifications.EmailAddresses;
using Unity.Notifications.EmailNotifications;
using Unity.Notifications.Emails;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal.Notifications;

public class ApplicantUpdateNotificationService(
    IApplicantRepository applicantRepository,
    IEmailAddressConfigurationsRepository addressRepository,
    IEmailLogsRepository emailLogsRepository,
    IEmailNotificationManager emailNotificationManager,
    IFeatureChecker featureChecker,
    ICurrentTenant currentTenant,
    IUnitOfWorkManager unitOfWorkManager,
    IGuidGenerator guidGenerator,
    IClock clock,
    IConfiguration configuration,
    ILogger<ApplicantUpdateNotificationService> logger) : IApplicantUpdateNotificationService, ITransientDependency
{
    public const string EmailTag = "GrantsPortal.ApplicantUpdate";

    public Task QueueAsync(Applicant applicant, ApplicantUpdateDetails details) =>
        ScheduleAsync(applicant.Id, applicant, details);

    public Task QueueAsync(Guid applicantId, ApplicantUpdateDetails details) =>
        ScheduleAsync(applicantId, null, details);

    private Task ScheduleAsync(Guid applicantId, Applicant? applicant, ApplicantUpdateDetails details)
    {
        try
        {
            if (details.Fields.Count == 0)
            {
                return Task.CompletedTask;
            }

            var tenantId = currentTenant.Id;
            var portalUow = unitOfWorkManager.Current;
            if (!tenantId.HasValue || portalUow == null)
            {
                logger.LogWarning("Skipping applicant update notification for {ApplicantId}: tenant or portal unit of work is missing.", applicantId);
                return Task.CompletedTask;
            }

            // Only copy in-memory values here. All notification I/O happens after the portal commit.
            var identity = applicant == null ? null : ApplicantIdentity.From(applicant);
            var snapshot = details with { Fields = details.Fields.ToArray() };
            var updatedAtUtc = clock.Now;
            portalUow.OnCompleted(() => NotifyAfterCommitAsync(
                tenantId.Value, applicantId, identity, snapshot, updatedAtUtc));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not schedule applicant update notification for {ApplicantId}. The portal update can continue.", applicantId);
        }
        return Task.CompletedTask;
    }

    private async Task NotifyAfterCommitAsync(Guid tenantId, Guid applicantId, ApplicantIdentity? identity,
        ApplicantUpdateDetails details, DateTime updatedAtUtc)
    {
        try
        {
            using (currentTenant.Change(tenantId))
            {
                EmailLog? emailLog;
                // A notification database error must never poison the portal update's transaction.
                using (var notificationUow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: true))
                {
                    emailLog = await CreateEmailLogAsync(tenantId, applicantId, identity, details, updatedAtUtc);
                    if (emailLog == null)
                    {
                        return;
                    }
                    await notificationUow.CompleteAsync();
                }

                // The email log must commit before the existing consumer can read it.
                using var queueUow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
                await emailNotificationManager.QueueEmailAsync(emailLog);
                await queueUow.CompleteAsync();
            }
        }
        catch (Exception ex)
        {
            // Best effort: no log means no retry; committed initialized logs remain recoverable.
            // Never propagate into the already committed portal command or its acknowledgment.
            logger.LogError(ex, "Could not create or queue applicant update notification for {ApplicantId} in tenant {TenantId}. The portal update remains saved.",
                applicantId, tenantId);
        }
    }

    private async Task<EmailLog?> CreateEmailLogAsync(Guid tenantId, Guid applicantId, ApplicantIdentity? identity,
        ApplicantUpdateDetails details, DateTime updatedAtUtc)
    {
        if (!await featureChecker.IsEnabledAsync("Unity.Notifications"))
        {
            return null;
        }

        var baseUrl = configuration["App:SelfUrl"]?.Trim();
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            logger.LogWarning("Skipping applicant update notification for {ApplicantId}: App:SelfUrl must be an absolute HTTP(S) base URL without a query or fragment.", applicantId);
            return null;
        }

        // The widget selects the active default across all configured address types.
        var defaults = await addressRepository.GetListAsync(address => address.IsActive && address.IsDefault);
        var recipient = defaults.FirstOrDefault()?.EmailAddress?.Trim();
        if (string.IsNullOrWhiteSpace(recipient))
        {
            logger.LogInformation("Skipping applicant update notification for {ApplicantId}: no active default email configured.", applicantId);
            return null;
        }

        identity ??= ApplicantIdentity.From(await applicantRepository.GetAsync(applicantId));
        var emailLog = new EmailLog
        {
            Id = guidGenerator.Create(),
            TenantId = tenantId,
            ApplicantId = applicantId,
            FromAddress = ResolveFromAddress(recipient),
            ToAddress = recipient,
            Subject = $"{identity.DisplayId} - {identity.Name} - Update",
            Body = ApplicantUpdateEmailRenderer.Render(uri.AbsoluteUri, tenantId, applicantId,
                identity.DisplayId, identity.Name, updatedAtUtc, details),
            BodyType = "html",
            Priority = "normal",
            Tag = EmailTag,
            Status = EmailStatus.Initialized,
            EmailType = EmailType.EventBased
        };

        await emailLogsRepository.InsertAsync(emailLog);
        return emailLog;
    }

    private sealed record ApplicantIdentity(string DisplayId, string Name)
    {
        public static ApplicantIdentity From(Applicant applicant) => new(
            string.IsNullOrWhiteSpace(applicant.UnityApplicantId) ? applicant.Id.ToString() : applicant.UnityApplicantId,
            !string.IsNullOrWhiteSpace(applicant.ApplicantName) ? applicant.ApplicantName
                : !string.IsNullOrWhiteSpace(applicant.OrgName) ? applicant.OrgName : "Unnamed applicant");
    }

    public static string ResolveFromAddress(string? defaultAddress) =>
        string.IsNullOrWhiteSpace(defaultAddress) ? "NoReply@gov.bc.ca" : defaultAddress.Trim();
}
