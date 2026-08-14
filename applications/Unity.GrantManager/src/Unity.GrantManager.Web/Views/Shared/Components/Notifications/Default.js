(function () {
    let formId;
    let notificationsTable;
    let templateAttachmentsTable;
    let select2Ready = false;
    let select2Loading = false;
    let initialized = false;  // Guard against reinitializing
    let isSaving = false;  // Guard against duplicate submissions

    // Load Select2 library dynamically only after jQuery is available
    function loadSelect2Library() {
        if (select2Loading || select2Ready || ($ === undefined)) {
            return;
        }
        select2Loading = true;

        const script = document.createElement('script');
        script.src = '/libs/select2/dist/js/select2.min.js';
        script.onerror = function() {
            console.error('Failed to load Select2 library');
            select2Loading = false;
        };
        document.head.appendChild(script);
    }

    // Wait for select2 to be available, then initialize
    function waitForSelect2() {
        if ($ === undefined) {
            // jQuery not available yet, try again
            setTimeout(waitForSelect2, 50);
            return;
        }

        if (!select2Loading && ($.fn === undefined || !$.fn.select2)) {
            // Select2 script not loaded yet, load it
            loadSelect2Library();
            setTimeout(waitForSelect2, 100);
            return;
        }

        if ($.fn?.select2) {
            if (!select2Ready) {
                select2Ready = true;
                try {
                    $('#recipientSelect').select2({
                        theme: 'bootstrap-5',
                        width: '100%',
                        closeOnSelect: true,
                        allowClear: false
                    });
                } catch (e) {
                    console.error('Failed to initialize Select2:', e);
                    select2Ready = false;
                }
            }
            return true;
        }
        return false;
    }

    // Keep trying to initialize select2 until it's available
    function initSelect2WhenReady() {
        if (!select2Ready) {
            if (waitForSelect2()) {
                return;
            }
            // Keep trying
            setTimeout(initSelect2WhenReady, 100);
        }
    }

    // Helper function to get selected recipient values (filters out blank option)
    function getSelectedRecipients() {
        if (select2Ready && $ !== undefined) {
            const val = $('#recipientSelect').val();
            const toArray = (val) => {
            if (Array.isArray(val)) return val;
            return val ? [val] : [];
            };

            const arr = toArray(val);
            // Filter out empty strings (the blank option)
            return arr.filter(v => v && v.trim() !== '');
        }
        
        // Fallback: get selected options directly
        return Array.from(document.getElementById('recipientSelect')?.options ?? [])
            .filter(opt => opt.selected && opt.value)
            .map(opt => opt.value);
    }

    // Helper function to set selected values
    function setSelectedRecipients(values) {
        let valueArr;
        if (Array.isArray(values)) {
            valueArr = values;
        } else if (values) {
            valueArr = [values];
        } else {
            valueArr = [];
        }
        
        // Set selected on all options
        Array.from(document.getElementById('recipientSelect')?.options ?? []).forEach(opt => {
            opt.selected = valueArr.includes(opt.value);
        });
        
        // Refresh select2 if available
        if (select2Ready && $.fn?.select2) {
            try {
                $('#recipientSelect').trigger('change');
            } catch (e) {
                console.debug('Select2 refresh failed:', e.message);
            }
        }
    }

    // Helper function to clear selected values
    function clearSelectedRecipients() {
        setSelectedRecipients([]);
    }

    // Refresh select2 if ready
    function refreshSelect2() {
        if (select2Ready && $.fn?.select2) {
            try {
                $('#recipientSelect').trigger('change');
            } catch (e) {
                console.debug('Select2 refresh failed:', e.message);
            }
        }
    }

    function fetchTemplates() {
        return fetch('/api/form-notifications/templates').then(r => r.json());
    }

    function fetchStatuses() {
        return fetch('/api/form-notifications/statuses').then(r => r.json());
    }
    function fetchPaymentStatuses() {
        return fetch('/api/form-notifications/payment-statuses').then(r => r.json());
    }

    function fetchRecipients(category) {
        return fetch('/api/form-notifications/recipients?category=' + encodeURIComponent(category)).then(r => r.json());
    }

    function renderTriggerDetail(data, type, row) {
        let detail = '';
        if (row.triggerType === 'Date') {
            detail = row.dateType ? row.dateType : '';
        } else {
            detail = row.eventStatus ? row.eventStatus : '';
        }
        if (row.recipientCategory) {
            detail += (detail ? ' → ' : '') + 'Category: ' + row.recipientCategory;
        }
        if (row.recipientIdentifier) {
            detail += (detail ? ', ' : '') + 'Recipients: ' + row.recipientIdentifier;
        }
        return detail;
    }

    function renderTriggerType(data, type, row) {
        if (row.triggerType === 'Event' && row.module) {
            return 'Event - ' + row.module;
        }

        return row.triggerType || '';
    }

    function getNotificationColumns() {
        let index = 0;
        return [
            { title: 'Template',      name: 'templateName', data: 'templateName', visible: true, index: index++ },
            { title: 'Trigger Type',  name: 'triggerType',  data: 'triggerType',  visible: true, index: index++, render: renderTriggerType },
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
        // Check if DataTable is already initialized
        const table = $('#NotificationsTable');
        if ($.fn.dataTable.isDataTable('#NotificationsTable')) {
            // Already initialized, just reload the data
            notificationsTable = table.DataTable();
            notificationsTable.ajax.reload();
            return;
        }

        // Use $.ajax so abp.libs.datatables.createAjax gets a jQuery-compatible deferred (has .always())
        function notificationsEndpoint() {
            return $.ajax({
                url: '/api/form-notifications/' + encodeURIComponent(formId),
                type: 'GET',
                dataType: 'json'
            });
        }

        notificationsTable = initializeDataTable({
            dt: table,
            listColumns: getNotificationColumns(),
            defaultVisibleColumns: ['templateName', 'triggerType', 'triggerDetail', 'status', 'rowActions'],
            defaultSortColumn: { name: 'templateName', dir: 'asc' },
            dataEndpoint: notificationsEndpoint,
            data: {},
            responseCallback,
            actionButtons: [],
            pagingEnabled: true,
            scrollY: 'calc(100vh - 280px)',
            scrollCollapse: true,
            reorderEnabled: false,
            fixedHeaders: true,
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
    }

    function reloadTable() {
        if (notificationsTable) {
            notificationsTable.ajax.reload();
        }
    }

    // ScrollResize skips sizing while the table is hidden (e.g. an inactive Bootstrap
    // tab pane), so nothing pins the pagination bar to the bottom until we recalculate
    // it ourselves once the tab holding this widget becomes visible.
    function resizeNotificationsScrollBody() {
        if (!notificationsTable) return;
        const scrollResize = notificationsTable.settings?.()[0]?._scrollResize;
        if (scrollResize && typeof scrollResize._size === 'function') {
            scrollResize._size();
            return;
        }
        try {
            notificationsTable.columns.adjust();
        } catch (e) {
            console.debug('Notifications table column adjust failed:', e.message);
        }
    }

    function bindTabVisibilityResize() {
        document.addEventListener('shown.bs.tab', function (e) {
            const target = e.target;
            if (!target) return;
            const isNotificationsTab =
                target.id === 'nav-notifications-tab' ||
                target.getAttribute?.('data-bs-target') === '#nav-notifications';
            if (isNotificationsTab) {
                resizeNotificationsScrollBody();
            }
        });

        const notificationsSection = document.getElementById('notifications-div');
        const notificationsMenuItem = document.getElementById('notifications-menu-item');
        const resizeWhenVisible = () => {
            if (!notificationsSection?.offsetParent) return;
            setTimeout(resizeNotificationsScrollBody, 0);
        };

        notificationsMenuItem?.addEventListener('click', resizeWhenVisible);
        if (notificationsSection && typeof MutationObserver !== 'undefined') {
            const observer = new MutationObserver(resizeWhenVisible);
            observer.observe(notificationsSection, { attributes: true, attributeFilter: ['class', 'style'] });
        }

        window.addEventListener('resize', resizeWhenVisible);
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
        resetValidationState();

        // Load recipients for both Event and Date trigger types
        if (row.recipientCategory) {
            try {
                const list = await fetchRecipients(row.recipientCategory);
                populateRecipients(list);
            } catch (err) {
                console.error('Failed to pre-load recipients for edit modal', err);
            }
        }

        const modalEl = document.getElementById('notificationModal');
        if (modalEl) {
            modalEl.dataset.editId = row.id;
        }
        document.getElementById('notificationModal')?.addEventListener('shown.bs.modal', async function () {
            const setVal = (id, val) => {
                document.getElementById(id).value = val ?? '';
            };

            setVal('templateSelect', row.templateId);
            updatePreview();

            setVal('triggerType', row.triggerType);
            // Use class-based visibility
            const dateOptionsEl = document.getElementById('dateOptions');
            const eventOptionsEl = document.getElementById('eventOptions');
            const recipientOptionsEl = document.getElementById('recipientOptions');
            
            if (row.triggerType === 'Date') {
                dateOptionsEl?.classList.remove('hidden-section');
                eventOptionsEl?.classList.add('hidden-section');
                recipientOptionsEl?.classList.remove('hidden-section');
            } else if (row.triggerType === 'Event') {
                dateOptionsEl?.classList.add('hidden-section');
                eventOptionsEl?.classList.remove('hidden-section');
                recipientOptionsEl?.classList.remove('hidden-section');
            }

            if (row.triggerType === 'Date') {
                setVal('dateType', row.dateType);
                setVal('recipientCategory', row.recipientCategory);
                // Set multiple values for recipient select
                const values = row.recipientIdentifier ? row.recipientIdentifier.split(',').map(v => v.trim()) : [];
                setSelectedRecipients(values);
            } else if (row.triggerType === 'Event') {
                setVal('moduleSelect', row.module);
                await loadStatusesForModule(row.module);
                setVal('statusSelect', row.applicationStatusId || row.eventStatus);
                setVal('recipientCategory', row.recipientCategory);
                // Set multiple values for recipient select
                const values = row.recipientIdentifier ? row.recipientIdentifier.split(',').map(v => v.trim()) : [];
                setSelectedRecipients(values);
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
        [...templates]
            .sort((left, right) => left.name.localeCompare(right.name, undefined, { sensitivity: 'base' }))
            .forEach(t => {
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
    async function loadStatusesForModule(module) {
        const statusSelect = document.getElementById('statusSelect');
        if (!statusSelect) return;

        statusSelect.innerHTML = '';
        const blank = document.createElement('option');
        blank.value = '';
        blank.text = '';
        statusSelect.appendChild(blank);
        statusSelect.disabled = !module;

        if (!module) return;

        const statuses = module === 'Payment'
            ? await fetchPaymentStatuses()
            : await fetchStatuses();
        populateStatuses(statuses);
        statusSelect.disabled = false;
    }

    function populateRecipients(list) {
        const sel = document.getElementById('recipientSelect');
        if (!sel) return;
        sel.innerHTML = '';
        // Add a blank default option first (required by spec, but hidden from dropdown)
        const empty = document.createElement('option');
        empty.value = '';
        empty.text = '';
        empty.disabled = true; // Hide from Select2 dropdown
        sel.appendChild(empty);
        list.forEach(r => {
            const opt = document.createElement('option');
            opt.value = r.id;
            opt.text = r.displayName;
            sel.appendChild(opt);
        });
        // Refresh select2 if available
        refreshSelect2();
    }

    function decodeHtmlEntities(value) {
        if (!value || typeof value !== 'string') {
            return '';
        }

        const parser = new DOMParser();
        const doc = parser.parseFromString(value, 'text/html');
        return doc.documentElement.textContent || '';
    }

    function decodeHtmlEntitiesRecursively(value, maxPasses = 12) {
        let current = value || '';
        for (let i = 0; i < maxPasses; i++) {
            const decoded = decodeHtmlEntities(current);
            if (!decoded || decoded === current) {
                break;
            }
            current = decoded;

            if (/<[a-z][\s\S]*>/i.test(current)) {
                break;
            }
        }
        return current;
    }

    function resolveTemplatePreviewBody(template) {
        const rawBody =
            template?.body ||
            template?.bodyHtml ||
            template?.bodyHTML ||
            template?.Body ||
            template?.BodyHtml ||
            template?.BodyHTML ||
            '';

        const decodedBody = decodeHtmlEntitiesRecursively(rawBody);

        // Prefer decoded content when it results in actual HTML tags.
        if (/<[a-z][\s\S]*>/i.test(decodedBody)) {
            return decodedBody;
        }

        // Fall back to raw body when it already contains HTML tags.
        if (/<[a-z][\s\S]*>/i.test(rawBody)) {
            return rawBody;
        }

        // Plain text body (no tags) still displays correctly.
        return decodedBody || rawBody;
    }

    function renderTemplatePreview(previewElement, template) {
        if (!previewElement) {
            return;
        }

        previewElement.replaceChildren();

        if (!template) {
            return;
        }

        const subjectRow = document.createElement('div');
        const subjectLabel = document.createElement('strong');
        subjectLabel.textContent = 'Subject: ';
        const subjectValue = document.createElement('span');
        subjectValue.textContent = template.subject || template.Subject || '';
        subjectRow.append(subjectLabel, subjectValue);

        const bodyRow = document.createElement('div');
        bodyRow.innerHTML = resolveTemplatePreviewBody(template);

        previewElement.append(subjectRow, document.createElement('br'), bodyRow);
    }

    function updatePreview() {
        const sel = document.getElementById('templateSelect');
        const preview = document.getElementById('templatePreview');
        if (sel === null || preview === null) return;
        const val = sel.value;
        updateTemplateAttachments(val);
        fetch('/api/form-notifications/templates').then(r => r.json()).then(list => {
            const t = list.find(x => String(x.id) === String(val));
            renderTemplatePreview(preview, t);
        });
    }

    function notifyAttachmentCount(count) {
        if (count === 0) return;

        Swal.fire({
            toast: true,
            position: 'top-end',
            icon: 'info',
            text: count === 1 ? '1 attachment is associated with this template.' : `${count} attachments are associated with this template.`,
            showConfirmButton: false,
            timer: 3000,
            timerProgressBar: true
        });
    }

    function updateTemplateAttachments(templateId) {
        const section = document.getElementById('template-attachments-section');
        const label = document.getElementById('templateAttachmentsLabel');
        const countLabel = document.getElementById('templateAttachmentsCount');
        const table = $('#TemplateAttachmentsTable');
        if (!section || !table.length) return;

        if ($.fn.dataTable.isDataTable(table)) {
            table.DataTable().destroy();
        }
        templateAttachmentsTable = null;

        section.classList.add('hidden-section');
        label?.classList.add('hidden-section');
        if (countLabel) countLabel.textContent = '0';

        if (!templateId) {
            return;
        }

        templateAttachmentsTable = table.DataTable(
            abp.libs.datatables.normalizeConfiguration({
                serverSide: false,
                order: [[2, 'asc']],
                searching: false,
                paging: false,
                select: false,
                info: false,
                scrollX: true,
                scrollY: '80px', // ~2 rows visible before scrolling
                scrollCollapse: true,
                drawCallback: function () {
                    const count = this.api().rows().count();
                    if (countLabel) countLabel.textContent = String(count);
                    section.classList.toggle('hidden-section', count === 0);
                    label?.classList.toggle('hidden-section', count === 0);
                    notifyAttachmentCount(count);
                },
                ajax: abp.libs.datatables.createAjax(
                    unity.notifications.emails.emailLogAttachment.getListByTemplateId,
                    function () { return templateId; },
                    function (result) { return { data: result }; }
                ),
                columnDefs: [
                    {
                        title: '<i class="fl fl-paperclip"></i>',
                        width: '40px',
                        className: 'text-center',
                        orderable: false,
                        render: function () {
                            return '<i class="fl fl-paperclip"></i>';
                        }
                    },
                    {
                        title: 'Document Name',
                        data: 'fileName',
                        className: 'data-table-header text-break',
                        width: '55%'
                    },
                    {
                        title: 'Date',
                        data: 'time',
                        className: 'data-table-header',
                        width: '130px',
                        render: function (data, type) {
                            if (type === 'display' || type === 'filter') {
                                return new Date(data).toDateString();
                            }
                            return data;
                        }
                    },
                    {
                        title: 'File Size',
                        data: 'fileSize',
                        className: 'data-table-header',
                        width: '90px',
                        render: function (data) {
                            if (!data) return '—';
                            const mb = data * 0.000001;
                            return mb >= 1 ? mb.toFixed(2) + ' MB' : (data / 1024).toFixed(0) + ' KB';
                        }
                    }
                ]
            })
        );
    }

    function showModal() {
        resetValidationState();

        ['templateSelect', 'triggerType', 'dateType', 'moduleSelect', 'statusSelect', 'recipientCategory'].forEach(id => {
            document.getElementById(id).value = '';
        });
        document.getElementById('statusSelect').disabled = true;

        // Clear the recipient select
        clearSelectedRecipients();

        // Hide all option sections using classes
        document.getElementById('dateOptions')?.classList.add('hidden-section');
        document.getElementById('eventOptions')?.classList.add('hidden-section');
        document.getElementById('recipientOptions')?.classList.add('hidden-section');
        renderTemplatePreview(document.getElementById('templatePreview'), null);
        updateTemplateAttachments('');

        const modalEl = document.getElementById('notificationModal');
        if (modalEl === null) return;
        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    }

    function validateForm(triggerType) {
        const form = document.getElementById('notificationForm');
        if (!form) return false;

        let valid = true;

        const requiredAlways = ['templateSelect', 'triggerType'];
        const requiredForDate = ['dateType', 'recipientCategory', 'recipientSelect'];
        const requiredForEvent = ['moduleSelect', 'statusSelect', 'recipientCategory', 'recipientSelect'];

        const fieldsToValidate = [
            ...requiredAlways,
            ...(triggerType === 'Date' ? requiredForDate : []),
            ...(triggerType === 'Event' ? requiredForEvent : [])
        ];

        // Clear all first
        form.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));

        fieldsToValidate.forEach(id => {
            const el = document.getElementById(id);
            if (!el) return;
            
            let isEmpty = false;
            if (el.id === 'recipientSelect') {
                // Recipients come from a Select2 control, so check the selection
                isEmpty = getSelectedRecipients().length === 0;
            } else {
                isEmpty = !el.value;
            }
            
            if (isEmpty) {
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
        return container.innerHTML;
    }

    function init() {
        // Prevent multiple initializations
        if (initialized) {
            console.warn('init() called again but already initialized, returning early');
            return;
        }

        const modalEl = document.getElementById('notificationModal');
        if (modalEl && modalEl.parentElement !== document.body) {
            document.body.appendChild(modalEl);
        }

        formId = document.getElementById('applicationFormId')?.value;
        if (!formId) {
            console.warn('formId not found, returning from init()');
            return;
        }

        initialized = true;
        console.debug('Marking initialized = true');

        configureSubmitOnlyValidation();

        if (modalEl) {
            // Always reset validation when modal is fully closed
            modalEl.addEventListener('hidden.bs.modal', () => {
                resetValidationState();
                updateTemplateAttachments('');
            });
            // Also reset when modal starts opening
            modalEl.addEventListener('show.bs.modal', () => resetValidationState());
            // Refresh select2 when modal is shown
            modalEl.addEventListener('shown.bs.modal', () => {
                refreshSelect2();
            });
        }

        // Prevent form submission - use button click only
        const form = document.getElementById('notificationForm');
        if (form) {
            form.addEventListener('submit', (e) => {
                console.debug('Form submit prevented');
                e.preventDefault();
                e.stopPropagation();
                return false;
            });
        }

        fetchTemplates().then(populateTemplates);
        initNotificationsTable();
        bindTabVisibilityResize();
        // In case the widget is already visible on load (not inside a hidden tab pane)
        resizeNotificationsScrollBody();

        // Attach save button listener (remove old one first to prevent duplicates)
        const saveBtn = document.getElementById('btn-save-notification');
        if (saveBtn) {
            saveBtn.removeEventListener('click', handleSaveNotification);
            saveBtn.addEventListener('click', handleSaveNotification);
            console.debug('Save button listener attached');
        }

        document.getElementById('btn-add-notification')?.addEventListener('click', () => {
            document.getElementById('notificationModal').dataset.editId = '';
            showModal();
        });

        document.getElementById('templateSelect')?.addEventListener('change', (e) => {
            e.target.classList.remove('is-invalid');
            updatePreview();
        });
        document.getElementById('templateConfigurationLink')?.addEventListener('click', () => {
            const templateId = document.getElementById('templateSelect')?.value;
            if (templateId) {
                localStorage.setItem('notifications-template-to-select', templateId);
            } else {
                localStorage.removeItem('notifications-template-to-select');
            }
        });
        ['dateType', 'moduleSelect', 'statusSelect'].forEach(id => {
            document.getElementById(id)?.addEventListener('change', (e) => {
                e.target.classList.remove('is-invalid');
            });
        });
        document.getElementById('recipientSelect')?.addEventListener('change', (e) => {
            e.target.classList.remove('is-invalid');
        });
        document.getElementById('triggerType')?.addEventListener('change', (e) => {
            const val = e.target.value;
            const dateOptionsEl = document.getElementById('dateOptions');
            const eventOptionsEl = document.getElementById('eventOptions');
            const recipientOptionsEl = document.getElementById('recipientOptions');
            
            // Show/hide sections based on trigger type using classes
            if (val === 'Date') {
                dateOptionsEl?.classList.remove('hidden-section');
                eventOptionsEl?.classList.add('hidden-section');
                recipientOptionsEl?.classList.remove('hidden-section');
            } else if (val === 'Event') {
                dateOptionsEl?.classList.add('hidden-section');
                eventOptionsEl?.classList.remove('hidden-section');
                recipientOptionsEl?.classList.remove('hidden-section');
                const moduleSelect = document.getElementById('moduleSelect');
                const statusSelect = document.getElementById('statusSelect');
                if (moduleSelect?.value) {
                    loadStatusesForModule(moduleSelect.value);
                } else if (statusSelect) {
                    statusSelect.disabled = true;
                }
            } else {
                dateOptionsEl?.classList.add('hidden-section');
                eventOptionsEl?.classList.add('hidden-section');
                recipientOptionsEl?.classList.add('hidden-section');
            }
            
            e.target.classList.remove('is-invalid');
        });

        document.getElementById('moduleSelect')?.addEventListener('change', (e) => {
            e.target.classList.remove('is-invalid');
            loadStatusesForModule(e.target.value).catch(err => {
                console.error('Failed to load module statuses', err);
                abp.notify.error('Failed to load status triggers');
            });
        });

        document.getElementById('recipientCategory')?.addEventListener('change', (e) => {
            const cat = e.target.value;
            e.target.classList.remove('is-invalid');
            if (cat) fetchRecipients(cat).then(populateRecipients);
        });

    function handleSaveNotification(e) {
        if (e) {
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();
        }
        
        console.debug('handleSaveNotification called', { isSaving, timestamp: Date.now() });
        
        // Prevent duplicate submissions
        if (isSaving) {
            console.warn('Save already in progress, ignoring duplicate click');
            return;
        }

        const triggerType = document.getElementById('triggerType').value;

        if (!formId) {
            abp.notify.error('Form identifier not found on page. Cannot create notification.');
            console.error('Missing formId for notification POST', { formId });
            return;
        }

        if (!validateForm(triggerType)) return;

        const templateId = (document.getElementById('templateSelect').value || '').trim();
        const dateType = document.getElementById('dateType').value;
        const module = document.getElementById('moduleSelect')?.value;
        const statusValue = document.getElementById('statusSelect')?.value;
        const recipientCategory = document.getElementById('recipientCategory')?.value;
        
        // Collect multiple selected recipients as comma-separated string
        const recipientIdentifier = getSelectedRecipients().join(',');

        const resolvedStatusId = triggerType === 'Event' && module === 'Application' ? (statusValue || null) : null;

        const bodyObj = {
            templateId: templateId,
            triggerType: triggerType,
            module: triggerType === 'Event' ? module : null,
            dateType: triggerType === 'Date' ? dateType : null,
            applicationStatusId: resolvedStatusId,
            eventStatus: triggerType === 'Event' && module === 'Payment' ? (statusValue || null) : null,
            recipientCategory: recipientCategory,
            recipientIdentifier: recipientIdentifier
        };

        const editId = document.getElementById('notificationModal').dataset.editId;
        const isEdit = !!editId;
        const url = isEdit
            ? '/api/form-notifications/' + encodeURIComponent(formId) + '/' + encodeURIComponent(editId)
            : '/api/form-notifications/' + encodeURIComponent(formId);
        const method = isEdit ? 'PUT' : 'POST';

        isSaving = true;
        console.debug('isSaving set to TRUE - Starting fetch', { method, isEdit, url, timestamp: Date.now() });
        
        fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json', 'Accept': 'application/json', 'RequestVerificationToken': abp.security.antiForgery.getToken() },
            body: JSON.stringify(bodyObj)
        }).then(async (r) => {
            console.debug('Fetch response received', { status: r.status, timestamp: Date.now() });
            if (!r.ok) {
                const text = await r.text();
                console.warn('Create notification failed', r.status, text);
                throw new Error(text || ('HTTP ' + r.status));
            }
            return r.json();
        }).then(() => {
            console.debug('Save completed, closing modal and reloading table', { timestamp: Date.now() });
            let modalEl = document.getElementById('notificationModal');
            bootstrap.Modal.getInstance(modalEl)?.hide();
            reloadTable();
        }).catch(err => {
            console.error('Save error:', err);
            abp.notify.error('Failed to save notification: ' + err.message);
        }).finally(() => {
            console.debug('isSaving set to FALSE - Save cycle complete', { timestamp: Date.now() });
            isSaving = false;
        });
    }
    }

    document.addEventListener('DOMContentLoaded', () => {
        // Initialize Select2 immediately - don't wait for applicationFormId
        initSelect2WhenReady();
        
        // Initialize the rest of the component
        init();
        
        // Load statuses and initial recipients
        fetchStatuses().then(populateStatuses);
    });

    function configureSubmitOnlyValidation() {
        if (globalThis.jQuery === undefined) return;
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

        if (globalThis.jQuery !== undefined) {
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
