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
            '<i class="fa fa-cog"></i> ' + lGm('TenantList:ActionsButton') + '</a>' +
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
        { title: lGm('TenantList:LicencePlate'),  data: 'licencePlate', name: 'licencePlate', index: 2 },
        { title: l('Division'),    data: 'division',     name: 'division',     index: 3 },
        { title: l('Branch'),      data: 'branch',       name: 'branch',       index: 4 },
        { title: l('Description'), data: 'description',  name: 'description',  index: 5 },
        {
            title: lGm('TenantList:CasClientCode'),
            data: 'casClientCode',
            name: 'casClientCode',
            index: 6,
            render: function (data, type, row) {
                if (type === 'display') {
                    return _casClientCodeHash[row.casClientCode || ''] || '';
                }
                return data;
            }
        },
        { title: l('Id'), data: 'id', name: 'id', index: 7 }
    ];

    let defaultVisibleColumns = ['actions', 'name', 'licencePlate', 'division', 'branch', 'description', 'casClientCode'];

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
                        function () {
                            return {
                                directory: 'IDIR',
                                firstName: $('#create-tenant-firstName').val(),
                                lastName: $('#create-tenant-lastName').val()
                            };
                        },
                        function (result) {
                            return {
                                recordsTotal: result.length,
                                recordsFiltered: result.length,
                                data: result
                            };
                        }
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
                    }],
                })
        );

        $('#TenantAdminSearchButton').click(function (e) {
            e.preventDefault();
            _filterDataTable.ajax.reloadEx();
            $('#create-tenant-btn').attr('disabled', true);
        });

        $('#cancel-tenant-btn').click(function (e) {
            _createModal.close();
        });

        _filterDataTable.on('select', function (e, dt, type, indexes) {
            if (type === 'row') {
                let selectedData = _filterDataTable.row(indexes).data();
                $('#create-tenant-admin-id').val(selectedData.userGuid);
                $('#create-tenant-btn').removeAttr('disabled');
            }
        });

        _filterDataTable.on('deselect', function (e, dt, type, indexes) {
            $('#create-tenant-admin-id').val();
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
    }

    abp.modals.createTenant = function () {
        return { initModal: _createTenantInitModal };
    };

    // ─── Modal setup: Configuration ───────────────────────────────────────────

    let _configTenantId = null;
    let _featuresLoaded = false;

    function _renderFeatureItem(feature) {
        let id = 'ft-' + feature.name.replaceAll('.', '-');
        let checked = feature.value === 'true' ? ' checked' : '';
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
            features.push({ name: $(this).data('feature-name'), value: $(this).prop('checked').toString() });
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
        let field = $('#config-search-field').val();
        let value = $('#config-search-value').val();
        if (field === 'firstAndLast') {
            let parts = value.trim().replace(/\s+/g, ' ').split(' ');
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

    function _configSearchResponseCallback(result) {
        return { recordsTotal: result.length, recordsFiltered: result.length, data: result };
    }

    function _configurationModalInitModal(publicApi, args) {
        _configTenantId = args.id;

        _loadManagersTab(_configTenantId);

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
            _configFilterDataTable.ajax.reloadEx();
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
                    _dataTable.ajax.reloadEx();
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
            data: { maxResultCount: 1000 },
            responseCallback: responseCallback,
            actionButtons: commonTableActionButtons('Tenants').filter(function (b) { return b.id !== 'btn-toggle-filter'; }),
            serverSideEnabled: false,
            pagingEnabled: true,
            reorderEnabled: true,
            languageSetValues: {},
            dynamicButtonContainerId: 'dynamicButtonContainerId',
            externalSearchId: 'search'
        });

        // Disable interactive row selection (selection is only ever driven via the API),
        // without needing a "selectable" option on the shared initializeDataTable helper.
        _dataTable.select.style('api');

        _createModal.onResult(function () {
            _dataTable.ajax.reloadEx();
        });

        _configurationModal.onResult(function () {
            _dataTable.ajax.reloadEx();
        });

        $('#AbpContentToolbar button[name=CreateTenant]').click(function (e) {
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
