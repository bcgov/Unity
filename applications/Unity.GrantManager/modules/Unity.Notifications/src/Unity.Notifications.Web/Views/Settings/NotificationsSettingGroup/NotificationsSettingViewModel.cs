using System.ComponentModel.DataAnnotations;

namespace Unity.Notifications.Web.Views.Settings.NotificationsSettingGroup;

public class NotificationsSettingViewModel
{
    [MaxLength(1024)]
    [Display(Name = "Setting:Notifications.Mailing.DefaultFromAddress.DisplayName")]
    [EmailAddress]
    public string DefaultFromAddress { get; set; } = string.Empty;
}
