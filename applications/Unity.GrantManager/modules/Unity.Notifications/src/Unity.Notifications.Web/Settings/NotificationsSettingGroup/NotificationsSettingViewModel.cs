using System.ComponentModel.DataAnnotations;

namespace Unity.Notifications.Web.Settings.NotificationsSettingGroup;

public class NotificationsSettingViewModel
{
    [MaxLength(1024)]
    [Display(Name = "Setting:Notifications.Mailing.DefaultFromAddress.DisplayName")]
    public string DefaultFromAddress { get; set; } = string.Empty;

    [MaxLength(1024)]
    [Display(Name = "Setting:Notifications.Mailing.DefaultFromDisplayName.DisplayName")]
    public string DefaultFromDisplayName { get; set; } = string.Empty;
}
