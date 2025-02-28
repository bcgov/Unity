using System.ComponentModel.DataAnnotations;
using Unity.GrantManager.Attributes;

namespace Unity.Notifications.Web.Views.Settings.NotificationsSettingGroup;

public class NotificationsSettingViewModel
{
    [MaxLength(1024)]
    [Display(Name = "Setting:Notifications.Mailing.DefaultFromAddress.DisplayName")]
    [EmailAddress]
    public string DefaultFromAddress { get; set; } = string.Empty;

    [Display(Name = "Maximum Email Retry Attempts")]
    [MaxValue(10)]
    [MaxLength(2)]
    public int MaximumRetryAttempts { get; set; } = 3;
}
