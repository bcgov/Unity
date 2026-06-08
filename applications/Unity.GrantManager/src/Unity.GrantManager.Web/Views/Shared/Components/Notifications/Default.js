(function () {
    let formId;
    let notificationsTable;

    function fetchTemplates() {
        return fetch('/api/form-notifications/templates').then(r => r.json());
    }

    function fetchStatuses() {
        return fetch('/api/form-notifications/statuses').then(r => r.json());
    }

    function fetchRecipients(category) {
        return fetch('/api/form-notifications/recipients?category=' + encodeURIComponent(category)).then(r => r.json());
    }

    function renderTriggerDetail(data, type, row) {
        if (row.triggerType === 'Date') {
            return row.dateType ? row.dateType : '';
        }
        return row.eventStatus ? row.eventStatus : '';
    }

    function getNotificationColumns() {
        let index = 0;
        return [
            { title: 'Template',      name: 'templateName', data: 'templateName', visible: true, index: index++ },
            { title: 'Trigger Type',  name: 'triggerType',  data: 'triggerType',  visible: true, index: index++ },
            { title: 'Trigger Detail',name: 'triggerDetail',data: null, visible: true, orderable: true, defaultContent: '', index: index++,
              render: renderTriggerDetail },
            { title: 'Status', name: 'status', data: 'isActive', visible: true, orderable: true, index: index++,
              render: function (data) {
                  return data
                      ? '<span class="badge bg-success">Active</span>'
                      : '<span class="badge bg-dark">Cancelled</span>';
              }
            },
            { title: 'Actions', name: 'rowActions', data: null, visible: true, orderable: false,
              className: 'text-center notexport', defaultContent: '', index: index++,
              render: function (data, type, row) { return generateNotificationsButtonContent(row); }
            }
        ];
    }

    function initNotificationsTable() {
        // Use $.ajax so abp.libs.datatables.createAjax gets a jQuery-compatible deferred (has .always())
        function notificationsEndpoint() {
            return $.ajax({
                url: '/api/form-notifications/' + encodeURIComponent(formId),
                type: 'GET',
                dataType: 'json'
            });
        }



        notificationsTable = initializeDataTable({
            dt: $('#NotificationsTable'),
            listColumns: getNotificationColumns(),
            defaultVisibleColumns: ['templateName', 'triggerType', 'triggerDetail', 'status', 'rowActions'],
            defaultSortColumn: { name: 'templateName', dir: 'asc' },
            dataEndpoint: notificationsEndpoint,
            data: {},
            responseCallback,
            actionButtons: [],
            pagingEnabled: true,
            reorderEnabled: false,
            languageSetValues: {},
            dynamicButtonContainerId: null,
            useNullPlaceholder: false,
            disableColumnSelect: true,
            externalFilterButtonId: null,
            lengthMenu: [10, 25, 50]
        });

        // jQuery event delegation — survives DataTables redraws
        $('#notifications-list').on('click', '.js-edit-notification', function () {
            onEditNotification($(this).data('id'));
        });
        $('#notifications-list').on('click', '.js-cancel-notification', function () {
            onCancelNotification($(this).data('id'));
        });
        $('#notifications-list').on('click', '.js-delete-notification', function () {
            onDeleteNotification($(this).data('id'));
        });
    }

    function reloadTable() {
        if (notificationsTable) {
            notificationsTable.ajax.reload();
        }
    }

    function onCancelNotification(id) {
        if (!id) return;
        Swal.fire({
            title: 'Cancel Notification Plan?',
            html: 'Are you sure you want to cancel this notification plan?<br><br>All future scheduled notifications and event-based triggers will be stopped. This action cannot be undone. Notifications that have already been sent are not impacted.',
            showCancelButton: true,
            confirmButtonText: 'Confirm',
            cancelButtonText: 'Cancel',
            customClass: {
                confirmButton: 'btn btn-primary',
                cancelButton: 'btn btn-secondary'
            }
        }).then((result) => {
            if (!result.isConfirmed) return;
            fetch('/api/form-notifications/' + encodeURIComponent(formId) + '/' + encodeURIComponent(id) + '/cancel', {
                    method: 'PATCH',
                    headers: { 'RequestVerificationToken': abp.security.antiForgery.getToken() }
                })
                .then(r => {
                    if (!r.ok) throw new Error('Failed to cancel');
                    abp.notify.success('Notification plan cancelled');
                    reloadTable();
                })
                .catch(err => {
                    console.error(err);
                    abp.notify.error('Failed to cancel notification plan');
                });
        });
    }

    function onDeleteNotification(id) {
        if (!id) return;
        Swal.fire({
            title: 'Delete Notification?',
            text: 'Are you sure you want to delete this scheduled notification?',
            showCancelButton: true,
            confirmButtonText: 'Confirm',
            customClass: {
                confirmButton: 'btn btn-primary',
                cancelButton: 'btn btn-secondary'
            }
        }).then((result) => {
            if (!result.isConfirmed) return;
            fetch('/api/form-notifications/' + encodeURIComponent(formId) + '/' + encodeURIComponent(id), {
                    method: 'DELETE',
                    headers: { 'RequestVerificationToken': abp.security.antiForgery.getToken() }
                })
                .then(r => {
                    if (!r.ok) throw new Error('Failed to delete');
                    abp.notify.success('Notification deleted');
                    reloadTable();
                })
                .catch(err => {
                    console.error(err);
                    abp.notify.error('Failed to delete notification');
                });
        });
    }

    function onEditNotification(id) {
        if (!id) return;
        fetch('/api/form-notifications/' + encodeURIComponent(formId))
            .then(r => r.json())
            .then(list => {
                const row = list.find(n => String(n.id) === String(id));
                if (!row) { abp.notify.error('Notification not found'); return; }
                populateModalForEdit(row);
            })
            .catch(err => {
                console.error(err);
                abp.notify.error('Failed to load notification for editing');
            });
    }

    async function populateModalForEdit(row) {
        const modalEl = document.getElementById('notificationModal');
        if (!modalEl) return;

        resetValidationState();

        if (row.triggerType === 'Event' && row.recipientCategory) {
            try {
                const list = await fetchRecipients(row.recipientCategory);
                populateRecipients(list);
            } catch (err) {
                console.error('Failed to pre-load recipients for edit modal', err);
            }
        }

        modalEl.dataset.editId = row.id;
        modalEl.addEventListener('shown.bs.modal', function () {
            const setVal = (id, val) => {
                const el = document.getElementById(id);
                if (el) el.value = val ?? '';
            };

            setVal('templateSelect', row.templateId);
            updatePreview();

            setVal('triggerType', row.triggerType);
            document.getElementById('dateOptions').style.display = row.triggerType === 'Date' ? 'block' : 'none';
            document.getElementById('eventOptions').style.display = row.triggerType === 'Event' ? 'block' : 'none';

            if (row.triggerType === 'Date') {
                setVal('dateType', row.dateType);
            } else if (row.triggerType === 'Event') {
                setVal('statusSelect', row.applicationStatusId);
                setVal('recipientCategory', row.recipientCategory);
                setVal('recipientSelect', row.recipientIdentifier);
            }
        }, { once: true });

        showModal();
    }

    function populateTemplates(templates) {
        const sel = document.getElementById('templateSelect');
        sel.innerHTML = '';
        const blank = document.createElement('option');
        blank.value = '';
        blank.text = '';
        sel.appendChild(blank);
        templates.forEach(t => {
            const opt = document.createElement('option');
            // Use template id as the option value so we can reference templates reliably
            opt.value = t.id;
            opt.text = t.name + ' — ' + t.subject;
            sel.appendChild(opt);
        });
        updatePreview();
    }

    function populateStatuses(statuses) {
        const sel = document.getElementById('statusSelect');
        if (!sel) return;
        sel.innerHTML = '';
        const blank = document.createElement('option');
        blank.value = '';
        blank.text = '';
        sel.appendChild(blank);
        statuses.forEach(s => {
            const opt = document.createElement('option');
            opt.value = s.id;
            opt.text = s.internalStatus;
            sel.appendChild(opt);
        });
    }

    function populateRecipients(list) {
        const sel = document.getElementById('recipientSelect');
        if (!sel) return;
        sel.innerHTML = '';
        // Add a blank default option first (required by spec)
        const empty = document.createElement('option');
        empty.value = '';
        empty.text = '';
        sel.appendChild(empty);
        list.forEach(r => {
            const opt = document.createElement('option');
            opt.value = r.id;
            opt.text = r.displayName;
            sel.appendChild(opt);
        });
    }

    function updatePreview() {
        const sel = document.getElementById('templateSelect');
        const preview = document.getElementById('templatePreview');
        if (!sel || !preview) return;
        const val = sel.value;
        fetch('/api/form-notifications/templates').then(r => r.json()).then(list => {
            const t = list.find(x => String(x.id) === String(val));
            preview.innerText = t ? `${t.subject}\n\n${t.body}` : '';
        });
    }

    function showModal() {
        resetValidationState();

        ['templateSelect', 'triggerType', 'dateType', 'statusSelect', 'recipientCategory', 'recipientSelect'].forEach(id => {
            const el = document.getElementById(id);
            if (el) el.value = '';
        });

        document.getElementById('dateOptions').style.display = 'none';
        document.getElementById('eventOptions').style.display = 'none';
        document.getElementById('templatePreview').innerText = '';

        const modalEl = document.getElementById('notificationModal');
        if (!modalEl) return;
        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    }

    function validateForm(triggerType) {
        const form = document.getElementById('notificationForm');
        if (!form) return false;

        let valid = true;

        const requiredAlways = ['templateSelect', 'triggerType'];
        const requiredForDate = ['dateType'];
        const requiredForEvent = ['statusSelect', 'recipientCategory', 'recipientSelect'];

        const fieldsToValidate = [
            ...requiredAlways,
            ...(triggerType === 'Date' ? requiredForDate : []),
            ...(triggerType === 'Event' ? requiredForEvent : [])
        ];

        // Clear all first
        form.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));

        fieldsToValidate.forEach(id => {
            const el = document.getElementById(id);
            if (el && !el.value) {
                el.classList.add('is-invalid');
                valid = false;
            }
        });

        return valid;
    }

    function generateNotificationsButtonContent(row) {
        const template = document.getElementById('notification-actions-template');
        if (!template) return '';

        const container = document.createElement('div');
        container.appendChild(template.content.cloneNode(true));

        const editBtn = container.querySelector('.js-edit-notification');
        if (editBtn) {
            if (row.isActive) {
                editBtn.dataset.id = row.id;
            } else {
                editBtn.remove();
            }
        }

        const cancelBtn = container.querySelector('.js-cancel-notification');
        if (cancelBtn) {
            if (row.isActive) {
                cancelBtn.dataset.id = row.id;
            } else {
                cancelBtn.remove();
            }            
        }

        const deleteBtn = container.querySelector('.js-delete-notification');
        if (deleteBtn) {
            deleteBtn.dataset.id = row.id;
        }

        return container.innerHTML;
    }

    function init() {
        formId = document.getElementById('applicationFormId')?.value;
        if (!formId) return;

        configureSubmitOnlyValidation();

        const modalEl = document.getElementById('notificationModal');
        if (modalEl) {
            // Always reset validation when modal is fully closed
            modalEl.addEventListener('hidden.bs.modal', () => resetValidationState());
            // Also reset when modal starts opening
            modalEl.addEventListener('show.bs.modal', () => resetValidationState());
        }

        fetchTemplates().then(populateTemplates);
        initNotificationsTable();

        document.getElementById('btn-add-notification')?.addEventListener('click', () => {
            document.getElementById('notificationModal').dataset.editId = '';
            showModal();
        });

        document.getElementById('templateSelect')?.addEventListener('change', (e) => {
            e.target.classList.remove('is-invalid');
            updatePreview();
        });
        ['dateType', 'statusSelect', 'recipientSelect'].forEach(id => {
            document.getElementById(id)?.addEventListener('change', (e) => {
                e.target.classList.remove('is-invalid');
            });
        });
        document.getElementById('triggerType')?.addEventListener('change', (e) => {
            const val = e.target.value;
            document.getElementById('dateOptions').style.display = val === 'Date' ? 'block' : 'none';
            document.getElementById('eventOptions').style.display = val === 'Event' ? 'block' : 'none';
            e.target.classList.remove('is-invalid');
        });

        document.getElementById('recipientCategory')?.addEventListener('change', (e) => {
            const cat = e.target.value;
            e.target.classList.remove('is-invalid');
            if (cat) fetchRecipients(cat).then(populateRecipients);
        });

        document.getElementById('btn-save-notification')?.addEventListener('click', () => {
            const triggerType = document.getElementById('triggerType').value;

            if (!formId) {
                abp.notify.error('Form identifier not found on page. Cannot create notification.');
                console.error('Missing formId for notification POST', { formId });
                return;
            }

            if (!validateForm(triggerType)) return;

            const templateId = (document.getElementById('templateSelect').value || '').trim();
            const dateType = document.getElementById('dateType').value;
            const applicationStatusId = document.getElementById('statusSelect')?.value;
            const recipientCategory = document.getElementById('recipientCategory')?.value;
            const recipientIdentifier = document.getElementById('recipientSelect')?.value;

            const resolvedStatusId = triggerType === 'Event' ? (applicationStatusId || null) : null;

            const bodyObj = {
                templateId: templateId,
                triggerType: triggerType,
                dateType: triggerType === 'Date' ? dateType : null,
                applicationStatusId: resolvedStatusId,
                recipientCategory: triggerType === 'Event' ? recipientCategory : null,
                recipientIdentifier: triggerType === 'Event' ? recipientIdentifier : null
            };

            const editId = document.getElementById('notificationModal').dataset.editId;
            const isEdit = !!editId;
            const url = isEdit
                ? '/api/form-notifications/' + encodeURIComponent(formId) + '/' + encodeURIComponent(editId)
                : '/api/form-notifications/' + encodeURIComponent(formId);
            const method = isEdit ? 'PUT' : 'POST';

            fetch(url, {
                method: method,
                headers: { 'Content-Type': 'application/json', 'Accept': 'application/json', 'RequestVerificationToken': abp.security.antiForgery.getToken() },
                body: JSON.stringify(bodyObj)
            }).then(async (r) => {
                if (!r.ok) {
                    const text = await r.text();
                    console.warn('Create notification failed', r.status, text);
                    throw new Error(text || ('HTTP ' + r.status));
                }
                return r.json();
            }).then(() => {
                let modalEl = document.getElementById('notificationModal');
                bootstrap.Modal.getInstance(modalEl)?.hide();
                reloadTable();
            }).catch(err => {
                console.error(err);
                abp.notify.error('Failed to save notification: ' + err.message);
            });
        });
    }

    document.addEventListener('DOMContentLoaded', () => {
        init();
        // Load statuses and initial recipients
        fetchStatuses().then(populateStatuses);
    });

    function configureSubmitOnlyValidation() {
        if (!globalThis.jQuery) return;
        const form = getNotificationForm();
        if (!form) return;
        const $form = globalThis.jQuery(form);
        if (typeof $form.validate !== 'function') return;
        const validator = $form.data('validator') || $form.validate();
        validator.settings.onfocusout = false;
        validator.settings.onkeyup = false;
        validator.settings.onclick = false;
    }

    function resetValidationState() {
        const form = document.getElementById('notificationForm');
        if (!form) return;

        // Remove all invalid states from all form fields
        form.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));
        form.classList.remove('was-validated');

        if (globalThis.jQuery) {
            const $form = globalThis.jQuery(form);
            const validator = $form.data('validator');
            if (validator && typeof validator.resetForm === 'function') {
                validator.resetForm();
            }
        }
    }

    function getNotificationForm() {
        const modalEl = document.getElementById('notificationModal');
        return modalEl ? modalEl.querySelector('form') : null;
    }

    const responseCallback = function (result) {
        return { recordsTotal: result.length, recordsFiltered: result.length, data: result };
    };
})();
