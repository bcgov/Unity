using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Notifications;

public class NotificationListInputDto : PagedAndSortedResultRequestDto
{
    // BC/Vancouver-local calendar date (date-only). Inclusive start of that day.
    public DateTime? DateFrom { get; set; }

    // BC/Vancouver-local calendar date (date-only). Inclusive end of that day.
    public DateTime? DateTo { get; set; }

    // Filters on the server so rows the user excluded are never loaded or sent to the browser.
    public NotificationListView View { get; set; } = NotificationListView.Both;
}
