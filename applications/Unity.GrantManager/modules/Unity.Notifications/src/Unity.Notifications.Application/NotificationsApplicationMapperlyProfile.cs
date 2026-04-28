using Riok.Mapperly.Abstractions;
using Unity.Notifications.EmailGroups;
using Unity.Notifications.Emails;
using Volo.Abp.Mapperly;
using Volo.Abp.Users;

namespace Unity.Notifications;

[Mapper]
public partial class EmailLogToEmailHistoryDtoMapper : MapperBase<EmailLog, EmailHistoryDto>
{
    [MapperIgnoreTarget(nameof(EmailHistoryDto.SentBy))]
    public override partial EmailHistoryDto Map(EmailLog source);

    [MapperIgnoreTarget(nameof(EmailHistoryDto.SentBy))]
    public override partial void Map(EmailLog source, EmailHistoryDto destination);
}

[Mapper]
public partial class IUserDataToEmailHistoryUserDtoMapper : MapperBase<IUserData, EmailHistoryUserDto>
{
    public override partial EmailHistoryUserDto Map(IUserData source);

    public override partial void Map(IUserData source, EmailHistoryUserDto destination);
}

[Mapper]
public partial class EmailGroupToEmailGroupDtoMapper : MapperBase<EmailGroup, EmailGroupDto>
{
    public override partial EmailGroupDto Map(EmailGroup source);

    public override partial void Map(EmailGroup source, EmailGroupDto destination);
}

[Mapper]
public partial class EmailGroupUserToEmailGroupUsersDtoMapper : MapperBase<EmailGroupUser, EmailGroupUsersDto>
{
    public override partial EmailGroupUsersDto Map(EmailGroupUser source);

    public override partial void Map(EmailGroupUser source, EmailGroupUsersDto destination);
}
