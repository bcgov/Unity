(function () {
    let l = abp.localization.getResource('AbpTenantManagement');
    let lGm = abp.localization.getResource('GrantManager');
    let _tenantAppService = unity.tenantManagement.tenant;
    let _userImportService = unity.grantManager.identity.userImport;
    let _casClientCodeHash = {};

    let _createModal = new abp.ModalManager({
            viewUrl: abp.appPath + 'TenantManagement/Tenants/CreateModal',
            modalClass: 'createTenant'
        }
    );

    let _configurationModal = new abp.ModalManager({
            viewUrl: abp.appPath + 'TenantManagement/Tenants/ConfigurationModal',
            modalClass: 'configurationModal'
        }
    );

    let _reportingDatabaseInfoModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'ReportingAdmin/Configuration/DatabaseInfoModal'
    });

    let _dataTable = null;

    // ─── Actions column renderer ──────────────────────────────────────────────

    function _buildActionsCell(id, name) {
        let items = [];
        if (abp.auth.isGranted('UnityTenantManagement.Tenants.Update') || abp.auth.isGranted('ITOperations')) {
            items.push('<a href="javascript:;" class="dropdown-item tenant-action-config" data-id="' + id + '">' + lGm('TenantList:ConfigurationAction') + '</a>');
        }
        if (abp.auth.isGranted('UnityTenantManagement.Tenants.Delete')) {
            items.push('<a href="javascript:;" class="dropdown-item tenant-action-delete" data-id="' + id + '" data-name="' + $('<span>').text(name || '').html() + '">' + l('Delete') + '</a>');
        }
        if (!items.length) return '';
        return '<div class="text-center"><div class="dropdown d-inline-block">' +
            '<a href="javascript:;" class="btn btn-primary btn-sm dropdown-toggle" data-bs-toggle="dropdown" aria-expanded="false">' +
            '<i class="fa-solid fa-gear"></i> ' + lGm('TenantList:ActionsButton') + '</a>' +
            '<div class="dropdown-menu">' + items.join('') + '</div>' +
            '</div></div>';
    }

    // ─── Column definitions ───────────────────────────────────────────────────

    let listColumns = [
        {
            title: l('Actions'),
            name: 'actions',
            data: 'id',
            orderable: false,
            className: 'notexport text-center',
            index: 0,
            render: function (data, type, row) {
                return type === 'display' ? _buildActionsCell(data, row.name) : '';
            }
        },
        { title: l('TenantName'),  data: 'name',         name: 'name',         index: 1 },
        { title: lGm('TenantList:DisplayName'), data: 'displayName', name: 'displayName', index: 2 },
        { title: lGm('TenantList:LicencePlate'),  data: 'licencePlate', name: 'licencePlate', index: 3 },
        { title: l('Division'),    data: 'division',     name: 'division',     index: 4 },
        { title: l('Branch'),      data: 'branch',       name: 'branch',       index: 5 },
        { title: l('Description'), data: 'description',  name: 'description',  index: 6 },
        {
            title: lGm('TenantList:CasClientCode'),
            data: 'casClientCode',
            name: 'casClientCode',
            index: 7,
            render: function (data, type, row) {
                if (type === 'display') {
                    return _casClientCodeHash[row.casClientCode || ''] || '';
                }
                return data;
            }
        },
        {
            // Small info icon next to the header, hinting that the status icon in this column is
            // clickable - a native title attribute (not a Bootstrap tooltip) so it works without
            // needing to re-run ABP's tooltip auto-init after every DataTable redraw.
            title: lGm('TenantList:PostCreationStatus') +
                ' <i class="fa-regular fa-circle-question text-muted" title="' +
                $('<span>').text(lGm('TenantList:PostCreationStatus:HeaderHint')).html() + '"></i>',
            data: 'sections',
            name: 'sections',
            orderable: false,
            className: 'text-center',
            index: 8,
            render: function (data, type) {
                if (type !== 'display') {
                    return data;
                }
                return _renderPostCreationStatusIcon(data);
            }
        },
        { title: l('Id'), data: 'id', name: 'id', index: 9 }
    ];

    let defaultVisibleColumns = ['actions', 'name', 'displayName', 'licencePlate', 'division', 'branch', 'description', 'casClientCode', 'sections'];

    // ─── Post-creation status column (e.g. Metabase sync) ─────────────────────

    let _postCreationSummaryByStatus = {
        Waiting: { icon: 'fa-clock', cssClass: 'text-secondary' },
        Success: { icon: 'fa-circle-check', cssClass: 'text-success' },
        Partial: { icon: 'fa-triangle-exclamation', cssClass: 'text-warning' },
        Failure: { icon: 'fa-circle-xmark', cssClass: 'text-danger' }
    };

    function _parsePostCreationSections(json) {
        try {
            let sections = JSON.parse(json || '[]');
            return Array.isArray(sections) ? sections : [];
        } catch (e) {
            // Malformed/unexpected sections JSON is treated the same as "no post-creation steps
            // recorded" (empty array) - the row already renders that case as no status icon at
            // all (see _renderPostCreationStatusIcon), so there's nothing actionable to surface
            // to the user here.
            return [];
        }
    }

    // Rolls up the per-step statuses into one of Waiting / Success / Partial / Failure:
    // Success - every step succeeded; Failure - nothing succeeded and at least one step
    // failed; Partial - a mix of succeeded and failed steps; Waiting - otherwise (nothing has
    // failed yet, but not every step has finished either).
    function _summarizePostCreationStatus(sections) {
        if (!sections.length) {
            return 'Waiting';
        }
        let successCount = sections.filter(function (s) { return s.status === 'Success'; }).length;
        let failedCount = sections.filter(function (s) { return s.status === 'Error' || s.status === 'Failure'; }).length;
        let waitingCount = sections.length - successCount - failedCount;

        if (failedCount === 0 && waitingCount === 0) {
            return 'Success';
        }
        if (successCount === 0 && failedCount > 0) {
            return 'Failure';
        }
        if (successCount > 0 && failedCount > 0) {
            return 'Partial';
        }
        return 'Waiting';
    }

    function _renderPostCreationStatusIcon(json) {
        let sections = _parsePostCreationSections(json);

        // No tracked steps at all means this is a legacy tenant that predates post-creation step
        // tracking (created before this feature shipped) - it will never get seeded/updated, so
        // showing a permanent "Waiting" icon for it would be misleading. Only tenants created
        // going forward (seeded at creation time - see SeedPostTenantCreationSections) have any
        // sections, so an empty list here is the legacy case, not a "still waiting" case.
        if (!sections.length) {
            return '';
        }

        let status = _summarizePostCreationStatus(sections);
        let summary = _postCreationSummaryByStatus[status] || _postCreationSummaryByStatus.Waiting;
        let statusText = lGm('TenantList:PostCreationStatus:' + status);
        let title = $('<span>').text(statusText + ' - ' + lGm('TenantList:PostCreationStatus:ClickForDetails')).html();

        // The per-step detail (name/status/message/timestamp) is read back from the DataTable's
        // own row data on click (see the delegated click handler below) rather than embedded here
        // as a data-* attribute - the sections JSON contains double quotes, which break a
        // double-quoted HTML attribute unless escaped for attribute context specifically (`.text()`
        // + `.html()` only escapes for element *content*, not attribute values).
        return '<a href="javascript:;" class="post-creation-status-icon" title="' + title + '">' +
            '<i class="fa-solid ' + summary.icon + ' ' + summary.cssClass + '" style="font-size: 1.1rem;"></i>' +
            '</a>';
    }

    // abp.message.info in this app falls back to a plain browser alert() (see abp.jquery.js),
    // which can't render markup - HTML passed to it just shows up as literal tag text. Use
    // SweetAlert2 directly instead (already used elsewhere in this codebase, e.g.
    // AssessmentScoresWidget/Default.js, for the same reason: it supports an `html` option).
    function _showPostCreationSectionsDetail(sections) {
        if (!sections.length) {
            Swal.fire({
                title: lGm('TenantList:PostCreationStatus'),
                text: lGm('TenantList:PostCreationStatus:NoSteps'),
                confirmButtonText: 'OK',
                customClass: { confirmButton: 'btn btn-primary' }
            });
            return;
        }

        let html = '<div class="text-start">' + sections.map(function (section) {
            let statusKey = section.status || 'Waiting';
            let statusText = lGm('TenantList:PostCreationStatus:' + statusKey);
            let updatedAtText = section.updatedAt ? new Date(section.updatedAt).toLocaleString() : '';
            return '<div class="mb-2">' +
                '<strong>' + $('<span>').text(section.name || section.key || '').html() + '</strong>: ' +
                $('<span>').text(statusText).html() +
                (section.message ? '<div class="text-muted small">' + $('<span>').text(section.message).html() + '</div>' : '') +
                (updatedAtText ? '<div class="text-muted small">' + $('<span>').text(updatedAtText).html() + '</div>' : '') +
                '</div>';
        }).join('') + '</div>';

        Swal.fire({
            title: lGm('TenantList:PostCreationStatus'),
            html: html,
            width: '32rem',
            confirmButtonText: 'OK',
            customClass: { confirmButton: 'btn btn-primary' }
        });
    }

    let responseCallback = function (result) {
        return {
            recordsTotal: result.totalCount,
            recordsFiltered: result.totalCount,
            data: result.items
        };
    };

    // ─── Modal setup: Create tenant ───────────────────────────────────────────

    let _filterDataTable = null;
    let _configFilterDataTable = null;
    let _createFeaturesLoaded = false;
    let _createFeatureProviderKey = null;

    function _searchFieldInputAction(fieldSelectId, valueInputId) {
        let field = $('#' + fieldSelectId).val();
        let value = $('#' + valueInputId).val();
        if (field === 'firstAndLast') {
            let parts = value.trim().replaceAll(/\s+/g, ' ').split(' ');
            return {
                directory: 'IDIR',
                firstName: parts[0] || '',
                lastName: parts[1] || '',
                email: ''
            };
        }
        return {
            directory: 'IDIR',
            firstName: field === 'firstName' ? value : '',
            lastName: field === 'lastName' ? value : '',
            email: field === 'email' ? value : ''
        };
    }

    function _searchResponseCallback(result) {
        return { recordsTotal: result.length, recordsFiltered: result.length, data: result };
    }

    let setupCreateTenantModal = function () {
        let _$filterTable = $('#UserSearchTable');
        _filterDataTable = _$filterTable.DataTable(
            abp.libs.datatables.normalizeConfiguration(
                {
                    order: [[0, 'asc']],
                    processing: true,
                    serverSide: false,
                    scrollX: true,
                    paging: true,
                    searching: false,
                    ajax: abp.libs.datatables.createAjax(
                        _userImportService.search,
                        function () { return _searchFieldInputAction('create-search-field', 'create-search-value'); },
                        _searchResponseCallback
                    ),
                    select: {
                        style: 'single',
                    },
                    columnDefs: [{
                        title: 'First Name',
                        name: 'firstName',
                        data: 'firstName',
                        className: 'data-table-header',
                    },
                    {
                        title: 'Last Name',
                        name: 'lastName',
                        data: 'lastName',
                        className: 'data-table-header'
                    },
                    {
                        title: 'Display Name',
                        name: 'displayName',
                        data: 'displayName',
                        className: 'data-table-header'
                    },
                    {
                        title: 'Email',
                        name: 'email',
                        data: 'email',
                        className: 'data-table-header'
                    }],
                })
        );

        $('#create-search-field').on('change', function () {
            let placeholders = {
                firstName: 'At least 2 characters...',
                lastName: 'At least 2 characters...',
                firstAndLast: 'e.g. John Smith',
                email: 'At least 2 characters...'
            };
            $('#create-search-value').val('').attr('placeholder', placeholders[$(this).val()] || 'At least 2 characters...');
        });

        $('#TenantAdminSearchButton').click(function (e) {
            e.preventDefault();
            if ($('#create-search-value').val().trim().length < 2) {
                abp.notify.warn(lGm('TenantList:SearchMinChars'));
                return;
            }
            _filterDataTable.ajax.reload();
            $('#create-tenant-admin-id').val('');
            $('#create-selected-user-display').hide();
            $('#create-tenant-btn').attr('disabled', true);
        });

        $('#cancel-tenant-btn').click(function (e) {
            _createModal.close();
        });

        _filterDataTable.on('select', function (e, dt, type, indexes) {
            if (type === 'row') {
                let selectedData = _filterDataTable.row(indexes).data();
                $('#create-tenant-admin-id').val(selectedData.userGuid);
                let displayName = selectedData.displayName || (selectedData.firstName + ' ' + selectedData.lastName).trim();
                $('#create-selected-user-name').text(displayName);
                $('#create-selected-user-display').show();
                $('#create-tenant-btn').removeAttr('disabled');
            }
        });

        _filterDataTable.on('deselect', function () {
            $('#create-tenant-admin-id').val('');
            $('#create-selected-user-display').hide();
            $('#create-tenant-btn').attr('disabled', true);
        });
    };

    _createModal.onOpen(function () {
        setTimeout(() => {
            _filterDataTable.columns.adjust().draw();
        });
    });

    _configurationModal.onOpen(function () {
        setTimeout(() => {
            if (_configFilterDataTable) {
                _configFilterDataTable.columns.adjust().draw();
            }
        });
    });

    function _createTenantInitModal(publicApi, args) {
        setupCreateTenantModal();

        _createFeaturesLoaded = false;
        _createFeatureProviderKey = _generateGuid();
        $('#create-tab-features').on('shown.bs.tab', function () {
            if (!_createFeaturesLoaded) {
                _createFeaturesLoaded = true;
                _loadCreateFeaturesTab();
            }
        });
        $('#create-features-content').on('change', '[data-feature-group="Specializations"] input[type="checkbox"]', _specializationCheckboxChange);
        $('#create-features-content').on('change', 'input[type="checkbox"]', _captureCreateFeaturesToForm);

        _metabaseNewlyAddedEmails = [];
        _metabaseRemovedDefaultEmails = [];
        _captureMetabaseUsersToForm();
        $('#metabase-user-list').on('change', '.metabase-user-checkbox', _captureMetabaseUsersToForm);
        $('#metabase-save-as-default').on('change', _captureMetabaseUsersToForm);
        $('#metabase-add-user-btn').on('click', function (e) {
            e.preventDefault();
            _addMetabaseUser($('#metabase-new-user-email').val());
            $('#metabase-new-user-email').val('');
        });
        $('#metabase-new-user-email').on('keypress', function (e) {
            if (e.which === 13) {
                e.preventDefault();
                $('#metabase-add-user-btn').click();
            }
        });
        $('#metabase-user-list').on('click', '.metabase-remove-default-btn', function (e) {
            e.preventDefault();
            _metabaseRemovedDefaultEmails.push($(this).data('email'));
            $(this).closest('.form-check').remove();
            _captureMetabaseUsersToForm();
        });

        $('#create-pane-features').closest('form').on('invalid-form.validate', function (e, validator) {
            if (validator.errorList.length > 0) {
                let $firstErrorPane = $(validator.errorList[0].element).closest('.tab-pane');
                if ($firstErrorPane.length) {
                    $('[data-bs-target="#' + $firstErrorPane.attr('id') + '"]').tab('show');
                }
            }
        });
    }

    // ─── Metabase tab: user list ───────────────────────────────────────────────

    let _metabaseNewlyAddedEmails = [];
    let _metabaseRemovedDefaultEmails = [];

    function _captureMetabaseUsersToForm() {
        let checked = [];
        $('#metabase-user-list .metabase-user-checkbox:checked').each(function () {
            checked.push($(this).val());
        });
        $('#metabase-user-emails').val(checked.join(','));
        $('#metabase-removed-default-user-emails').val(_metabaseRemovedDefaultEmails.join(','));

        if ($('#metabase-save-as-default').prop('checked')) {
            let newDefaults = _metabaseNewlyAddedEmails.filter(function (email) {
                return checked.includes(email);
            });
            $('#metabase-new-default-user-emails').val(newDefaults.join(','));
        } else {
            $('#metabase-new-default-user-emails').val('');
        }
    }

    function _addMetabaseUser(email) {
        email = (email || '').trim();
        if (!email) return;

        let exists = $('#metabase-user-list .metabase-user-checkbox').toArray().some(function (el) {
            return $(el).val().toLowerCase() === email.toLowerCase();
        });
        if (exists) {
            abp.notify.warn('That user is already in the list.');
            return;
        }

        let id = 'metabase-user-' + $('#metabase-user-list .metabase-user-checkbox').length + '-' + Date.now();
        let $checkbox = $('<input class="form-check-input metabase-user-checkbox" type="checkbox" checked>')
            .attr('id', id).val(email);
        let $label = $('<label class="form-check-label"></label>').attr('for', id).text(email);
        $('<div class="form-check"></div>').append($checkbox).append($label).appendTo('#metabase-user-list');

        _metabaseNewlyAddedEmails.push(email);
        _captureMetabaseUsersToForm();
    }

    abp.modals.createTenant = function () {
        return { initModal: _createTenantInitModal };
    };

    // ─── Modal setup: Configuration ───────────────────────────────────────────

    let _configTenantId = null;
    let _featuresLoaded = false;

    function _generateGuid() {
        if (globalThis.crypto?.randomUUID) return globalThis.crypto.randomUUID();

        // Fallback for environments without crypto.randomUUID - still CSPRNG-backed via
        // crypto.getRandomValues, not Math.random(), since this key is used as a cache-busting
        // provider key sent to the server, not truly security-sensitive, but there's no reason
        // to reach for a weaker PRNG when getRandomValues is universally available.
        const bytes = new Uint8Array(16);
        globalThis.crypto.getRandomValues(bytes);
        bytes[6] = (bytes[6] & 0x0f) | 0x40;
        bytes[8] = (bytes[8] & 0x3f) | 0x80;
        const hex = Array.from(bytes, function (b) { return b.toString(16).padStart(2, '0'); }).join('');
        return hex.slice(0, 8) + '-' + hex.slice(8, 12) + '-' + hex.slice(12, 16) + '-' + hex.slice(16, 20) + '-' + hex.slice(20);
    }

    function _renderFeatureItem(feature) {
        let id = 'ft-' + feature.name.replaceAll('.', '-');
        let checked = (feature.value || '').toLowerCase() === 'true' ? ' checked' : '';
        return '<div class="form-check form-switch mb-2">' +
            '<input class="form-check-input" type="checkbox" id="' + id + '"' +
            ' data-feature-name="' + feature.name + '"' + checked + '>' +
            '<label class="form-check-label" for="' + id + '">' +
            (feature.displayName || feature.name) + '</label>' +
            '</div>';
    }

    function _renderFeatureGroups(groups) {
        if (!groups?.length) {
            return '<p class="text-muted">No features available.</p>';
        }
        let html = '';
        groups.forEach(function (group) {
            html += '<div class="mb-4" data-feature-group="' + group.name + '">';
            html += '<h6 class="mb-2 text-secondary">' + (group.displayName || group.name) + '</h6>';
            group.features?.forEach(function (feature) {
                html += _renderFeatureItem(feature);
            });
            html += '</div>';
        });
        return html;
    }

    function _loadManagersTab(tenantId) {
        $('#config-managers-loading').show();
        $('#config-managers-content').html('');

        abp.ajax({
            url: abp.appPath + 'api/multi-tenancy/tenants/' + tenantId + '/managers',
            type: 'GET'
        }).done(function (result) {
            $('#config-managers-loading').hide();
            $('#config-managers-count').text(result?.length ?? 0);
            if (result?.length) {
                let html = '<ul class="list-unstyled mb-0">';
                result.forEach(function (m) {
                    html += '<li class="d-flex align-items-center py-1">' +
                        '<i class="fl fl-user me-2 text-muted"></i>' +
                        '<span>' + $('<span>').text(m.displayName).html() + '</span>' +
                        (m.email ? '<span class="text-muted small ms-2">(' + $('<span>').text(m.email).html() + ')</span>' : '') +
                        '</li>';
                });
                html += '</ul>';
                $('#config-managers-content').html(html);
            } else {
                $('#config-managers-content').html('<p class="text-muted small mb-0">No program managers assigned.</p>');
            }
        }).fail(function () {
            $('#config-managers-loading').hide();
            $('#config-managers-content').html('<p class="text-danger small mb-0">Failed to load program managers.</p>');
        });
    }

    function _loadFeaturesTab(tenantId) {
        $('#config-features-loading').show();
        $('#config-features-content').html('');
        $('#config-features-actions').hide();

        abp.ajax({
            url: abp.appPath + 'api/feature-management/features',
            type: 'GET',
            data: { providerName: 'T', providerKey: tenantId }
        }).done(function (result) {
            $('#config-features-loading').hide();
            $('#config-features-content').html(_renderFeatureGroups(result.groups));
            $('#config-features-actions').show();
            _captureFeaturesToForm();
        }).fail(function () {
            $('#config-features-loading').hide();
            $('#config-features-content').html('<div class="alert alert-danger">Failed to load features. Please try again.</div>');
        });
    }

    function _captureFeaturesToForm() {
        if (!_featuresLoaded) return;
        let features = [];
        $('#config-features-content input[type="checkbox"]').each(function () {
            features.push({ name: $(this).data('feature-name'), value: $(this).prop('checked') ? 'True' : 'False' });
        });
        $('#config-features-json').val(JSON.stringify(features));
    }

    function _specializationCheckboxChange() {
        if ($(this).prop('checked')) {
            let $allSpecs = $('[data-feature-group="Specializations"] input[type="checkbox"]');
            $allSpecs.not(this).prop('checked', false);
        }
    }

    function _configSearchInputAction() {
        return _searchFieldInputAction('config-search-field', 'config-search-value');
    }

    function _configSearchResponseCallback(result) {
        return _searchResponseCallback(result);
    }

    function _loadCreateFeaturesTab() {
        $('#create-features-loading').show();
        $('#create-features-content').html('');

        abp.ajax({
            url: abp.appPath + 'api/feature-management/features',
            type: 'GET',
            data: { providerName: 'T', providerKey: _createFeatureProviderKey }
        }).done(function (result) {
            $('#create-features-loading').hide();
            $('#create-features-content').html(_renderFeatureGroups(result.groups));
            _captureCreateFeaturesToForm();
        }).fail(function () {
            $('#create-features-loading').hide();
            $('#create-features-content').html('<div class="alert alert-danger">Failed to load features. Please try again.</div>');
        });
    }

    function _captureCreateFeaturesToForm() {
        if (!_createFeaturesLoaded) return;
        let featureKeys = [];
        $('#create-features-content input[type="checkbox"]:checked').each(function () {
            featureKeys.push($(this).data('feature-name'));
        });
        $('#create-features-json').val(featureKeys.join(','));
    }

    // ─── Configuration modal: Reporting tab (view role) ───────────────────────

    let _tenantViewRoleAppService = unity.reporting.configuration.tenantViewRole;

    function _saveReportingViewRole(tenantId, onSaved) {
        let $btn = $('#config-save-role-btn');
        let viewRole = $('#config-view-role-input').val().trim();

        if (!viewRole) {
            abp.notify.warn('Please enter a view role name.');
            return;
        }

        $btn.prop('disabled', true).html('<i class="fa-solid fa-spinner fa-spin"></i> Saving...');

        _tenantViewRoleAppService.update(tenantId, { viewRole: viewRole })
            .done(function () {
                let $indicator = $('#pane-reporting .default-role-indicator');
                if ($indicator.length) {
                    $indicator.tooltip('dispose');
                    $indicator.remove();
                }
                $('#config-view-role-input').attr('data-is-default', 'false');

                abp.notify.success('View role saved successfully.');
                if (onSaved) onSaved(viewRole);
            })
            .fail(function () {
                abp.notify.error('Failed to save view role.');
            })
            .always(function () {
                $btn.prop('disabled', false).html('<i class="fa-regular fa-floppy-disk"></i> Save');
            });
    }

    function _assignReportingRoleToViews(tenantId, tenantName, viewRole) {
        let $btn = $('#config-assign-role-btn');
        $btn.prop('disabled', true).html('<i class="fa-solid fa-spinner fa-spin"></i> Assigning...');

        _tenantViewRoleAppService.assignRoleToViews(tenantId)
            .done(function () {
                abp.notify.success('Role assignment jobs have been queued for tenant "' + tenantName + '". The process will complete in the background.');
            })
            .fail(function () {
                abp.notify.error('Failed to queue role assignment jobs.');
            })
            .always(function () {
                $btn.prop('disabled', false).html('<i class="fa-solid fa-gears"></i> Assign to Views');
            });
    }

    function _wireReportingTabHandlers(tenantId) {
        $('#config-save-role-btn').off('click').on('click', function () {
            _saveReportingViewRole(tenantId);
        });

        $('#config-assign-role-btn').off('click').on('click', function () {
            let $btn = $(this);
            let tenantName = $btn.data('tenant-name');
            let viewRole = $('#config-view-role-input').val().trim();
            let isDefault = $('#config-view-role-input').attr('data-is-default') === 'true';

            if (!viewRole) {
                abp.notify.warn('Please enter a view role name before assigning it to views.');
                return;
            }

            if (isDefault) {
                abp.message.confirm(
                    'The role "' + viewRole + '" is using the default pattern and hasn\'t been saved yet. Would you like to save it first and then assign it to views?',
                    'Save and Assign Role',
                    function (isConfirmed) {
                        if (isConfirmed) {
                            _saveReportingViewRole(tenantId, function (savedViewRole) {
                                _assignReportingRoleToViews(tenantId, tenantName, savedViewRole);
                            });
                        }
                    }
                );
            } else {
                _assignReportingRoleToViews(tenantId, tenantName, viewRole);
            }
        });

        $('#config-view-database-info-btn').off('click').on('click', function () {
            let $btn = $(this);
            _reportingDatabaseInfoModal.open({
                tenantId: tenantId,
                tenantName: $btn.data('tenant-name')
            });
        });
    }

    function _configurationModalInitModal(publicApi, args) {
        _configTenantId = args.id;

        _loadManagersTab(_configTenantId);
        _wireReportingTabHandlers(_configTenantId);

        _configFilterDataTable = $('#ConfigUserSearchTable').DataTable(
            abp.libs.datatables.normalizeConfiguration({
                order: [[0, 'asc']],
                processing: true,
                serverSide: false,
                scrollX: true,
                paging: true,
                searching: false,
                ajax: abp.libs.datatables.createAjax(
                    _userImportService.search,
                    _configSearchInputAction,
                    _configSearchResponseCallback
                ),
                select: { style: 'single' },
                columnDefs: [
                    { title: 'First Name', name: 'firstName', data: 'firstName', className: 'data-table-header' },
                    { title: 'Last Name', name: 'lastName', data: 'lastName', className: 'data-table-header' },
                    { title: 'Display Name', name: 'displayName', data: 'displayName', className: 'data-table-header' },
                    { title: 'Email', name: 'email', data: 'email', className: 'data-table-header' }
                ]
            })
        );

        $('#config-search-field').on('change', function () {
            let placeholders = {
                firstName: 'At least 2 characters...',
                lastName: 'At least 2 characters...',
                firstAndLast: 'e.g. John Smith',
                email: 'At least 2 characters...'
            };
            $('#config-search-value').val('').attr('placeholder', placeholders[$(this).val()] || 'At least 2 characters...');
        });

        $('#ConfigTenantAdminSearchButton').click(function (e) {
            e.preventDefault();
            if ($('#config-search-value').val().trim().length < 2) {
                abp.notify.warn(lGm('TenantList:SearchMinChars'));
                return;
            }
            _configFilterDataTable.ajax.reload();
            $('#config-selected-user-identifier').val('');
            $('#config-selected-user-display').hide();
        });

        _configFilterDataTable.on('select', function (e, dt, type, indexes) {
            if (type === 'row') {
                let selectedData = _configFilterDataTable.row(indexes).data();
                $('#config-selected-user-identifier').val(selectedData.userGuid);
                let displayName = selectedData.displayName || (selectedData.firstName + ' ' + selectedData.lastName).trim();
                $('#config-selected-user-name').text(displayName);
                $('#config-selected-user-display').show();
            }
        });

        _configFilterDataTable.on('deselect', function () {
            $('#config-selected-user-identifier').val('');
            $('#config-selected-user-display').hide();
        });

        _featuresLoaded = false;
        $('#tab-features').on('shown.bs.tab', function () {
            if (!_featuresLoaded) {
                _featuresLoaded = true;
                _loadFeaturesTab(_configTenantId);
            }
        });
        $('#config-features-content').on('change', '[data-feature-group="Specializations"] input[type="checkbox"]', _specializationCheckboxChange);
        $('#config-features-content').on('change', 'input[type="checkbox"]', _captureFeaturesToForm);

        $('#pane-features').closest('form').on('invalid-form.validate', function (e, validator) {
            if (validator.errorList.length > 0) {
                let $firstErrorPane = $(validator.errorList[0].element).closest('.tab-pane');
                if ($firstErrorPane.length) {
                    $('[data-bs-target="#' + $firstErrorPane.attr('id') + '"]').tab('show');
                }
            }
        });
    }

    abp.modals.configurationModal = function () {
        return { initModal: _configurationModalInitModal };
    };

    // ─── Delete confirmation ──────────────────────────────────────────────────

    function _onDeleteConfirmed(id) {
        return function (confirmed) {
            if (confirmed) {
                _tenantAppService.delete(id).then(function () {
                    _dataTable.ajax.reload();
                    abp.notify.success(l('SuccessfullyDeleted'));
                });
            }
        };
    }

    function _confirmDeleteTenant(id, name) {
        abp.message.confirm(l('TenantDeletionConfirmationMessage', name), _onDeleteConfirmed(id));
    }

    // ─── Document ready ───────────────────────────────────────────────────────

    $(function () {
        // Parse CAS client code hash from hidden field data attribute
        let casClientCodeHashEl = document.getElementById('casClientCodeHashData');
        try {
            _casClientCodeHash = casClientCodeHashEl ? JSON.parse(casClientCodeHashEl.dataset.hash || '{}') : {};
        } catch (e) {
            console.warn('Failed to parse CAS client code hash', e);
        }

        _dataTable = initializeDataTable({
            dt: $('#TenantsTable'),
            listColumns: listColumns,
            defaultVisibleColumns: defaultVisibleColumns,
            defaultSortColumn: 1,
            dataEndpoint: _tenantAppService.getList,
            responseCallback: responseCallback,
            actionButtons: commonTableActionButtons('Tenants').filter(function (b) { return b.id !== 'btn-toggle-filter'; }),
            serverSideEnabled: false,
            pagingEnabled: true,
            reorderEnabled: true,
            languageSetValues: {},
            dynamicButtonContainerId: 'dynamicButtonContainerId',
            externalSearchId: 'search',
            fixedHeaders: true
        });

        // Disable interactive row selection (selection is only ever driven via the API),
        // without needing a "selectable" option on the shared initializeDataTable helper.
        _dataTable.select.style('api');

        _createModal.onResult(function () {
            _dataTable.ajax.reload();
        });

        _configurationModal.onResult(function () {
            _dataTable.ajax.reload();
        });

        // Relocate the page-toolbar "New Tenant" button into the action bar, to the
        // left of the Filter button, matching the Endpoints list layout.
        $('#tenantCreateButtonContainer').append($('#AbpContentToolbar button[name=CreateTenant]'));

        $('#tenantCreateButtonContainer button[name=CreateTenant]').click(function (e) {
            e.preventDefault();
            _createModal.open();
        });

        // Action column event delegation
        $(document).on('click', '.tenant-action-config', function (e) {
            e.preventDefault();
            _configurationModal.open({ id: $(this).data('id') });
        });

        $(document).on('click', '.tenant-action-delete', function (e) {
            e.preventDefault();
            _confirmDeleteTenant($(this).data('id'), $(this).data('name'));
        });

        $(document).on('click', '.post-creation-status-icon', function (e) {
            e.preventDefault();
            let rowData = _dataTable.row($(this).closest('tr')).data();
            _showPostCreationSectionsDetail(_parsePostCreationSections(rowData ? rowData.sections : '[]'));
        });
    });

    // ─── CAS client select handler (event delegation for dynamic elements) ────

    $(document).on('change', '.cas-client-select', function() {
        const $select = $(this);
        const selectedOption = $select.find('option:selected');

        const ministryValue = selectedOption.data('ministry') || '';
        const ministryTarget = $select.data('ministry-target');
        if (ministryTarget) {
            const $targetInput = $(ministryTarget);
            if ($targetInput.length) {
                $targetInput.val(ministryValue);
            }
        }

        const casClientCode = selectedOption.data('cas-client-code');
        if (casClientCode) {
            const $container = $select.closest('form, .modal-body');
            const $hiddenField = $container.find('input[name="CasClientCode"]');
            if ($hiddenField.length) {
                $hiddenField.val(casClientCode);
            }
        }
    });
})();
