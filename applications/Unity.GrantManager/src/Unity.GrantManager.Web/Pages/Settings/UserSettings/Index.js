$(function () {
    const UIElements = {
        alerts: $('#AbpPageAlerts'),
    };

    init();

    function init() {
        processAbpNotifications();
    }

    function processAbpNotifications() {
        // Find all alert elements within the container
        UIElements.alerts.find('.alert').each(function () {
            const $alert = $(this);
            
            // Extract text, excluding the close button
            const $clone = $alert.clone();
            $clone.find('.btn-close, .close, button').remove();
            const message = $clone.text().trim();

            // Trigger corresponding abp notification
            if ($alert.hasClass('alert-success')) {
                abp.notify.success(message);
            } else if ($alert.hasClass('alert-danger') || $alert.hasClass('alert-error')) {
                abp.notify.error(message);
            } else if ($alert.hasClass('alert-warning')) {
                abp.notify.warn(message);
            } else {
                abp.notify.info(message);
            }
        });
    }    
});