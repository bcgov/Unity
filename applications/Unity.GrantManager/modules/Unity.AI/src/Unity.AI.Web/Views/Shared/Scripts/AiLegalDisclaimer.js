(function () {
    window.unity = window.unity || {};
    unity.aI = unity.aI || {};

    unity.aI.legalDisclaimer = {
        confirmIfNeeded: function (turningOn, onConfirmed) {
            if (!turningOn) {
                onConfirmed();
                return;
            }

            const modal = new abp.ModalManager({
                viewUrl: abp.appPath + 'Settings/LegalDisclaimerModal'
            });

            modal.onResult(function () {
                onConfirmed();
            });

            modal.open();
        }
    };
})();
