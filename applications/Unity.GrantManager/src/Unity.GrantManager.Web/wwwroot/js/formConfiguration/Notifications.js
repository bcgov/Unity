(function () {
    const formId = document.getElementById('applicationFormId')?.value || null;

    const $table = $('#NotificationsTable');
    let dataTable;

    function fetchList() {
        return fetch('/api/form-notifications/' + encodeURIComponent(formId ?? 'global')).then(r => r.json());
    }

    function loadTemplates() {
        return fetch('/api/form-notifications/templates').then(r => r.json()).then(handleTemplatesList);
    }

    function loadStatuses() {
        return fetch('/api/form-notifications/statuses').then(r => r.json()).then(list => {
            const sel = document.getElementById('cf_appStatus');
            sel.innerHTML = '';
            // Expecting list of status DTOs { id, internalStatus }
            list.forEach(s => {
                const opt = document.createElement('option');
                opt.value = s.id;
                opt.text = s.internalStatus;
                sel.appendChild(opt);
            });
            return list;
        });
    }

    function loadRecipients(category) {
        return fetch('/api/form-notifications/recipients?category=' + encodeURIComponent(category)).then(r => r.json()).then(list => {
            const sel = document.getElementById('cf_recipient');
            sel.innerHTML = '';
            list.forEach(rp => {
                const opt = document.createElement('option');
                opt.value = rp.id;
                opt.text = rp.displayName;
                sel.appendChild(opt);
            });
            return list;
        });
    }

    function updatePreview() {
        const sel = document.getElementById('cf_template');
        const preview = document.getElementById('previewCard');
        if (!sel || !preview) return;
        fetch('/api/form-notifications/templates').then(r => r.json()).then(list => {
            const t = list.find(x => String(x.id) === sel.value);
            preview.innerText = t ? `Subject: ${t.subject}\n\n${t.body}` : '';
        });
    }

    function renderTable(items) {
        if (dataTable) {
            dataTable.clear();
            dataTable.rows.add(items);
            dataTable.draw();
            return;
        }

        dataTable = $table.DataTable(abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            paging: true,
            searching: true,
            responsive: true,
            data: items,
            columns: [
                { data: 'templateName', title: 'Template' },
                { data: 'triggerType', title: 'Trigger Type' },
                { data: 'eventStatus', title: 'Trigger Detail', render: function (d, t, r) {
                    return formatTriggerDetail(r);
                } },
                { data: 'isActive', title: 'Status', render: function (d) { return d ? '<span class="badge bg-success">Active</span>' : '<span class="badge bg-secondary">Inactive</span>'; } },
                {
                    data: null,
                    orderable: false,
                    render: function (data, type, row) {
                        return `<div class="btn-group">
                                    <button class="btn btn-sm btn-outline-primary btn-edit" data-id="${row.id}">Edit</button>
                                    <button class="btn btn-sm btn-outline-danger btn-delete" data-id="${row.id}">Delete</button>
                                </div>`;
                    }
                }
            ]
        }));
    }

    function init() {
        loadTemplates();
        loadStatuses();
        loadRecipients(document.getElementById('cf_recipientCategory')?.value || 'Internal');

        document.getElementById('cf_template')?.addEventListener('change', updatePreview);
        document.getElementById('cf_recipientCategory')?.addEventListener('change', (e) => loadRecipients(e.target.value));

        document.getElementById('triggerEvent')?.addEventListener('change', () => toggleTriggerSections());
        document.getElementById('triggerDate')?.addEventListener('change', () => toggleTriggerSections());
        toggleTriggerSections();

        document.getElementById('btn-open-create')?.addEventListener('click', () => {
            document.getElementById('cf_template').focus();
            window.scrollTo({ top: document.getElementById('cf_template').offsetTop - 100, behavior: 'smooth' });
        });

        document.getElementById('btn-save')?.addEventListener('click', onSave);
        document.getElementById('btn-cancel')?.addEventListener('click', onCancel);

        $table.on('click', '.btn-delete', onDeleteButtonClick);

        loadList();
    }

    function toggleTriggerSections() {
        const isDate = document.getElementById('triggerDate').checked;
        document.getElementById('dateSection').style.display = isDate ? 'block' : 'none';
        document.getElementById('eventSection').style.display = isDate ? 'none' : 'block';
    }

    function getApplicationStatusId(triggerType, appStatusId) {
        if (triggerType !== 'Event') {
            return null;
        }
        return appStatusId || null;
    }

    function calculateOffsetDays(triggerType, offsetType, offset) {
        if (triggerType !== 'Date') {
            return 0;
        }
        return offsetType === 'Before' ? -Math.abs(offset) : Math.abs(offset);
    }

    function formatTriggerDetail(r) {
        if (!r) return '';
        if (r.triggerType === 'Date') {
            const offset = Number(r.offsetDays) || 0;
            const offsetDisplay = offset > 0 ? ('+' + offset) : offset;
            let detail = `${r.dateType} ${offsetDisplay}`;
            // Include recipient category and identifier for date triggers
            if (r.recipientCategory && r.recipientIdentifier) {
                detail += ` → ${r.recipientCategory}: ${r.recipientIdentifier}`;
            }
            return detail;
        }
        return r.eventStatus || '';
    }

    function handleTemplatesList(list) {
        const sel = document.getElementById('cf_template');
        sel.innerHTML = '';
        list.forEach(t => {
            const opt = document.createElement('option');
            opt.value = String(t.id);
            opt.text = `${t.name} — ${t.subject}`;
            sel.appendChild(opt);
        });
        updatePreview();
        return list;
    }

    function loadList() {
        fetchList().then(renderTable).catch(err => console.error(err));
    }

    function onSave() {
        const btn = document.getElementById('btn-save');
        btn.disabled = true;

        const templateId = document.getElementById('cf_template').value;
        const triggerType = document.querySelector('input[name="triggerType"]:checked').value;
        const dateType = document.getElementById('cf_dateField').value;
        const offset = Number.parseInt(document.getElementById('cf_offset').value || '0', 10);
        const offsetType = document.getElementById('cf_offsetType').value;
        const appStatusId = document.getElementById('cf_appStatus').value;
        const recipientCategory = document.getElementById('cf_recipientCategory').value;
        
        // Get all selected recipients and join with comma for multiple selections
        const recipientSelect = document.getElementById('cf_recipient');
        const recipients = Array.from(recipientSelect.selectedOptions || []).map(opt => opt.value);
        const recipient = recipients.length > 0 ? recipients.join(',') : null;

        const body = {
            templateId: templateId,
            triggerType: triggerType,
            dateType: triggerType === 'Date' ? dateType : null,
            offsetDays: calculateOffsetDays(triggerType, offsetType, offset),
            applicationStatusId: getApplicationStatusId(triggerType, appStatusId),
            eventStatus: null,
            recipientCategory: recipientCategory || null,
            recipientIdentifier: recipient
        };

        fetch('/api/form-notifications/' + encodeURIComponent(formId ?? 'global'), {
            method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body)
        }).then(r => {
            if (!r.ok) throw new Error('Save failed');
            return r.json();
        }).then(() => {
            abp.notify.success('Notification saved');
            loadList();
        }).catch(err => { console.error(err); abp.notify.error('Save failed'); }).finally(() => btn.disabled = false);
    }

    function onCancel() {
        // reset form
        document.getElementById('cf_offset').value = '0';
    }

    function handleDeleteConfirmed(id) {
        fetch('/api/form-notifications/' + encodeURIComponent(formId ?? 'global') + '/' + id, { method: 'DELETE' }).then(r => {
            if (!r.ok) throw new Error('Delete failed');
            abp.notify.success('Deleted');
            loadList();
        }).catch(err => { console.error(err); abp.notify.error('Delete failed'); });
    }

    function onDeleteButtonClick(event) {
        const id = $(this).data('id');
        if (!id) return;
        abp.message.confirm(`Delete this notification?`).then(function (confirmed) {
            if (!confirmed) return;
            handleDeleteConfirmed(id);
        });
    }

    document.addEventListener('DOMContentLoaded', init);
})();
