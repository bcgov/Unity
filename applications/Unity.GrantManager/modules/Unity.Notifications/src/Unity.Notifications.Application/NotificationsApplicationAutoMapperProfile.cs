using AutoMapper;
using Unity.Notifications.Emails;
using Volo.Abp.Users;

namespace Unity.Notifications;

public class NotificationsApplicationAutoMapperProfile : Profile
{
    public NotificationsApplicationAutoMapperProfile()
    {
        CreateMap<EmailLog, EmailHistoryDto>()
            .ForMember(x => x.SentBy, map => map.Ignore());
        CreateMap<IUserData, EmailHistoryUserDto>();
    }
}
