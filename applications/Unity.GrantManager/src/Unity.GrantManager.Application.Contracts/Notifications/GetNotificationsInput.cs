using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Notifications
{
    public class GetNotificationsInput : PagedAndSortedResultRequestDto
    {
        public Guid? FormId { get; set; }
        public string? Filter { get; set; }
    }
}
