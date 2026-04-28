$(function () {
    const toggle = document.getElementById('analytics-enabled-toggle');
    const statusLabel = document.getElementById('analytics-status-label');

    if (!toggle) return;

    toggle.addEventListener('change', function () {
        const enabled = toggle.checked;

        abp.ajax({
            url: '/ConfigurationManagement?handler=ToggleAnalytics',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ enabled: enabled }),
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                    || abp.security.antiForgery.getToken()
            }
        }).done(function () {
            statusLabel.textContent = enabled ? 'Enabled' : 'Disabled';
            abp.notify.success(
                enabled ? 'Analytics tracking enabled.' : 'Analytics tracking disabled.',
                'Analytics'
            );
        }).fail(function () {
            // Revert toggle on failure
            toggle.checked = !enabled;
            statusLabel.textContent = !enabled ? 'Enabled' : 'Disabled';
            abp.notify.error('Failed to update analytics setting.', 'Analytics');
        });
    });
});
