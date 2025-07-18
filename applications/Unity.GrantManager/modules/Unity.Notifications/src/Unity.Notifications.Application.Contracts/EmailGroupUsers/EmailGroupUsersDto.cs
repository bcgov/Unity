using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Notifications.EmailGroups
{
    public class EmailGroupUsersDto :EntityDto<Guid>
    {
        
        public Guid GroupId { get; set; }
        public Guid UserId { get; set; }

    }
}
