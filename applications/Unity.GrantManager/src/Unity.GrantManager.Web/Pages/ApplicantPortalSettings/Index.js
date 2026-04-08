(function ($) {

    const l = abp.localization.getResource('GrantManager');

    const $form = $('#PortalStatusForm');

    let portalStatusTable = new DataTable("#PortalStatusTable", {
        paging: false,
        sort: false,
        info: false
    });

    $form.on('submit', function (e) {
        e.preventDefault();

        const statuses = [];
        $form.find('tbody tr').each(function () {
            const $row = $(this);
            const id = $row.find('input[type="hidden"]').val();
            const externalStatus = $row.find('input[type="text"]').val().trim();

            if (!externalStatus) {
                abp.notify.warn(l('ApplicantPortalSettings:ValidationRequired'));
                return false;
            }

            statuses.push({
                id: id,
                externalStatus: externalStatus
            });
        });

        if (statuses.length === 0) {
            return;
        }

        abp.ui.setBusy($form);

        unity.grantManager.grantApplications.applicationStatus
            .updateExternalStatusLabels({ statuses: statuses })
            .then(function () {
                abp.notify.success(l('ApplicantPortalSettings:SaveSuccess'));
            })
            .catch(function (error) {
                abp.notify.error(error.message || l('ApplicantPortalSettings:SaveError'));
            })
            .always(function () {
                abp.ui.clearBusy($form);
            });
    });

})(jQuery);
