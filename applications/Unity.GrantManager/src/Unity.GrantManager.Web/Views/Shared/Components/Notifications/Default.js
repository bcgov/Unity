(function () {
    let formId;

    function fetchTemplates() {
        return fetch('/api/form-notifications/templates').then(r => r.json());
    }

    function fetchNotifications() {
        return fetch('/api/form-notifications/' + encodeURIComponent(formId)).then(r => r.json());
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

    function renderActions(data, type, row) {
        return '<button class="btn btn-sm btn-outline-danger delete-notification" data-id="' + row.id + '">Delete</button>';
    }

    function refreshTable(list) {
        if (notificationsTable) {
            notificationsTable.clear().rows.add(list || []).draw();
        } else {
            renderList(list);
        }
    }

    function onDeleteNotification(ev) {
        ev.preventDefault();
        const id = $(this).data('id');
        if (!id) return;
        if (!confirm('Delete this scheduled notification?')) return;
        fetch('/api/form-notifications/' + encodeURIComponent(formId) + '/' + encodeURIComponent(id), { method: 'DELETE' })
            .then(r => {
                if (!r.ok) throw new Error('Failed to delete');
                abp.notify.success('Notification deleted');
                return fetchNotifications();
            })
            .then(list => refreshTable(list))
            .catch(err => {
                console.error(err);
                abp.notify.error('Failed to delete notification');
            });
    }

    let notificationsTable;
    function renderList(items) {
        const container = document.getElementById('notifications-list');
        if (!container) return;

        // Initialize DataTable if not already
        if (notificationsTable == null) {
            notificationsTable = $('#NotificationsTable').DataTable(
                abp.libs.datatables.normalizeConfiguration({
                    serverSide: false,
                    paging: true,
                    searching: true,
                    data: items || [],
                    columns: [
                        { title: 'Template', data: 'templateName' },
                        { title: 'Trigger Type', data: 'triggerType' },
                        { title: 'Trigger Detail', data: null, render: renderTriggerDetail },
                        { title: 'Actions', data: null, orderable: false, render: renderActions }
                    ],
                    lengthMenu: [10, 25, 50]
                })
            );

            // delete handler (named function reduces nesting)
            $('#NotificationsTable').on('click', '.delete-notification', onDeleteNotification);
        } else {
            // update existing table
            notificationsTable.clear().rows.add(items || []).draw();
        }
    }

    function populateTemplates(templates) {
        const sel = document.getElementById('templateSelect');
        sel.innerHTML = '';
        const blank = document.createElement('option');
        blank.value = '';
        blank.text = '-- Select --';
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
        blank.text = '-- Select --';
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
        fetch('/api/form-notifications/templates').then(r=>r.json()).then(list => {
            const t = list.find(x => String(x.id) === String(val));
            preview.innerText = t ? `${t.subject}\n\n${t.body}` : '';
        });
    }


    function showModal() {
        // Reset form fields
        ['templateSelect', 'triggerType', 'dateType', 'statusSelect', 'recipientCategory', 'recipientSelect'].forEach(id => {
            const el = document.getElementById(id);
            if (el) { el.value = ''; el.classList.remove('is-invalid'); }
        });
        document.getElementById('dateOptions').style.display = 'none';
        document.getElementById('eventOptions').style.display = 'none';
        document.getElementById('templatePreview').innerText = '';
        const modalEl = document.getElementById('notificationModal');
        if (!modalEl) return;
        const modal = new bootstrap.Modal(modalEl);
        modal.show();
        // modal shown; columns are fixed 33%/66%
    }

    function validateForm(triggerType) {
        let valid = true;
        function check(id) {
            const el = document.getElementById(id);
            if (!el) return;
            const ok = (el.value || '').trim() !== '';
            el.classList.toggle('is-invalid', !ok);
            if (!ok) valid = false;
        }
        check('templateSelect');
        check('triggerType');
        if (triggerType === 'Date') {
            check('dateType');
            ['statusSelect', 'recipientCategory', 'recipientSelect'].forEach(id => {
                document.getElementById(id)?.classList.remove('is-invalid');
            });
        } else if (triggerType === 'Event') {
            check('statusSelect');
            check('recipientCategory');
            check('recipientSelect');
            document.getElementById('dateType')?.classList.remove('is-invalid');
        }
        return valid;
    }

    function init() {
        formId = document.getElementById('applicationFormId')?.value;
        if (!formId) return;
        fetchTemplates().then(populateTemplates);
        fetchNotifications().then(list => renderList(list));

        document.getElementById('btn-add-notification')?.addEventListener('click', () => showModal());

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

            const bodyObj = {
                templateId: templateId,
                triggerType: triggerType,
                dateType: triggerType === 'Date' ? dateType : null,
                applicationStatusId: triggerType === 'Event' ? (applicationStatusId ? applicationStatusId : null) : null,
                recipientCategory: triggerType === 'Event' ? recipientCategory : null,
                recipientIdentifier: triggerType === 'Event' ? recipientIdentifier : null
            };

            console.debug('POST /api/form-notifications payload', { formId, bodyObj });

            fetch('/api/form-notifications/' + encodeURIComponent(formId), {
                method: 'POST',
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
                fetchNotifications().then(refreshTable);
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
})();
