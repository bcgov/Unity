using System;
using System.Collections.Generic;
using System.Text;

namespace Unity.Notifications.Settings;
public class NotificationsSettingsDto
{
    public string DefaultFromAddress { get; set; } = string.Empty;
    public string DefaultFromDisplayName { get; set; } = string.Empty;
}
