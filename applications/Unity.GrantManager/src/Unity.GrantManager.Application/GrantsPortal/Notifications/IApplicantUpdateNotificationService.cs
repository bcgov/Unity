using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.GrantsPortal.Notifications;

/// <summary>
/// Schedules best-effort notifications after the portal unit of work commits.
/// Notification failures are logged without failing the portal operation.
/// </summary>
public interface IApplicantUpdateNotificationService
{
    Task QueueAsync(Applicant applicant, ApplicantUpdateDetails details);
    Task QueueAsync(Guid applicantId, ApplicantUpdateDetails details);
}
