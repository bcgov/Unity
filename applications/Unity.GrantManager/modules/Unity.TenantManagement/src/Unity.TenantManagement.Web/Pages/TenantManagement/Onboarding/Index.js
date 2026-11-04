(function () {
    let l = abp.localization.getResource('GrantManager');
    let _onboardingRequestAppService = unity.tenantManagement.onboardingRequest;
    let _dataTable = null;
    let _$table = null;
    let _defaultVisibleColumns = [];
    let _selectedRow = null;
    let _selectedRows = [];
    let _selectedCategory = 'Onboarding';

    const CATEGORY_STORAGE_KEY = 'Onboarding_SelectedCategory';
    const DEFAULT_VISIBLE_COLUMNS = ['submissionNumber', 'status', 'submissionDate', 'ownerName', 'assignees'];

    // ─── Fixed columns (always present) ──────────────────────────────────────

    const FIXED_COLUMNS = [
        {
            title: l('Onboarding:ColumnSubmissionNumber'),
            data: 'submissionNumber',
            name: 'submissionNumber',
            index: 0,
            render: function (data, type, row) {
                if (type !== 'display') return data ?? '';
                return '<a href="/GrantApplications/Details?ApplicationId=' + row.id + '">' + _escapeHtml(data) + '</a>';
            }
        },
        { title: l('Onboarding:ColumnStatus'),           data: 'status',           name: 'status',           index: 1 },
        {
            title: l('Onboarding:ColumnSubmissionDate'),
            data: 'submissionDate',
            name: 'submissionDate',
            index: 2,
            render: function (data, type) {
                return DateUtils.formatUtcDateToLocal(data, type);
            }
        },
        { title: l('Onboarding:ColumnCategory'), data: 'category', name: 'category', index: 3 },
        {
            title: l('Onboarding:ColumnOwner'),
            data: 'ownerName',
            name: 'ownerName',
            index: 4,
            defaultContent: '',
            render: function (data, type) {
                if (type !== 'display') return data ?? '';
                return _escapeHtml(data ?? '');
            }
        },
        {
            title: l('Onboarding:ColumnAssignee'),
            data: 'assignees',
            name: 'assignees',
            index: 5,
            orderable: false,
            defaultContent: '',
            render: function (data, type) {
                const text = _formatAssignees(data);
                return type === 'display' ? _escapeHtml(text) : text;
            }
        }
    ];

    // ─── Field value renderers ────────────────────────────────────────────────

    function _escapeHtml(str) {
        return String(str)
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;');
    }

    // "Jane Smith [Reviewer], Alex Chen [Program Lead]"; a role is optional.
    function _formatAssignees(assignees) {
        return (assignees || [])
            .map(function (a) { return a.role ? a.name + ' [' + a.role + ']' : a.name; })
            .join(', ');
    }

    function _formatCheckboxKey(key) {
        return String(key)
            .replaceAll(/([a-z])([A-Z])/g, '$1 $2')
            .replaceAll(/\b\w/g, function (c) { return c.toUpperCase(); });
    }

    // Worksheet CheckboxGroup values are always serialized as [{key, value}, ...].
    function _extractCheckboxLabels(data) {
        if (data === null || data === undefined || data === '') return [];
        try {
            const items = JSON.parse(data);
            if (Array.isArray(items)) {
                return items.filter(function (i) { return i?.value === true; }).map(function (i) { return i.key; });
            }
        } catch {
            // not a CheckboxGroup value
        }
        return [];
    }

    function _renderCheckboxGroup(data, type) {
        // No value submitted at all for this field — leave the cell blank, as opposed to
        // data being present but resolving to zero checked labels (handled below).
        if (data === null || data === undefined || data === '') return '';

        const labels = _extractCheckboxLabels(data);

        if (type === 'sort' || type === 'filter' || type === 'type') {
            return labels.join(', ');
        }

        if (labels.length === 0) return '<span class="text-muted">—</span>';

        return labels.map(function (label) {
            return '<span class="badge rounded-pill bg-light text-dark border me-1 onboarding-checkbox-badge">'
                + _escapeHtml(_formatCheckboxKey(label))
                + '</span>';
        }).join('');
    }

    // ─── Program Managers DataGrid email extraction ──────────────────────────

    // Formio/CHEFS "Program Managers" fields are submitted as a DataGrid: one row per
    // program manager, with columns such as name/email/title. The email column's key varies
    // per worksheet (e.g. "s03_SuperUserEmail" on forms authored before this terminology
    // changed), so it's matched by name rather than a fixed key — mirrors
    // ProgramManagersValidationStep.ParseEmails on the server.
    // Returns null when `raw` isn't a DataGrid value at all (legacy plain-text field).
    function _extractDataGridEmails(raw) {
        if (!raw) return null;
        let parsed;
        try { parsed = typeof raw === 'string' ? JSON.parse(raw) : raw; } catch { return null; }
        if (!parsed || !Array.isArray(parsed.rows)) return null;

        return parsed.rows
            .map(function (row) {
                const cell = (row.cells || []).find(function (c) { return /email/i.test(c.key || ''); });
                return cell ? String(cell.value || '').trim() : '';
            })
            .filter(function (v) { return v.includes('@'); });
    }

    // ─── DOM-based preview builders ──────────────────────────────────────────
    // Used by _updateFieldPreview below, which injects these via DOM append rather than
    // .html() — these build real elements with jQuery's .text() for any dynamic content
    // (the worksheet field value), so there's no HTML-string sink for CodeQL/XSS scanners
    // to flag, unlike the string-concatenation renderers above (which still serve the
    // DataTables column-render path, where a string return is required).

    function _buildMutedPlaceholder() {
        return $('<span>').addClass('text-muted').text('—');
    }

    function _buildCheckboxBadgesPreview(data) {
        const labels = _extractCheckboxLabels(data);
        if (labels.length === 0) return _buildMutedPlaceholder();

        return $(labels.map(function (label) {
            return $('<span>')
                .addClass('badge rounded-pill bg-light text-dark border me-1 onboarding-checkbox-badge')
                .text(_formatCheckboxKey(label))
                .get(0);
        }));
    }

    function _buildProgramManagersPreview(data) {
        const emails = _extractDataGridEmails(data);
        if (emails === null) {
            return data ? $('<span>').text(data) : _buildMutedPlaceholder();
        }
        return emails.length ? $('<span>').text(emails.join(', ')) : _buildMutedPlaceholder();
    }

    // ─── DataGrid cell renderer ───────────────────────────────────────────────

    let _dataGridCache = {};
    let _dataGridCacheId = 0;

    function _renderDataGridIcon(data, columnTitle) {
        if (!data) return '';

        let parsed;
        try {
            parsed = typeof data === 'string' ? JSON.parse(data) : data;
        } catch {
            return '';
        }
        if (!parsed || !Array.isArray(parsed.rows) || parsed.rows.length === 0) return '';

        const cacheKey = 'dg-' + (_dataGridCacheId++);
        _dataGridCache[cacheKey] = { grid: parsed, title: columnTitle };
        return '<button type="button" class="btn btn-icon btn-sm onboarding-datagrid-btn" data-datagrid-key="' + cacheKey + '" title="' + l('Onboarding:ViewDataGrid') + '"><i class="fa-solid fa-table-cells"></i></button>';
    }

    function _openDataGridModal(grid, title) {
        const keys = [];
        grid.rows.forEach(function (row) {
            (row.cells || []).forEach(function (cell) {
                if (!keys.includes(cell.key)) keys.push(cell.key);
            });
        });

        const headerHtml = keys.map(function (k) { return '<th>' + _escapeHtml(_formatCheckboxKey(k)) + '</th>'; }).join('');
        const rowsHtml = grid.rows.map(function (row) {
            const cellsByKey = {};
            (row.cells || []).forEach(function (c) { cellsByKey[c.key] = c.value; });
            const tds = keys.map(function (k) { return '<td>' + _escapeHtml(cellsByKey[k] ?? '') + '</td>'; }).join('');
            return '<tr>' + tds + '</tr>';
        }).join('');

        const modalHtml = '<div class="modal fade" id="onboardingDataGridModal" tabindex="-1">' +
            '<div class="modal-dialog modal-lg modal-dialog-scrollable">' +
            '<div class="modal-content">' +
            '<div class="modal-header">' +
            '<h5 class="modal-title">' + _escapeHtml(title || l('Onboarding:DataGridModalTitle')) + '</h5>' +
            '<button type="button" class="btn-close" data-bs-dismiss="modal"></button>' +
            '</div>' +
            '<div class="modal-body">' +
            '<table class="table table-bordered table-striped table-sm mb-3"><thead><tr>' + headerHtml + '</tr></thead><tbody>' + rowsHtml + '</tbody></table>' +
            '</div>' +
            '</div></div></div>';

        $('#onboardingDataGridModal').remove();
        $('body').append(modalHtml);
        const modalEl = document.getElementById('onboardingDataGridModal');
        const modal = new bootstrap.Modal(modalEl);
        modalEl.addEventListener('hidden.bs.modal', function () { $(modalEl).remove(); });
        modal.show();
    }

    $(document).on('click', '.onboarding-datagrid-btn', function (e) {
        e.stopPropagation();
        const cached = _dataGridCache[$(this).data('datagrid-key')];
        if (cached) _openDataGridModal(cached.grid, cached.title);
    });

    // ─── Column builder ───────────────────────────────────────────────────────

    // Tenant Name and Program Manager Email are required to create a tenant, so they are shown by
    // default. Their keys vary by form: use the saved field mapping, otherwise match on the label.
    function _requiredFieldKeys(schema) {
        const columns = schema?.columns || [];
        const find = function (savedKey, canonicals) {
            return columns.some(function (c) { return c.key === savedKey; }) ? savedKey : _bestMatch(columns, canonicals);
        };
        const tenantName = find(schema?.tenantNameFieldKey, TENANT_NAME_CANONICALS);
        const programManagers = find(schema?.programManagersFieldKey, PROGRAM_MANAGERS_CANONICALS);
        return { tenantName, programManagers: programManagers === tenantName ? null : programManagers };
    }

    function buildColumns(schema, requiredKeys) {
        let cols = FIXED_COLUMNS.map(function (c, i) { return { ...c, index: i }; });

        if (schema?.columns) {
            let offset = cols.length;
            schema.columns.forEach(function (c, i) {
                let col = {
                    title: c.label,
                    // Use a dot-path string (not a function) so DataTables/ABP can resolve
                    // the column's name for server-side sorting — a function data accessor
                    // breaks that resolution.
                    data: 'fields.' + c.key,
                    name: c.key,
                    defaultContent: '',
                    index: offset + i
                };
                if (c.type === 'Date') {
                    col.render = function (data, type) { return DateUtils.formatUtcDateToLocal(data, type); };
                } else if (c.type === 'CheckboxGroup') {
                    col.render = _renderCheckboxGroup;
                } else if (c.type === 'DataGrid') {
                    col.render = function (data, type) { return type === 'display' ? _renderDataGridIcon(data, c.label) : ''; };
                }
                cols.push(col);
            });
        }

        // Tenant Name follows Submission Date, and Program Manager Email ends the default columns.
        const permanent = cols.slice(0, FIXED_COLUMNS.length);
        const fields = cols.slice(FIXED_COLUMNS.length);
        const field = function (key) { return fields.find(function (c) { return c.name === key; }); };
        const tenantName = field(requiredKeys.tenantName);
        const programManagers = field(requiredKeys.programManagers);
        const dateIndex = permanent.findIndex(function (c) { return c.name === 'submissionDate'; }) + 1;

        return [
            ...permanent.slice(0, dateIndex),
            tenantName,
            ...permanent.slice(dateIndex),
            programManagers,
            ...fields.filter(function (c) { return c !== tenantName && c !== programManagers; })
        ].filter(Boolean).map(function (c, i) { return { ...c, index: i }; });
    }

    // ─── Saved views ──────────────────────────────────────────────────────────

    // Saved views and the remembered table state are stored against the table's id, and each
    // category has different columns, so giving each category its own id keeps them apart.
    function _tableIdForCategory(category) {
        const safeCategory = String(category).replaceAll(/[^a-zA-Z0-9]/g, function (c) {
            return '-' + c.codePointAt(0).toString(16);
        });
        return 'OnboardingRequestsTable_' + safeCategory;
    }

    // DataTables stores column order by position, which stops matching as soon as a form change
    // adds or removes a column, so the names are stored too.
    function _saveColumnOrderByName(settings, data) {
        data.columnOrderNames = settings.aoColumns.map(function (col) { return col.sName; });
    }

    function _restoreColumnOrderByName(data, listColumns) {
        if (!data?.columnOrderNames) return;

        const definedNames = listColumns.map(function (col) { return col.name; });
        const displayNames = data.columnOrderNames.filter(function (name) { return definedNames.includes(name); });
        definedNames.forEach(function (name) {
            if (!displayNames.includes(name)) displayNames.push(name);
        });

        data.colReorder = displayNames.map(function (name) { return definedNames.indexOf(name); });

        // ColReorder and the column filter restore both read this list in display order.
        const columnsByName = new Map((data.columns || []).map(function (col) { return [col.name, col]; }));
        data.columns = displayNames.map(function (name) { return columnsByName.get(name); }).filter(Boolean);

        // Sort is stored by name, but DataTables resolves it against the column positions from
        // before the reorder above, so hand it the position the column is moving to.
        data.order = (data.order || [])
            .map(function (o) { return [typeof o[0] === 'string' ? displayNames.indexOf(o[0]) : o[0], o[1]]; })
            .filter(function (o) { return o[0] >= 0; });
    }

    // Loading a view on a table that is already on screen applies visibility by position, using the
    // layout from before the view's column order was applied, and leaves any search or filter the
    // view doesn't have. The filter row rebuilds from its cache and inputs on every visibility
    // change, so filters are replaced before visibility is set by name.
    function _applyLoadedView(dtApi, data) {
        if (!dtApi.ready() || !data?.columns) return;

        const settings = dtApi.settings()[0];
        const names = settings.aoColumns.map(function (col) { return col.sName; });
        const filterRow = settings._filterRow;

        $(settings.oInit.externalSearchInputId).val(data.externalSearch || '');

        if (filterRow) {
            filterRow.dom.filterRow.find('.custom-filter-input').val('');
            const filterData = filterRow.s.filterData;
            Object.keys(filterData).forEach(function (name) { delete filterData[name]; });
            data.columns.forEach(function (col) {
                if (col.visible && col.search?.search) filterData[col.name] = col.search.search;
            });
        }

        data.columns.forEach(function (col) {
            const index = names.indexOf(col.name);
            if (index >= 0) dtApi.column(index).search(col.search?.search || '');
        });
        data.columns.forEach(function (col) {
            const index = names.indexOf(col.name);
            if (index >= 0 && col.visible !== undefined) dtApi.column(index).visible(col.visible, false);
        });
        dtApi.columns.adjust();
    }

    function _resetToDefaultView(dt) {
        dt.colReorder.reset();
        dt.columns().visible(false, false);
        _defaultVisibleColumns.forEach(function (name) {
            dt.column(name + ':name').visible(true, false);
        });
        dt.columns.adjust();
        dt.settings()[0]._filterRow?.clearFilters();
        // clearFilters restores dt.init().order, which ColReorder has moved along with the columns.
        dt.order([[0, 'asc']]).draw();

        dt.buttons('.grp-savedStates').container().find('.dt-button-collection').hide();
        $('div.dt-button-background').trigger('click');
    }

    function _savedViewButton() {
        return {
            extend: 'savedStates',
            className: 'custom-table-btn flex-none btn btn-secondary grp-savedStates',
            config: {
                creationModal: true,
                // A selection refers to rows on the current page, not to the view.
                saveState: { select: false },
                splitSecondaries: [
                    { extend: 'updateState', text: '<i class="fa-regular fa-floppy-disk" aria-hidden="true"></i> ' + l('OnboardingList:UpdateView') },
                    { extend: 'renameState', text: '<i class="fa-regular fa-pen-to-square" aria-hidden="true"></i> ' + l('OnboardingList:RenameView') },
                    { extend: 'removeState', text: '<i class="fa-regular fa-trash-can" aria-hidden="true"></i> ' + l('OnboardingList:DeleteView') }
                ]
            },
            buttons: [
                { extend: 'createState', text: l('OnboardingList:SaveAsView') },
                {
                    text: l('OnboardingList:ResetToDefaultView'),
                    action: function (e, dt) { _resetToDefaultView(dt); }
                },
                { extend: 'removeAllStates', text: l('OnboardingList:DeleteAllViews') },
                { extend: 'spacer', style: 'bar' }
            ]
        };
    }

    function _savedViewLanguage() {
        return {
            buttons: { stateRestore: l('OnboardingList:DefaultViewName') },
            stateRestore: {
                creationModal: {
                    title: l('OnboardingList:CreateViewTitle'),
                    name: l('OnboardingList:ViewNameLabel'),
                    button: l('OnboardingList:SaveViewButton')
                },
                emptyStates: l('OnboardingList:NoSavedViews'),
                renameTitle: l('OnboardingList:RenameViewTitle'),
                renameLabel: l('OnboardingList:RenameViewLabel'),
                removeTitle: l('OnboardingList:DeleteViewTitle'),
                removeConfirm: l('OnboardingList:DeleteViewConfirm'),
                removeSubmit: l('OnboardingList:DeleteView'),
                duplicateError: l('OnboardingList:DuplicateViewName'),
                removeError: l('OnboardingList:DeleteViewFailed')
            }
        };
    }

    function _showSaveViewLabel() {
        $('.grp-savedStates').text(l('OnboardingList:SaveView'))
            .prepend('<i class="fa-regular fa-bookmark" aria-hidden="true"></i> ');
    }

    // ─── DataTable init ───────────────────────────────────────────────────────

    function initTable(schema) {
        // Held once, because the id changes per category below.
        _$table ??= $('#OnboardingRequestsTable');

        if (_dataTable) {
            _dataTable.off('select deselect draw stateRestore-change');
            // The saved-view plugin's destroy handler removes the DataTables container, and this
            // table with it. Only the plugin's handlers need clearing before the rebuild.
            _$table.off('.dtsr dtsr-modal-inserted');
            _dataTable.destroy();
            _$table.empty();
            _dataTable = null;
            _selectedRow = null;
            _selectedRows = [];
            manageActionButtons();
        }

        const requiredKeys = _requiredFieldKeys(schema);
        let listColumns = buildColumns(schema, requiredKeys);
        _defaultVisibleColumns = [...DEFAULT_VISIBLE_COLUMNS, requiredKeys.tenantName, requiredKeys.programManagers].filter(Boolean);
        _$table.attr('id', _tableIdForCategory(_selectedCategory));

        _dataTable = initializeDataTable({
            dt: _$table,
            defaultVisibleColumns: _defaultVisibleColumns,
            listColumns: listColumns,
            defaultSortColumn: 0,
            dataEndpoint: _onboardingRequestAppService.getList,
            data: function (requestData) {
                const extras = { category: _selectedCategory };

                const globalSearch = requestData?.search?.value;
                if (globalSearch) extras.filter = globalSearch;

                // Column-level filters from FilterRow; category dropdown is always included
                const columnFilters = (requestData?.columns || [])
                    .filter(function (col) { return col?.name && col?.search?.value; })
                    .map(function (col) { return { name: col.name, value: col.search.value }; });

                columnFilters.push({ name: 'category', value: _selectedCategory });
                extras.columnFilters = columnFilters;

                return extras;
            },
            responseCallback: function (result) {
                return { recordsTotal: result.totalCount, recordsFiltered: result.totalCount, data: result.items };
            },
            actionButtons: [
                ...commonTableActionButtons(l('Menu:Onboarding')).filter(b => b.id !== 'btn-toggle-filter'),
                _savedViewButton()
            ],
            serverSideEnabled: true,
            pagingEnabled: true,
            reorderEnabled: true,
            languageSetValues: _savedViewLanguage(),
            onStateSaveParams: _saveColumnOrderByName,
            onStateLoadParams: function (settings, data) { _restoreColumnOrderByName(data, listColumns); },
            onStateLoaded: _applyLoadedView,
            dynamicButtonContainerId: 'dynamicButtonContainerId',
            externalSearchId: 'search',
            lengthMenu: [10, 25, 50]
        });

        _openRequestedCreateTenant();

        _dataTable.select.style('multi');

        _dataTable.on('select deselect', function (e, dt, type) {
            if (type === 'row') _syncSelection();
        });

        // A server-side redraw replaces the rows, which drops the selection without a deselect event.
        _dataTable.on('draw', _syncSelection);

        $('.grp-savedStates').closest('.btn-group').addClass('cstm-save-view');
        _dataTable.on('stateRestore-change', _showSaveViewLabel);
        _showSaveViewLabel();
    }

    // Open and Create Tenant act on one request (_selectedRow is only set for exactly one);
    // Assign acts on all of _selectedRows.
    function _syncSelection() {
        _selectedRows = _dataTable.rows({ selected: true }).data().toArray();
        _selectedRow = _selectedRows.length === 1 ? _selectedRows[0] : null;
        manageActionButtons();
    }

    // ─── Create Tenant requested from the request details page ───────────────

    // Details page links here with ?createTenant={applicationId}; open the modal once, then drop
    // the parameter so a refresh doesn't reopen it. The server still enforces Approved.
    let _requestedCreateTenantId = new URLSearchParams(globalThis.location.search).get('createTenant');

    function _openRequestedCreateTenant() {
        const id = _requestedCreateTenantId;
        _requestedCreateTenantId = null;
        if (!id || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(id)) return;

        globalThis.history.replaceState(null, '', globalThis.location.pathname);
        _createTenantModal.open({ id: id });
    }

    // ─── Action button state ──────────────────────────────────────────────────

    function manageActionButtons() {
        const hasSelection = _selectedRow !== null;
        $('#btn-open-onboarding').toggleClass('action-bar-btn-unavailable', !hasSelection);
        $('#btn-create-tenant').toggleClass('action-bar-btn-unavailable',
            !hasSelection || _selectedRow.status !== 'Approved');
        $('#btn-assign-onboarding').toggleClass('action-bar-btn-unavailable', _selectedRows.length === 0);
    }

    // ─── Field value preview ─────────────────────────────────────────────────

    let _fieldValues = {};

    function _updateFieldPreview(selectId, previewId, domBuilder) {
        const key = $('#' + selectId).val();
        const raw = key ? (_fieldValues[key] ?? '') : '';
        const text = raw ? String(raw) : '';
        const $preview = $('#' + previewId);
        if (domBuilder) {
            // domBuilder() returns real jQuery/DOM elements built with .text(), not an HTML
            // string — appended directly so there's no .html()-of-untrusted-string sink.
            $preview.empty().append(domBuilder(text));
        } else {
            $preview.text(text || '—').toggleClass('text-muted', !text);
        }
    }

    // ─── Create Tenant modal ──────────────────────────────────────────────────

    let _createTenantModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'TenantManagement/Onboarding/CreateTenantModal',
        modalClass: 'createTenantModal'
    });

    function _renderValidationResult(result) {
        $('#onboarding-validation-loading').addClass('d-none');
        if (result.isValid) {
            $('#onboarding-validation-result')
                .html('<div class="alert alert-success mb-0"><i class="fa-solid fa-check me-1"></i> ' + l('OnboardingModal:ValidationPassed') + '</div>')
                .removeClass('d-none');
            $('#btn-confirm-create-tenant').prop('disabled', false);
        } else {
            let issuesHtml = (result.issues || [])
                .map(function (i) { return '<li>' + $('<span>').text(i).html() + '</li>'; })
                .join('');
            $('#onboarding-validation-result')
                .html('<div class="alert alert-danger mb-0"><strong>' + l('OnboardingModal:ValidationFailed') + '</strong><ul class="mb-0 mt-1">' + issuesHtml + '</ul></div>')
                .removeClass('d-none');
        }
    }

    function _renderValidationFail() {
        $('#onboarding-validation-loading').addClass('d-none');
        $('#onboarding-validation-result')
            .html('<div class="alert alert-danger mb-0">' + l('OnboardingModal:ValidateFailed') + '</div>')
            .removeClass('d-none');
    }

    function _triggerValidation(applicationId) {
        $('#onboarding-validation-loading').removeClass('d-none');
        $('#onboarding-validation-result').addClass('d-none');
        $('#btn-confirm-create-tenant').prop('disabled', true);

        const tenantNameFieldKey  = $('#create-tenant-tenant-name-field').val() || null;
        const displayNameFieldKey = $('#create-tenant-display-name-field').val() || null;
        const programManagersFieldKey  = $('#create-tenant-program-managers-field').val() || null;
        const branchFieldKey      = $('#create-tenant-branch-field').val() || null;
        const featuresFieldKey    = $('#create-tenant-features-field').val() || null;
        const ministryFieldKey    = $('#create-tenant-ministry-field').val() || null;
        const divisionFieldKey    = $('#create-tenant-division-field').val() || null;
        const programAreaFieldKey = $('#create-tenant-program-area-field').val() || null;

        const params = new URLSearchParams();
        if (tenantNameFieldKey)  params.append('tenantNameFieldKey',  tenantNameFieldKey);
        if (displayNameFieldKey) params.append('displayNameFieldKey', displayNameFieldKey);
        if (programManagersFieldKey)  params.append('programManagersFieldKey',  programManagersFieldKey);
        if (branchFieldKey)      params.append('branchFieldKey',      branchFieldKey);
        if (featuresFieldKey)    params.append('featuresFieldKey',    featuresFieldKey);
        if (ministryFieldKey)    params.append('ministryFieldKey',    ministryFieldKey);
        if (divisionFieldKey)    params.append('divisionFieldKey',    divisionFieldKey);
        if (programAreaFieldKey) params.append('programAreaFieldKey', programAreaFieldKey);
        const query = params.size ? '?' + params.toString() : '';

        abp.ajax({
            url: abp.appPath + 'api/onboarding-requests/' + applicationId + '/validate' + query,
            type: 'GET'
        }).done(_renderValidationResult).fail(_renderValidationFail);
    }

    function _onCreateTenantConfirm(applicationId) {
        return function () {
            const $btn = $(this);
            $btn.prop('disabled', true);
            $('#btn-cancel-create-tenant').prop('disabled', true);
            $('#onboarding-validation-result').addClass('d-none');
            $('#onboarding-creating').removeClass('d-none');

            const tenantNameFieldKey  = $('#create-tenant-tenant-name-field').val() || null;
            const displayNameFieldKey = $('#create-tenant-display-name-field').val() || null;
            const programManagersFieldKey  = $('#create-tenant-program-managers-field').val() || null;
            const branchFieldKey      = $('#create-tenant-branch-field').val() || null;
            const featuresFieldKey    = $('#create-tenant-features-field').val() || null;
            const ministryFieldKey    = $('#create-tenant-ministry-field').val() || null;
            const divisionFieldKey    = $('#create-tenant-division-field').val() || null;
            const programAreaFieldKey = $('#create-tenant-program-area-field').val() || null;

            abp.ajax({
                url: abp.appPath + 'api/onboarding-requests/' + applicationId + '/create-tenant',
                type: 'POST',
                data: JSON.stringify({ tenantNameFieldKey, displayNameFieldKey, programManagersFieldKey, branchFieldKey, featuresFieldKey, ministryFieldKey, divisionFieldKey, programAreaFieldKey }),
                contentType: 'application/json'
            }).done(function () {
                abp.notify.success(l('OnboardingModal:CreateSuccess'));
                _createTenantModal.close();
                _dataTable.ajax.reload();
            }).fail(function () {
                $('#onboarding-creating').addClass('d-none');
                $btn.prop('disabled', false);
                $('#btn-cancel-create-tenant').prop('disabled', false);
                abp.notify.error(l('OnboardingModal:CreateFailed'));
            });
        };
    }

    function _wireCreateTenantMappingHandlers(applicationId) {
        $('#create-tenant-ministry-field').on('change', function () {
            _updateFieldPreview('create-tenant-ministry-field', 'create-tenant-ministry-value');
        });
        $('#create-tenant-division-field').on('change', function () {
            _updateFieldPreview('create-tenant-division-field', 'create-tenant-division-value');
        });
        $('#create-tenant-branch-field').on('change', function () {
            _updateFieldPreview('create-tenant-branch-field', 'create-tenant-branch-value');
        });
        $('#create-tenant-program-area-field').on('change', function () {
            _updateFieldPreview('create-tenant-program-area-field', 'create-tenant-program-area-value');
        });
        $('#create-tenant-features-field').on('change', function () {
            _updateFieldPreview('create-tenant-features-field', 'create-tenant-features-value', _buildCheckboxBadgesPreview);
        });
        $('#create-tenant-tenant-name-field').on('change', function () {
            _updateFieldPreview('create-tenant-tenant-name-field', 'create-tenant-tenant-name-value');
            _triggerValidation(applicationId);
        });
        $('#create-tenant-display-name-field').on('change', function () {
            _updateFieldPreview('create-tenant-display-name-field', 'create-tenant-display-name-value');
            _triggerValidation(applicationId);
        });
        $('#create-tenant-program-managers-field').on('change', function () {
            _updateFieldPreview('create-tenant-program-managers-field', 'create-tenant-program-managers-value', _buildProgramManagersPreview);
            _triggerValidation(applicationId);
        });
    }

    function _renderNoFieldsWarning() {
        $('#onboarding-validation-loading').addClass('d-none');
        $('#onboarding-validation-result')
            .html('<div class="alert alert-warning mb-0">' + l('CreateTenantModal:NoFieldsWarning') + '</div>')
            .removeClass('d-none');
        $('#btn-confirm-create-tenant').prop('disabled', true);
    }

    function _loadCreateTenantFields(applicationId) {
        abp.ajax({
            url: abp.appPath + 'api/onboarding-requests/column-schema',
            type: 'GET',
            data: { category: _selectedCategory }
        }).done(function (schema) {
            if (!schema?.columns?.length) {
                _renderNoFieldsWarning();
                return;
            }
            _renderMappingDropdown('create-tenant-ministry-field',    schema.columns, MINISTRY_CANONICALS,     schema.ministryFieldKey);
            _renderMappingDropdown('create-tenant-division-field',   schema.columns, DIVISION_CANONICALS,     schema.divisionFieldKey);
            _renderMappingDropdown('create-tenant-branch-field',      schema.columns, BRANCH_CANONICALS,        schema.branchFieldKey);
            _renderMappingDropdown('create-tenant-program-area-field', schema.columns, PROGRAM_AREA_CANONICALS, schema.programAreaFieldKey);
            _renderMappingDropdown('create-tenant-features-field',    schema.columns, FEATURES_CANONICALS,      schema.featuresFieldKey);
            _renderMappingDropdown('create-tenant-tenant-name-field', schema.columns, TENANT_NAME_CANONICALS,   schema.tenantNameFieldKey);
            _renderMappingDropdown('create-tenant-display-name-field', schema.columns, DISPLAY_NAME_CANONICALS, schema.displayNameFieldKey);
            _renderMappingDropdown('create-tenant-program-managers-field', schema.columns, PROGRAM_MANAGERS_CANONICALS,   schema.programManagersFieldKey);
            _updateFieldPreview('create-tenant-ministry-field',    'create-tenant-ministry-value');
            _updateFieldPreview('create-tenant-division-field',    'create-tenant-division-value');
            _updateFieldPreview('create-tenant-branch-field',      'create-tenant-branch-value');
            _updateFieldPreview('create-tenant-program-area-field', 'create-tenant-program-area-value');
            _updateFieldPreview('create-tenant-features-field',    'create-tenant-features-value', _buildCheckboxBadgesPreview);
            _updateFieldPreview('create-tenant-tenant-name-field', 'create-tenant-tenant-name-value');
            _updateFieldPreview('create-tenant-display-name-field', 'create-tenant-display-name-value');
            _updateFieldPreview('create-tenant-program-managers-field', 'create-tenant-program-managers-value', _buildProgramManagersPreview);
            $('#create-tenant-field-mapping').show();
            _wireCreateTenantMappingHandlers(applicationId);
            _triggerValidation(applicationId);
        }).fail(function () {
            _renderValidationFail();
        });
    }

    abp.modals.createTenantModal = function () {
        return {
            initModal: function (publicApi, args) {
                const applicationId = args.id;

                try {
                    const raw = document.getElementById('create-tenant-fields-json');
                    _fieldValues = raw ? JSON.parse(raw.textContent) : {};
                } catch { _fieldValues = {}; }

                _loadCreateTenantFields(applicationId);
                $('#btn-confirm-create-tenant').on('click', _onCreateTenantConfirm(applicationId));
            }
        };
    };

    // ─── Assign Owner and Assignees modal ─────────────────────────────────────

    let _assignModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'TenantManagement/Onboarding/AssignModal',
        modalClass: 'assignOnboardingModal'
    });

    let _assignRows = [];

    function _assignmentKey(row) {
        return JSON.stringify({
            ownerId: row.ownerId || null,
            assignees: (row.assignees || [])
                .map(function (a) { return a.assigneeId + '|' + (a.role || ''); })
                .sort(function (x, y) { return x.localeCompare(y); })
        });
    }

    function _appendUserOptions($select, users, placeholder) {
        $select.append($('<option>').val('').text(placeholder));
        users.forEach(function (u) {
            $select.append($('<option>').val(u.id).text(u.name));
        });
    }

    function _addAssigneeRow(users, assigneeId, role) {
        const $row = $('<div>').addClass('d-flex gap-2 mb-2 assign-onboarding-row');
        const $user = $('<select>')
            .addClass('form-select form-select-sm assign-onboarding-user')
            .attr('aria-label', l('AssignOnboardingModal:AssigneesLabel'));
        _appendUserOptions($user, users, l('AssignOnboardingModal:AssigneePlaceholder'));
        $user.val(assigneeId || '');

        const $role = $('<input>')
            .addClass('form-control form-control-sm assign-onboarding-role')
            .attr({
                type: 'text',
                maxlength: 250,
                placeholder: l('AssignOnboardingModal:RolePlaceholder'),
                'aria-label': l('AssignOnboardingModal:RolePlaceholder')
            })
            .val(role || '');

        const $remove = $('<button>')
            .addClass('btn btn-secondary btn-sm')
            .attr({ type: 'button', title: l('AssignOnboardingModal:RemoveAssignee'), 'aria-label': l('AssignOnboardingModal:RemoveAssignee') })
            .append($('<i>').addClass('fa-solid fa-xmark'))
            .on('click', function () { $row.remove(); });

        $('#assign-onboarding-assignees').append($row.append($user, $role, $remove));
    }

    function _fillAssignForm(users) {
        // Rows that already agree are prefilled so they can be edited; rows that disagree start
        // blank, with a warning, because saving overwrites all of them with the same values.
        const isSame = new Set(_assignRows.map(_assignmentKey)).size === 1;
        const current = isSame ? _assignRows[0] : { ownerId: null, assignees: [] };

        _appendUserOptions($('#assign-onboarding-owner'), users, l('AssignOnboardingModal:NoOwner'));
        $('#assign-onboarding-owner').val(current.ownerId || '');
        $('#assign-onboarding-mixed').toggleClass('d-none', isSame);

        (current.assignees || []).forEach(function (a) { _addAssigneeRow(users, a.assigneeId, a.role); });
        if (!current.assignees?.length) _addAssigneeRow(users);

        $('#btn-assign-onboarding-add').on('click', function () { _addAssigneeRow(users); });
        $('#assign-onboarding-loading').addClass('d-none');
        $('#assign-onboarding-form').removeClass('d-none');
        $('#btn-assign-onboarding-save').prop('disabled', false);
    }

    // Returns the assignees to save, or null after showing why they can't be saved. A row left
    // completely blank is ignored, so an owner can be saved without any assignee.
    function _readAssignees() {
        const assignees = [];
        let error = null;

        $('#assign-onboarding-assignees .assign-onboarding-row').each(function () {
            const assigneeId = $(this).find('.assign-onboarding-user').val();
            const role = String($(this).find('.assign-onboarding-role').val() || '').trim();

            if (!assigneeId) {
                if (role) error = 'AssignOnboardingModal:AssigneeRequired';
                return;
            }
            if (assignees.some(function (a) { return a.assigneeId === assigneeId; })) {
                error = error || 'AssignOnboardingModal:DuplicateAssignee';
                return;
            }
            assignees.push({ assigneeId: assigneeId, role: role || null });
        });

        $('#assign-onboarding-error').text(error ? l(error) : '').toggleClass('d-none', !error);
        return error ? null : assignees;
    }

    function _saveAssignment() {
        const assignees = _readAssignees();
        if (assignees === null) return;

        const $btn = $(this);
        $btn.prop('disabled', true);

        abp.ajax({
            url: abp.appPath + 'api/onboarding-requests/assign',
            type: 'POST',
            data: JSON.stringify({
                applicationIds: _assignRows.map(function (r) { return r.id; }),
                ownerId: $('#assign-onboarding-owner').val() || null,
                assignees: assignees
            }),
            contentType: 'application/json'
        }).done(function () {
            abp.notify.success(l('AssignOnboardingModal:SaveSuccess'));
            _assignModal.close();
            _dataTable.ajax.reload(null, false);
        }).fail(function () {
            $btn.prop('disabled', false);
        });
    }

    abp.modals.assignOnboardingModal = function () {
        return {
            initModal: function () {
                $('#btn-assign-onboarding-save').on('click', _saveAssignment);

                abp.ajax({
                    url: abp.appPath + 'api/onboarding-requests/assignable-users',
                    type: 'GET'
                }).done(_fillAssignForm).fail(function () {
                    $('#assign-onboarding-loading').addClass('d-none');
                    $('#assign-onboarding-error').text(l('AssignOnboardingModal:LoadFailed')).removeClass('d-none');
                });
            }
        };
    };

    // ─── Jaro-Winkler field auto-detection ───────────────────────────────────

    function _normalizeLabel(s) {
        return s
            .replaceAll(/([a-z])([A-Z])/g, '$1 $2')
            .replaceAll(/[_-]+/g, ' ')
            .toLowerCase()
            .trim();
    }

    function _computeJaroMatchings(s1, s2, matchDist) {
        const l1 = s1.length, l2 = s2.length;
        const s1m = new Array(l1).fill(false), s2m = new Array(l2).fill(false);
        let matches = 0;
        for (let i = 0; i < l1; i++) {
            const lo = Math.max(0, i - matchDist), hi = Math.min(i + matchDist + 1, l2);
            for (let j = lo; j < hi; j++) {
                if (s2m[j] || s1[i] !== s2[j]) continue;
                s1m[i] = s2m[j] = true; matches++; break;
            }
        }
        return { s1m, s2m, matches };
    }

    function _computeJaroTranspositions(s1, s2, s1m, s2m) {
        let k = 0, transpositions = 0;
        for (let i = 0; i < s1.length; i++) {
            if (!s1m[i]) continue;
            while (!s2m[k]) k++;
            if (s1[i] !== s2[k]) transpositions++;
            k++;
        }
        return transpositions;
    }

    function _jaroWinkler(s1, s2) {
        if (s1 === s2) return 1;
        const l1 = s1.length, l2 = s2.length;
        if (!l1 || !l2) return 0;
        const matchDist = Math.max(Math.floor(Math.max(l1, l2) / 2) - 1, 0);
        const { s1m, s2m, matches } = _computeJaroMatchings(s1, s2, matchDist);
        if (!matches) return 0;
        const transpositions = _computeJaroTranspositions(s1, s2, s1m, s2m);
        const jaro = (matches / l1 + matches / l2 + (matches - transpositions / 2) / matches) / 3;
        let prefix = 0;
        for (let p = 0; p < Math.min(4, l1, l2); p++) {
            if (s1[p] === s2[p]) prefix++; else break;
        }
        return jaro + prefix * 0.1 * (1 - jaro);
    }

    const TENANT_NAME_CANONICALS  = ['name', 'tenant name', 'organization name', 'company name', 'program name', 'applicant name', 'tenant abbreviation'];
    const DISPLAY_NAME_CANONICALS = ['display name', 'tenant display name', 'organization display name', 'public name'];
    const PROGRAM_MANAGERS_CANONICALS  = ['super user', 'super users', 'admin email', 'program manager', 'manager email', 'administrator', 'user email'];
    const MINISTRY_CANONICALS     = ['ministry', 'ministry name', 'government ministry', 'responsible ministry'];
    const DIVISION_CANONICALS     = ['division', 'ministry division', 'division name', 'responsible division'];
    const BRANCH_CANONICALS       = ['branch', 'division branch', 'ministry branch', 'business branch'];
    const PROGRAM_AREA_CANONICALS = ['program area', 'program area name', 'program name', 'program'];
    const FEATURES_CANONICALS     = ['features', 'feature flags', 'program features', 'modules', 'enabled features', 'features to be enabled'];
    const MATCH_THRESHOLD = 0.85;

    // Aliases listed for more than one mapping target (e.g. 'program name' is both a Tenant Name
    // and a Program Area alias). They are too ambiguous to pick an empty field on label alone.
    const SHARED_CANONICALS = (function () {
        const seen = new Set(), shared = new Set();
        [TENANT_NAME_CANONICALS, DISPLAY_NAME_CANONICALS, PROGRAM_MANAGERS_CANONICALS, MINISTRY_CANONICALS,
            DIVISION_CANONICALS, BRANCH_CANONICALS, PROGRAM_AREA_CANONICALS, FEATURES_CANONICALS]
            .forEach(function (list) {
                new Set(list).forEach(function (c) { (seen.has(c) ? shared : seen).add(c); });
            });
        return shared;
    })();

    // A field whose normalized label exactly equals one of the canonicals is an unambiguous
    // match, so it wins outright without fuzzy scoring. This avoids e.g. "ProgramManagerEmail"
    // (which shares a "program " prefix with the "program name" canonical, and so scores highly
    // under Jaro-Winkler's prefix bonus) out-scoring a field that's literally labeled "Name" —
    // "name" only appears as a *suffix* of every Tenant Name canonical, so it gets no prefix
    // bonus and loses on fuzzy score alone.
    function _exactMatch(fields, canonicals) {
        const exact = fields.find(function (f) { return canonicals.includes(_normalizeLabel(f.label || f.key)); });
        return exact ? exact.key : null;
    }

    function _bestMatch(fields, canonicals) {
        const exact = _exactMatch(fields, canonicals);
        if (exact) return exact;

        let best = null, bestScore = 0;
        fields.forEach(function (f) {
            const norm = _normalizeLabel(f.label || f.key);
            const score = Math.max(...canonicals.map(function (c) { return _jaroWinkler(norm, c); }));
            if (score > bestScore) { bestScore = score; best = f.key; }
        });
        return bestScore >= MATCH_THRESHOLD ? best : null;
    }

    function _renderMappingDropdown(selectId, fields, canonicals, savedKey) {
        const $sel = $('#' + selectId);
        $sel.find('option:not(:first)').remove();
        fields.forEach(function (f) {
            $sel.append('<option value="' + $('<span>').text(f.key).html() + '">' + $('<span>').text(f.label).html() + '</option>');
        });
        const savedKeyValid = savedKey && fields.some(function (f) { return f.key === savedKey; });
        const fieldsWithValue = fields.filter(function (f) { return _hasFieldValue(f.key); });
        const pick = (savedKeyValid && _hasFieldValue(savedKey) ? savedKey : null)
            || _exactMatch(fieldsWithValue, canonicals)
            || _exactMatch(fields, canonicals.filter(function (c) { return !SHARED_CANONICALS.has(c); }))
            || _bestMatch(fieldsWithValue, canonicals)
            || (savedKeyValid ? savedKey : null);
        if (pick) $sel.val(pick);
    }

    function _hasFieldValue(key) {
        const value = _fieldValues[key];
        if (value === null || value === undefined) return false;
        if (Array.isArray(value)) return value.length > 0;
        const text = String(value).trim();
        return text !== '' && text !== '[]' && text !== '{}';
    }

    // ─── Document ready ───────────────────────────────────────────────────────

    function _loadSchemaAndInitTable() {
        abp.ajax({
            url: abp.appPath + 'api/onboarding-requests/column-schema',
            type: 'GET',
            data: { category: _selectedCategory }
        }).done(function (schema) {
            initTable(schema);
        }).fail(function () {
            initTable(null);
        });
    }

    $(function () {
        abp.ajax({
            url: abp.appPath + 'api/onboarding-requests/categories',
            type: 'GET'
        }).done(function (categories) {
            const categoryList = categories || ['Onboarding'];
            const $sel = $('#onboarding-category-filter');
            $sel.empty();
            categoryList.forEach(function (cat) {
                $sel.append('<option value="' + _escapeHtml(cat) + '">' + _escapeHtml(cat) + '</option>');
            });

            const savedCategory = localStorage.getItem(CATEGORY_STORAGE_KEY);
            _selectedCategory = (savedCategory && categoryList.includes(savedCategory)) ? savedCategory : 'Onboarding';
            $sel.val(_selectedCategory);
        }).always(function () {
            _loadSchemaAndInitTable();
        });

        $('#onboarding-category-filter').on('change', function () {
            _selectedCategory = $(this).val() || 'Onboarding';
            localStorage.setItem(CATEGORY_STORAGE_KEY, _selectedCategory);
            _loadSchemaAndInitTable();
        });

        $('#btn-open-onboarding').on('click', function () {
            if (!_selectedRow) return;
            globalThis.location.href = '/GrantApplications/Details?ApplicationId=' + _selectedRow.id;
        });

        $('#btn-create-tenant').on('click', function () {
            if (!_selectedRow) return;
            _createTenantModal.open({ id: _selectedRow.id });
        });

        $('#btn-assign-onboarding').on('click', function () {
            if (_selectedRows.length === 0) return;
            _assignRows = _selectedRows;
            _assignModal.open();
        });
    });
})();
