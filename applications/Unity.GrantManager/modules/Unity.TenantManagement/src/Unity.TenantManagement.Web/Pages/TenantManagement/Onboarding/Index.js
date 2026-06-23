(function () {
    let l = abp.localization.getResource('GrantManager');
    let _onboardingRequestAppService = unity.tenantManagement.onboardingRequest;
    let _dataTable = null;
    let _selectedRow = null;
    let _selectedCategory = 'Onboarding';

    const CATEGORY_STORAGE_KEY = 'Onboarding_SelectedCategory';

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
        { title: l('Onboarding:ColumnCategory'), data: 'category', name: 'category', index: 3 }
    ];

    // ─── Field value renderers ────────────────────────────────────────────────

    function _escapeHtml(str) {
        return String(str)
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;');
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

    // ─── Super Users DataGrid email extraction ───────────────────────────────

    // Formio/CHEFS "Super Users" fields are submitted as a DataGrid: one row per super
    // user, with columns such as name/email/title. The email column's key varies per
    // worksheet (e.g. "s03_SuperUserEmail"), so it's matched by name rather than a fixed
    // key — mirrors SuperUsersValidationStep.ParseEmails on the server.
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

    function _buildSuperUsersPreview(data) {
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
        return '<button type="button" class="btn btn-icon btn-sm onboarding-datagrid-btn" data-datagrid-key="' + cacheKey + '" title="' + l('Onboarding:ViewDataGrid') + '"><i class="fl fl-datagrid"></i></button>';
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

    function buildColumns(schema) {
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

        return cols;
    }

    // ─── DataTable init ───────────────────────────────────────────────────────

    function initTable(schema) {
        if (_dataTable) {
            _dataTable.off('select deselect');
            _dataTable.destroy();
            $('#OnboardingRequestsTable').empty();
            _dataTable = null;
            _selectedRow = null;
            manageActionButtons();
        }

        let listColumns = buildColumns(schema);

        _dataTable = initializeDataTable({
            dt: $('#OnboardingRequestsTable'),
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
            actionButtons: commonTableActionButtons(l('Menu:Onboarding')).filter(b => b.id !== 'btn-toggle-filter'),
            serverSideEnabled: true,
            pagingEnabled: true,
            reorderEnabled: true,
            languageSetValues: {},
            dynamicButtonContainerId: 'dynamicButtonContainerId',
            externalSearchId: 'search',
            lengthMenu: [10, 25, 50]
        });

        _dataTable.select.style('single');

        _dataTable.on('select', function (e, dt, type, indexes) {
            if (type === 'row' && indexes.length) {
                _selectedRow = dt.row(indexes[0]).data();
                manageActionButtons();
            }
        });

        _dataTable.on('deselect', function (e, dt, type) {
            if (type === 'row' && dt.rows({ selected: true }).count() === 0) {
                _selectedRow = null;
                manageActionButtons();
            }
        });
    }

    // ─── Action button state ──────────────────────────────────────────────────

    function manageActionButtons() {
        const hasSelection = _selectedRow !== null;
        $('#btn-open-onboarding').toggleClass('action-bar-btn-unavailable', !hasSelection);
        $('#btn-create-tenant').toggleClass('action-bar-btn-unavailable',
            !hasSelection || _selectedRow.status !== 'Approved');
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
                .html('<div class="alert alert-success mb-0"><i class="fl fl-check me-1"></i> ' + l('OnboardingModal:ValidationPassed') + '</div>')
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
        const superUsersFieldKey  = $('#create-tenant-super-users-field').val() || null;
        const branchFieldKey      = $('#create-tenant-branch-field').val() || null;
        const featuresFieldKey    = $('#create-tenant-features-field').val() || null;
        const ministryFieldKey    = $('#create-tenant-ministry-field').val() || null;
        const programAreaFieldKey = $('#create-tenant-program-area-field').val() || null;

        const params = new URLSearchParams();
        if (tenantNameFieldKey)  params.append('tenantNameFieldKey',  tenantNameFieldKey);
        if (superUsersFieldKey)  params.append('superUsersFieldKey',  superUsersFieldKey);
        if (branchFieldKey)      params.append('branchFieldKey',      branchFieldKey);
        if (featuresFieldKey)    params.append('featuresFieldKey',    featuresFieldKey);
        if (ministryFieldKey)    params.append('ministryFieldKey',    ministryFieldKey);
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
            const superUsersFieldKey  = $('#create-tenant-super-users-field').val() || null;
            const branchFieldKey      = $('#create-tenant-branch-field').val() || null;
            const featuresFieldKey    = $('#create-tenant-features-field').val() || null;
            const ministryFieldKey    = $('#create-tenant-ministry-field').val() || null;
            const programAreaFieldKey = $('#create-tenant-program-area-field').val() || null;

            abp.ajax({
                url: abp.appPath + 'api/onboarding-requests/' + applicationId + '/create-tenant',
                type: 'POST',
                data: JSON.stringify({ tenantNameFieldKey, superUsersFieldKey, branchFieldKey, featuresFieldKey, ministryFieldKey, programAreaFieldKey }),
                contentType: 'application/json'
            }).done(function () {
                abp.notify.success(l('OnboardingModal:CreateSuccess'));
                _createTenantModal.close();
                _dataTable.ajax.reloadEx();
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
        $('#create-tenant-super-users-field').on('change', function () {
            _updateFieldPreview('create-tenant-super-users-field', 'create-tenant-super-users-value', _buildSuperUsersPreview);
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
            _renderMappingDropdown('create-tenant-branch-field',      schema.columns, BRANCH_CANONICALS,        schema.branchFieldKey);
            _renderMappingDropdown('create-tenant-program-area-field', schema.columns, PROGRAM_AREA_CANONICALS, schema.programAreaFieldKey);
            _renderMappingDropdown('create-tenant-features-field',    schema.columns, FEATURES_CANONICALS,      schema.featuresFieldKey);
            _renderMappingDropdown('create-tenant-tenant-name-field', schema.columns, TENANT_NAME_CANONICALS,   schema.tenantNameFieldKey);
            _renderMappingDropdown('create-tenant-super-users-field', schema.columns, SUPER_USERS_CANONICALS,   schema.superUsersFieldKey);
            _updateFieldPreview('create-tenant-ministry-field',    'create-tenant-ministry-value');
            _updateFieldPreview('create-tenant-branch-field',      'create-tenant-branch-value');
            _updateFieldPreview('create-tenant-program-area-field', 'create-tenant-program-area-value');
            _updateFieldPreview('create-tenant-features-field',    'create-tenant-features-value', _buildCheckboxBadgesPreview);
            _updateFieldPreview('create-tenant-tenant-name-field', 'create-tenant-tenant-name-value');
            _updateFieldPreview('create-tenant-super-users-field', 'create-tenant-super-users-value', _buildSuperUsersPreview);
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

    const TENANT_NAME_CANONICALS  = ['tenant name', 'organization name', 'company name', 'program name', 'applicant name', 'tenant abbreviation'];
    const SUPER_USERS_CANONICALS  = ['super user', 'super users', 'admin email', 'program manager', 'manager email', 'administrator', 'user email'];
    const MINISTRY_CANONICALS     = ['ministry', 'ministry name', 'government ministry', 'responsible ministry'];
    const BRANCH_CANONICALS       = ['branch', 'division branch', 'ministry branch', 'business branch'];
    const PROGRAM_AREA_CANONICALS = ['program area', 'program area name', 'program name', 'program'];
    const FEATURES_CANONICALS     = ['features', 'feature flags', 'program features', 'modules', 'enabled features', 'features to be enabled'];
    const MATCH_THRESHOLD = 0.85;

    function _bestMatch(fields, canonicals) {
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
        const pick = (savedKeyValid ? savedKey : null) || _bestMatch(fields, canonicals);
        if (pick) $sel.val(pick);
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
    });
})();
