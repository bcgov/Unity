abp.modals.UpsertCustomFieldModal = function () {

    const classificationHints = {
        'ProtectedA': 'If compromised, could cause limited or moderate injury to an individual or organisation — e.g. an exact salary figure or home address.',
        'ProtectedB': 'Could cause serious injury if disclosed — e.g. Social Insurance Numbers, employment equity data, or personal health records.',
        'ProtectedC': 'The most sensitive level — disclosure could cause extremely grave injury.'
    };

    const placeholderSupportedTypes = ['Text', 'TextArea', 'Numeric', 'Currency', 'Email', 'Phone'];

    function updateClassificationHint(value) {
        const hint = document.getElementById('classificationHint');
        if (hint) hint.textContent = classificationHints[value] || '';
    }

    function updatePlaceholderVisibility(type) {
        const row = document.getElementById('placeholder-row');
        if (row) row.style.display = placeholderSupportedTypes.includes(type) ? '' : 'none';
    }

    function initModal(modalManager, args) {
        const initClassification = document.getElementById('InitSecurityClassification')?.value ?? '';
        const initFieldType = document.getElementById('InitFieldType')?.value ?? 'Text';

        updateClassificationHint(initClassification);
        updatePlaceholderVisibility(initFieldType);

        document.getElementById('fieldType')?.addEventListener('change', function () {
            updatePlaceholderVisibility(this.value);
            const customFieldWidget = new abp.WidgetManager({
                wrapper: '#definition-editor',
                filterCallback: function () {
                    return { 'type': $('#fieldType').val() };
                }
            });
            customFieldWidget.refresh();
        });

        document.getElementById('SecurityClassification')?.addEventListener('change', function () {
            updateClassificationHint(this.value);
        });

        document.querySelector('[name="deleteCustomFieldBtn"]')?.addEventListener('click', function () {
            Swal.fire({
                title: "Delete Custom Field?",
                text: 'Are you sure you want to delete this custom field?',
                showCancelButton: true,
                confirmButtonText: 'Confirm',
                customClass: {
                    confirmButton: 'btn btn-primary',
                    cancelButton: 'btn btn-secondary'
                }
            }).then((result) => {
                if (result.isConfirmed) {
                    $('#DeleteAction').val(true);
                    $('#customFieldInfo').submit();
                }
            });
        });

        document.querySelector('[name="saveCustomFieldBtn"]')?.addEventListener('click', function () {
            setTimeout(function () {
                var panes = ['pane-display', 'pane-attributes'];
                var tabButtons = { 'pane-display': 'tab-display', 'pane-attributes': 'tab-attributes' };
                var activePane = document.querySelector('#customFieldTabContent .tab-pane.show');
                var activePaneId = activePane ? activePane.id : null;
                var hasOffTabErrors = false;

                panes.forEach(function (paneId) {
                    if (paneId === activePaneId) return;
                    var pane = document.getElementById(paneId);
                    var invalid = pane ? pane.querySelector(':invalid') : null;
                    var btn = document.getElementById(tabButtons[paneId]);
                    if (!btn) return;
                    var badge = btn.querySelector('.tab-error-badge');
                    if (invalid) {
                        if (!badge) {
                            badge = document.createElement('span');
                            badge.className = 'tab-error-badge';
                            badge.setAttribute('aria-label', 'This tab has errors');
                            btn.appendChild(badge);
                        }
                        hasOffTabErrors = true;
                    } else {
                        if (badge) badge.remove();
                    }
                });

                if (hasOffTabErrors) {
                    abp.notify.warn('There are validation errors on another tab. Please review all tabs before saving.');
                }
            }, 0);
        });

        document.querySelectorAll('#customFieldTabs [data-bs-toggle="tab"]').forEach(function (btn) {
            btn.addEventListener('shown.bs.tab', function () {
                var badge = btn.querySelector('.tab-error-badge');
                if (badge) badge.remove();
            });
        });
    }

    return { initModal: initModal };
};
