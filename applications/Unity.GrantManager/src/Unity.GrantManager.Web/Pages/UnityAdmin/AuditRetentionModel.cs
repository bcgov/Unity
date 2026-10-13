using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Unity.GrantManager.Settings;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;

namespace Unity.GrantManager.Web.Pages.UnityAdmin;

public partial class IndexModel
{
    [BindProperty]
    public bool RetentionCleanupEnabled { get; set; } = true;

    [BindProperty]
    public bool AuditLogRetentionIndefinite { get; set; }

    [BindProperty]
    [Range(1, 36500)]
    public int AuditLogRetentionDays { get; set; } = SettingsConstants.DefaultAuditLogRetentionDays;

    [BindProperty]
    public bool ExceptionLogRetentionIndefinite { get; set; }

    [BindProperty]
    [Range(1, 36500)]
    public int ExceptionLogRetentionDays { get; set; } = SettingsConstants.DefaultExceptionLogRetentionDays;

    public DateTime? AuditLogCutoffDate => AuditLogRetentionIndefinite
        ? null
        : DateTime.UtcNow.Date.AddDays(-AuditLogRetentionDays);
    public DateTime? ExceptionLogCutoffDate => ExceptionLogRetentionIndefinite
        ? null
        : DateTime.UtcNow.Date.AddDays(-ExceptionLogRetentionDays);

    private async Task LoadRetentionSettingsAsync(ISettingProvider settingProvider)
    {
        RetentionCleanupEnabled = await settingProvider.GetAsync(
            SettingsConstants.Retention.CleanupEnabled,
            defaultValue: true);
        var auditLogRetentionDays = await settingProvider.GetAsync(
            SettingsConstants.Retention.AuditLogRetentionDays,
            defaultValue: SettingsConstants.DefaultAuditLogRetentionDays);
        AuditLogRetentionIndefinite = auditLogRetentionDays <= SettingsConstants.IndefiniteRetentionDays;
        AuditLogRetentionDays = AuditLogRetentionIndefinite
            ? SettingsConstants.DefaultAuditLogRetentionDays
            : auditLogRetentionDays;

        var exceptionLogRetentionDays = await settingProvider.GetAsync(
            SettingsConstants.Retention.ExceptionLogRetentionDays,
            defaultValue: SettingsConstants.DefaultExceptionLogRetentionDays);
        ExceptionLogRetentionIndefinite = exceptionLogRetentionDays <= SettingsConstants.IndefiniteRetentionDays;
        ExceptionLogRetentionDays = ExceptionLogRetentionIndefinite
            ? SettingsConstants.DefaultExceptionLogRetentionDays
            : exceptionLogRetentionDays;
    }

    public async Task<IActionResult> OnPostRetentionAsync(
        [FromServices] ISettingManager settingManager,
        [FromServices] ISettingProvider settingProvider)
    {
        if (AuditLogRetentionIndefinite)
        {
            ModelState.Remove(nameof(AuditLogRetentionDays));
        }

        if (ExceptionLogRetentionIndefinite)
        {
            ModelState.Remove(nameof(ExceptionLogRetentionDays));
        }

        if (!ModelState.IsValid)
        {
            await LoadAdminPageStateAsync();
            return Page();
        }

        await settingManager.SetGlobalAsync(
            SettingsConstants.Retention.CleanupEnabled,
            RetentionCleanupEnabled.ToString());
        await settingManager.SetGlobalAsync(
            SettingsConstants.Retention.AuditLogRetentionDays,
            (AuditLogRetentionIndefinite
                ? SettingsConstants.IndefiniteRetentionDays
                : AuditLogRetentionDays).ToString());
        await settingManager.SetGlobalAsync(
            SettingsConstants.Retention.ExceptionLogRetentionDays,
            (ExceptionLogRetentionIndefinite
                ? SettingsConstants.IndefiniteRetentionDays
                : ExceptionLogRetentionDays).ToString());

        return RedirectToPage();
    }
}
