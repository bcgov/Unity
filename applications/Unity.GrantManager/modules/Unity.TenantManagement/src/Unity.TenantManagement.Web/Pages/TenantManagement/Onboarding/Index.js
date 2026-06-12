(function () {
    let _onboardingRequestAppService = unity.tenantManagement.onboardingRequest;
    let _dataTable = null;
    let _selectedRow = null;

    let _createTenantModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'TenantManagement/Onboarding/CreateTenantModal',
        modalClass: 'createTenantModal'
    });

    function _renderValidationResult(result) {
        $('#onboarding-validation-loading').addClass('d-none');
        if (result.isValid) {
            $('#onboarding-validation-result')
                .html('<div class="alert alert-success mb-0"><i class="fl fl-check me-1"></i> Validation passed. Ready to create tenant.</div>')
                .removeClass('d-none');
            $('#btn-confirm-create-tenant').prop('disabled', false);
        } else {
            let issuesHtml = (result.issues || [])
                .map(function (i) { return '<li>' + $('<span>').text(i).html() + '</li>'; })
                .join('');
            $('#onboarding-validation-result')
                .html('<div class="alert alert-danger mb-0"><strong>Validation failed:</strong><ul class="mb-0 mt-1">' + issuesHtml + '</ul></div>')
                .removeClass('d-none');
        }
    }

    function _renderValidationFail() {
        $('#onboarding-validation-loading').addClass('d-none');
        $('#onboarding-validation-result')
            .html('<div class="alert alert-danger mb-0">Failed to validate request. Please try again.</div>')
            .removeClass('d-none');
    }

    function _onCreateTenantConfirm(applicationId) {
        return function () {
            let $btn = $(this);
            $btn.prop('disabled', true);
            $('#btn-cancel-create-tenant').prop('disabled', true);
            $('#onboarding-creating').removeClass('d-none');

            abp.ajax({
                url: abp.appPath + 'api/multi-tenancy/onboarding-requests/' + applicationId + '/create-tenant',
                type: 'POST'
            }).done(function () {
                abp.notify.success('Tenant created successfully.');
                _createTenantModal.close();
                _dataTable.ajax.reloadEx();
            }).fail(function () {
                $('#onboarding-creating').addClass('d-none');
                $btn.prop('disabled', false);
                $('#btn-cancel-create-tenant').prop('disabled', false);
                abp.notify.error('Failed to create tenant. Please try again.');
            });
        };
    }

    abp.modals.createTenantModal = function () {
        return {
            initModal: function (publicApi, args) {
                let applicationId = args.id;

                abp.ajax({
                    url: abp.appPath + 'api/multi-tenancy/onboarding-requests/' + applicationId + '/validate',
                    type: 'GET'
                }).done(_renderValidationResult).fail(_renderValidationFail);

                $('#btn-confirm-create-tenant').on('click', _onCreateTenantConfirm(applicationId));
            }
        };
    };

    let listColumns = [
        { title: 'Tenant Name',              data: 'tenantName',            name: 'tenantName',            index: 0 },
        { title: 'Tenant Description',       data: 'tenantDescription',     name: 'tenantDescription',     index: 1 },
        { title: 'Program Area Name',        data: 'programAreaName',       name: 'programAreaName',       index: 2 },
        { title: 'Program Area Description', data: 'programAreaDescription', name: 'programAreaDescription', index: 3 },
        { title: 'Contact(s)',               data: 'contacts',              name: 'contacts',              index: 4 },
        { title: 'Features',                 data: 'features',              name: 'features',              index: 5 },
        { title: 'Super Users',              data: 'superUsers',            name: 'superUsers',            index: 6 },
        { title: 'Executive Director',       data: 'executiveDirector',     name: 'executiveDirector',     index: 7 },
        { title: 'Branch',                   data: 'branch',                name: 'branch',                index: 8 },
        { title: 'Ministry',                 data: 'ministry',              name: 'ministry',              index: 9 },
        { title: 'Status',                   data: 'status',                name: 'status',                index: 10 },
        { title: 'Category',                 data: 'category',              name: 'category',              index: 11 },
        {
            title: 'Submission Date',
            data: 'submissionDate',
            name: 'submissionDate',
            index: 12,
            render: function (data, type) {
                return DateUtils.formatUtcDateToLocal(data, type);
            }
        }
    ];

    let responseCallback = function (result) {
        return {
            recordsTotal: result.totalCount,
            recordsFiltered: result.totalCount,
            data: result.items
        };
    };

    function manageActionButtons() {
        const hasSelection = _selectedRow !== null;
        $('#btn-open-onboarding').toggleClass('action-bar-btn-unavailable', !hasSelection);
        $('#btn-create-tenant').toggleClass('action-bar-btn-unavailable',
            !hasSelection || _selectedRow.status !== 'Approved');
    }

    $(function () {
        _dataTable = initializeDataTable({
            dt: $('#OnboardingRequestsTable'),
            listColumns: listColumns,
            defaultSortColumn: 0,
            dataEndpoint: _onboardingRequestAppService.getList,
            data: { maxResultCount: 1000 },
            responseCallback: responseCallback,
            actionButtons: commonTableActionButtons('Onboarding Requests').filter(b => b.id !== 'btn-toggle-filter'),
            serverSideEnabled: false,
            pagingEnabled: true,
            reorderEnabled: true,
            languageSetValues: {},
            dynamicButtonContainerId: 'dynamicButtonContainerId',
            externalSearchId: 'search'
        });

        _dataTable.select.style('single');

        _dataTable.on('select', function (e, dt, type, indexes) {
            if (type === 'row' && indexes.length) {
                _selectedRow = _dataTable.row(indexes[0]).data();
                manageActionButtons();
            }
        });

        _dataTable.on('deselect', function () {
            if (_dataTable.rows({ selected: true }).count() === 0) {
                _selectedRow = null;
                manageActionButtons();
            }
        });

        $('#btn-open-onboarding').on('click', function () {
            if (!_selectedRow) return;
            // TBD: open the onboarding request details
            console.log('Open onboarding request:', _selectedRow);
        });

        $('#btn-create-tenant').on('click', function () {
            if (!_selectedRow) return;
            _createTenantModal.open({ id: _selectedRow.id });
        });
    });
})();
