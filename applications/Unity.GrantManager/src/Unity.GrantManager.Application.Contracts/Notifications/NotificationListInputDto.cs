using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Notifications;

public class NotificationListInputDto : PagedAndSortedResultRequestDto
{
    // BC/Vancouver-local calendar date (date-only). Inclusive start of that day.
    public DateTime? DateFrom { get; set; }

    // BC/Vancouver-local calendar date (date-only). Inclusive end of that day.
    public DateTime? DateTo { get; set; }
}
