using System.ComponentModel.DataAnnotations;
using Unity.GrantManager.Attributes;

namespace Unity.Notifications.Web.Views.Settings.NotificationsSettingGroup;

public class NotificationsSettingViewModel
{
    [Display(Name = "Maximum Email Retry Attempts")]
    [MaxValue(10)]
    [MaxLength(2)]
    public int MaximumRetryAttempts { get; set; } = 3;

    [Display(Name = "Enable Schedule Email for Individual Application")]
    public bool EnableEmailDelay { get; set; }

    public string AllowedFileTypes { get; set; } = string.Empty;
    public string MaxFileSize { get; set; } = string.Empty;
    public string EmailAttachmentMaxFileSize { get; set; } = string.Empty;
    public string TotalEmailAttachmentMaxFileSize { get; set; } = string.Empty;      
}
