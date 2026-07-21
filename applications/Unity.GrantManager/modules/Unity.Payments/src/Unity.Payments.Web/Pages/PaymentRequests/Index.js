$(function () {
    const l = abp.localization.getResource('Payments');
    const nullPlaceholder = '—';
    const requestedFieldsStorageKey = 'PaymentRequests_RequestedFields';
    const formatter = createNumberFormatter();
    const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
    let dt = $('#PaymentRequestListTable');
    let dataTable;
    let isApprove = false;

    const listColumns = getColumns();
    const defaultVisibleColumns = [
        'select',
        'referenceNumber',
        'batchName',
        'applicantName',
        'supplierNumber',
        'siteNumber',
        'contactNumber',
        'invoiceNumber',
        'payGroup',
        'amount',
        'status',
        'requestedOn',
        'updatedOn',
        'paidOn',
        'l1ApprovalDate',
        'l2ApprovalDate',
        'l3ApprovalDate',
        'CASResponse',
        'accountCodingDisplay'
    ];
    let initialLoad = true;
    let isRestoringState = false;
    let refreshDataTimeout = null;

    let languageSetValues = {
        buttons: {
            stateRestore: 'View %d'
        },
        stateRestore: {
            creationModal: {
                title: 'Create View',
                name: 'Name',
                button: 'Save',
            },
            emptyStates: 'No saved views',
            renameTitle: 'Rename View',
            renameLabel: 'New name for "%s"',
            removeTitle: 'Delete View',
            removeConfirm: 'Are you sure you want to delete "%s"?',
            removeSubmit: 'Delete',
            duplicateError: 'A view with this name already exists.',
            removeError: 'Failed to remove view.',
        }
    };

    let paymentRequestStatusModal = new abp.ModalManager({
        viewUrl: 'PaymentApprovals/UpdatePaymentRequestStatus',
    });

    paymentRequestStatusModal.onOpen(function () {
        calculateUpdateTotalAmount();
    });

    let selectedPaymentIds = [];

    let actionButtons = [
        {
            text: 'Check Status',
            className: 'custom-table-btn flex-none btn btn-secondary payment-check-status',
            attr: {
                'data-selector': 'batch-payment-table-actions'
            },
            action: function (e, dt, node, config) {
                if (!dt.rows({ selected: true }).any() || !selectedPaymentIds || selectedPaymentIds.length === 0) {
                    abp.notify.info('No Payment Requests were selected for this action.')
                    return;
                }

                $.ajax({
                    url: '/api/app/payment-request/manually-add-payment-requests-to-reconciliation-queue',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(selectedPaymentIds)
                })
                .done(() => {
                    abp.notify.success('The Status Check has been sent for verification to CFS. Please refresh this page to check for Status updates.');
                    $(".select-all-payments").prop("checked", false);
                    payment_approve_buttons.disable();
                    payment_check_status_buttons.disable();
                    history_button.disable();
                    if (cancel_button) cancel_button.disable();
                    selectedPaymentIds = [];
                    PubSub.publish("deselect_batchpayment_application", "reset_data");
                })
                .fail(() => abp.notify.error(l('Failed To Add To Reconciliation Queue')));
            }
        },
        {
            text: 'Approve',
            className: 'custom-table-btn flex-none btn btn-secondary payment-status',
            attr: {
                'data-selector': 'batch-payment-table-actions'
            },
            action: function (e, dt, node, config) {
                // Store payment IDs in distributed cache to avoid URL length limits
                unity.payments.paymentRequests.paymentBulkActions
                    .storePaymentIds({ paymentRequestIds: selectedPaymentIds })
                    .then(function(response) {
                        paymentRequestStatusModal.open({
                            cacheKey: response.cacheKey,
                            isApprove: true
                        });
                        isApprove = true;
                    })
                    .catch(function(error) {
                        abp.notify.error('Failed to prepare payment approval. Please try again.');
                        console.error('Error storing payment IDs:', error);
                    });
            }
        },
        {
            text: 'Decline',
            className: 'custom-table-btn flex-none btn btn-secondary payment-status',
            attr: {
                'data-selector': 'batch-payment-table-actions'
            },
            action: function (e, dt, node, config) {
                // Store payment IDs in distributed cache to avoid URL length limits
                unity.payments.paymentRequests.paymentBulkActions
                    .storePaymentIds({ paymentRequestIds: selectedPaymentIds })
                    .then(function(response) {
                        paymentRequestStatusModal.open({
                            cacheKey: response.cacheKey,
                            isApprove: false
                        });
                        isApprove = false;
                    })
                    .catch(function(error) {
                        abp.notify.error('Failed to prepare payment decline. Please try again.');
                        console.error('Error storing payment IDs:', error);
                    });
            }
        },
        ...(abp.auth.isGranted('PaymentsPermissions.Payments.CancelPayment') ? [{
            text: 'Cancel',
            className: 'custom-table-btn flex-none btn btn-secondary payment-cancel',
            action: function (e, dt, node, config) {
                if (selectedPaymentIds?.length !== 1) return;
                const rowData = dt.rows({ selected: true }).data().toArray()[0];
                abp.message.confirm(
                    `Are you sure you want to cancel the payment: "${rowData.referenceNumber}"?`,
                    'Cancel Payment',
                    function (confirmed) {
                        if (!confirmed) return;
                        unity.payments.paymentRequests.paymentRequest
                            .cancel(selectedPaymentIds[0])
                            .then(function () {
                                abp.notify.success('Payment has been cancelled successfully.');
                                $(".select-all-payments").prop("checked", false);
                                payment_approve_buttons.disable();
                                payment_check_status_buttons.disable();
                                history_button.disable();
                                if (cancel_button) cancel_button.disable();
                                selectedPaymentIds = [];
                                PubSub.publish("deselect_batchpayment_application", "reset_data");
                                dataTable.ajax.reload(null, false);
                            })
                            .catch(function (err) {
                                abp.notify.error('Failed to cancel payment. Please try again.');
                                console.warn('Cancel payment error:', err);
                            });
                    }
                );
            }
        }] : []),
        {
            text: 'History',
            className: 'custom-table-btn flex-none btn btn-secondary history',
            attr: {
                'data-selector': 'batch-payment-table-actions'
            },
            action: function (e, dt, node, config) {
                location.href = '/PaymentHistory/Details?PaymentId=' + selectedPaymentIds[0];
            }
        },
        {
            text: 'Filter',
            className: 'custom-table-btn flex-none btn btn-secondary',
            id: "btn-toggle-filter",
            action: function (e, dt, node, config) { },
            attr: {
                id: 'btn-toggle-filter'
            }
        },
        {
            extend: 'savedStates',
            className: 'custom-table-btn flex-none btn btn-secondary grp-savedStates',
            config: {
                creationModal: true,
                splitSecondaries: [
                    { extend: 'updateState', text: '<i class="fa-regular fa-floppy-disk"></i> Update' },
                    { extend: 'renameState', text: '<i class="fa-regular fa-pen-to-square"></i> Rename' },
                    { extend: 'removeState', text: '<i class="fa-regular fa-trash-can"></i> Delete' }
                ]
            },
            buttons: [
                { extend: 'createState', text: 'Save As View' },
                {
                    text: 'Reset to Default View',
                    action: function (e, dt, node, config) {
                        let dtInit = dt.init();
                        let initialSortOrder = dtInit?.order ?? [];

                        dt.columns().visible(false);

                        const allColumnNames = dt.settings()[0].aoColumns
                            .map(col => col.name)
                            .filter(colName => !defaultVisibleColumns.includes(colName));

                        const orderedIndexes = [];
                        defaultVisibleColumns.forEach((colName) => {
                            const colIdx = dt.column(`${colName}:name`).index();
                            if (colIdx !== undefined && colIdx !== -1) {
                                dt.column(colIdx).visible(true);
                                orderedIndexes.push(colIdx);
                            }
                        });

                        allColumnNames.forEach((colName) => {
                            const colIdx = dt.column(`${colName}:name`).index();
                            if (colIdx !== undefined && colIdx !== -1) {
                                orderedIndexes.push(colIdx);
                            }
                        });

                        dt.colReorder.order(orderedIndexes);
                        dt.columns.adjust();

                        if (typeof dt.filterRow === 'function') {
                            const filterRowApi = dt.filterRow();
                            if (filterRowApi && typeof filterRowApi?.clearFilters === 'function') {
                                filterRowApi.clearFilters();
                            }
                        }

                        $('.dt-search input').val('');
                        $('#search').val('');
                        dt.search('').order(initialSortOrder).draw();
                    }
                },
                { extend: 'removeAllStates', text: 'Delete All Views' },
                { extend: 'spacer', style: 'bar' }
            ]
        },
        {
            extend: 'csv',
            text: 'Export',
            title: 'Payment Requests',
            className: 'custom-table-btn flex-none btn btn-secondary',
            exportOptions: {
                columns: ':visible:not(.notexport)',
                orthogonal: 'fullName',
                format: {
                    body: function (data, row, column, node) {
                        return data === nullPlaceholder ? '' : data;
                    }
                }
            }
        }
    ];

    let responseCallback = function (result) {
        return {
            recordsTotal: result.totalCount,
            recordsFiltered: result.items.length,
            data: formatItems(result.items)
        };
    };

    let formatItems = function (items) {
        const newData = items.map((item, index) => {
            return {
                ...item,
                rowCount: index
            };
        });
        return newData;
    }

    dataTable = initializeDataTable({
        dt,
        defaultVisibleColumns,
        listColumns,
        maxRowsPerPage: 10,
        defaultSortColumn: {
            name: 'requestedOn',
            dir: 'desc'
        },
        dataEndpoint: unity.payments.paymentRequests.paymentRequest.getList,
        data: function () {
            let requestedFields;
            if (dataTable) {
                try {
                    const cols = dataTable.settings()[0].aoColumns;
                    requestedFields = cols
                        .filter(function (col, idx) { return dataTable.column(idx).visible(); })
                        .map(function (col) { return col.sName; })
                        .filter(function (name) { return !!name; });
                    if (requestedFields.length > 0) {
                        localStorage.setItem(requestedFieldsStorageKey, JSON.stringify(requestedFields));
                    }
                } catch {
                    // DataTable may still be initializing.
                }
            }

            if (!requestedFields || requestedFields.length === 0) {
                try {
                    const saved = localStorage.getItem(requestedFieldsStorageKey);
                    if (saved) {
                        requestedFields = JSON.parse(saved);
                    }
                } catch {
                    // Ignore local storage parse errors and use defaults.
                }
            }

            if (!requestedFields || requestedFields.length === 0) {
                requestedFields = defaultVisibleColumns;
            }

            return {
                requestedFields: requestedFields
            };
        },
        responseCallback,
        actionButtons,
        deferRender: true,
        pagingEnabled: true,
        reorderEnabled: true,
        languageSetValues,
        dataTableName: 'PaymentRequestListTable',
        dynamicButtonContainerId: 'dynamicButtonContainerId',
        useNullPlaceholder: true,
        fixedHeaders: true,
        onStateSaveParams: function (settings, data) {
            data.customFilters = {
                externalSearchValue: $('#search').val() || ''
            };
        },
        onStateLoadParams: function (settings, data) {
            if (!initialLoad) {
                isRestoringState = true;
                if (data?.customFilters) {
                    $('#search').val(data.customFilters.externalSearchValue || '');
                }
            }
        },
        onStateLoaded: function (dtApi, data) {
            if (!initialLoad) {
                isRestoringState = false;
                dtApi.ajax.reload(null, false);
            }
            initialLoad = false;
        },
        enableContextMenu: true,
        contextMenuActionsSelector: '[data-selector="batch-payment-table-actions"]'
    });

    $('.grp-savedStates').text('Save View');
    $('.grp-savedStates').closest('.btn-group').addClass('cstm-save-view');

    dataTable.on('column-visibility.dt', function (e, settings, columnIdx) {
        try {
            const cols = dataTable.settings()[0].aoColumns;
            const visibleFields = cols
                .filter(function (col, idx) { return dataTable.column(idx).visible(); })
                .map(function (col) { return col.sName; })
                .filter(function (name) { return !!name; });
            if (visibleFields.length > 0) {
                localStorage.setItem(requestedFieldsStorageKey, JSON.stringify(visibleFields));
            }
            // During a saved-view restore, isRestoringState is true and onStateLoaded
            // fires a single authoritative reload after all columns are applied.
            if (!isRestoringState && cols[columnIdx]?.refreshData) {
                clearTimeout(refreshDataTimeout);
                refreshDataTimeout = setTimeout(function () {
                    dataTable.ajax.reload(null, false);
                }, 300);
            }
        } catch { }
    });

    // Attach the draw event to add custom row coloring logic
    dataTable.on('draw', function () {
        dataTable.rows().every(function () {
            let data = this.data();
            if (data.errorSummary != null && data.errorSummary !== '') {
                $(this.node()).addClass('error-row'); // Change to your desired color
            }
        });

        // Initialize tooltips
        $('[data-toggle="tooltip"]').tooltip();
    });

    let payment_approve_buttons = dataTable.buttons(['.payment-status']);
    let payment_check_status_buttons = dataTable.buttons(['.payment-check-status']);
    let history_button = dataTable.buttons(['.history']);
    let cancel_button = abp.auth.isGranted('PaymentsPermissions.Payments.CancelPayment')
        ? dataTable.buttons(['.payment-cancel'])
        : null;

    payment_approve_buttons.disable();
    payment_check_status_buttons.disable();
    history_button.disable();
    if (cancel_button) cancel_button.disable();
    dataTable.on('search.dt', () => handleSearch());

    function checkAllRowsHaveState(states) {
        const allowedStates = Array.isArray(states) ? states : [states];
        return dataTable
            .rows('.selected')
            .data()
            .toArray()
            .every(row => allowedStates.includes(row.status));
    }

    $('#PaymentRequestListTable').on('click', 'tr td', function (e) {
        let column = dataTable.column(this);
        let columnName = dataTable.context[0].aoColumns[column.index()].sName;
        if (columnName == "CASResponse") {
            e.preventDefault();
            e.stopImmediatePropagation();
            return false;
        }
    });

    dataTable.on('select', function (e, dt, type, indexes) {
        if (indexes?.length) {
            indexes.forEach(index => {
                $("#row_" + index).prop("checked", true);
                if ($(".chkbox:checked").length == $(".chkbox").length) {
                    $(".select-all-payments").prop("checked", true);
                }
                let row = dataTable.row(index).node();
                let data = dataTable.row(index).data();
                if (data.errorSummary != null && data.errorSummary !== '') {
                    $(row).removeClass('error-row');
                    $(row).find('i.fa-flag').addClass('error-icon-selected');
                }
                selectApplication(type, index, 'select_batchpayment_application');
            });
        }
    });

    dataTable.on('deselect', function (e, dt, type, indexes) {
        if (indexes?.length) {
            indexes.forEach(index => {
                selectApplication(type, index, 'deselect_batchpayment_application');
                $("#row_" + index).prop("checked", false);
                if ($(".chkbox:checked").length != $(".chkbox").length) {
                    $(".select-all-payments").prop("checked", false);
                }
                let row = dataTable.row(index).node();
                let data = dataTable.row(index).data();
                if (data.errorSummary != null && data.errorSummary !== '') {
                    $(row).addClass('error-row');
                    $(row).find('i.fa-flag').removeClass('error-icon-selected');
                }
            });
        }
    });

    function selectApplication(type, indexes, action) {
        if (type === 'row') {
            let data = dataTable.row(indexes).data();
            PubSub.publish(action, data);

            if (action == 'select_batchpayment_application') {
                selectedPaymentIds.push(data.id);
            }
            else if (action == 'deselect_batchpayment_application') {
                selectedPaymentIds = selectedPaymentIds.filter(item => item !== data.id);
            }

            checkActionButtons();

        }
    }

    function checkActionButtons() {
        let isInSentState = checkAllRowsHaveState(['Submitted', 'FSB']);
        if (isInSentState) {
            payment_check_status_buttons.enable();
        } else {
            payment_check_status_buttons.disable();
        }
        let hasHistoricalPayment = dataTable.rows('.selected').data().toArray().some(row => row.status === 'HistoricalPayment');
        let hasCancelledPayment = dataTable.rows('.selected').data().toArray().some(row => row.status === 'Cancelled');
        const hasSelection = dataTable.rows({ selected: true }).indexes().length > 0;
        const canApprove = hasSelection && !isInSentState && !hasHistoricalPayment && !hasCancelledPayment
            && (abp.auth.isGranted('PaymentsPermissions.Payments.L1ApproveOrDecline')
                || abp.auth.isGranted('PaymentsPermissions.Payments.L2ApproveOrDecline')
                || abp.auth.isGranted('PaymentsPermissions.Payments.L3ApproveOrDecline'));
        if (canApprove) {
            payment_approve_buttons.enable();
        } else {
            payment_approve_buttons.disable();
        }
        checkEnableHistoryButton(dataTable, history_button);

        if (cancel_button) {
            const eligibleCancelStatuses = ['HistoricalPayment', 'L1Pending', 'L2Pending', 'L3Pending'];
            const selectedCount = dataTable.rows({ selected: true }).indexes().length;
            if (selectedCount === 1) {
                const rowData = dataTable.rows({ selected: true }).data().toArray()[0];
                if (eligibleCancelStatuses.includes(rowData.status)) {
                    cancel_button.enable();
                } else {
                    cancel_button.disable();
                }
            } else {
                cancel_button.disable();
            }
        }
    }

    function handleSearch() {
        let filterValue = $('.dt-search input').val();
        if (filterValue !== undefined && filterValue.length > 0) {
            Array.from(document.getElementsByClassName('selected')).forEach(
                function (element, index, array) {
                    element.classList.toggle('selected');
                }
            );
            PubSub.publish("deselect_batchpayment_application", "reset_data");
        }
    }

    function getColumns() {
        let columnIndex = 1;
        const columns = [
            getSelectColumn('Select Application', 'rowCount', 'payments'),
            getPaymentReferenceColumn(columnIndex++),
            getBatchNameColumn(columnIndex++),
            getSubmissionConfirmationCodeColumn(columnIndex++),
            getApplicantNameColumn(columnIndex++),
            getCategoryColumn(columnIndex++),
            getSupplierNumberColumn(columnIndex++),
            getSupplierNameColumn(columnIndex++),
            getSiteNumberColumn(columnIndex++),
            getContractNumberColumn(columnIndex++),
            getInvoiceNumberColumn(columnIndex++),
            getPayGroupColumn(columnIndex++),
            getAmountColumn(columnIndex++),
            getStatusColumn(columnIndex++),
            getRequestedonColumn(columnIndex++),
            getUpdatedOnColumn(columnIndex++),
            getPaidOnColumn(columnIndex++),
            getPaymentRequesterColumn(columnIndex++),
            getApprovalColumn(columnIndex++, 1),
            getApprovalDateColumn(columnIndex++, 1),
            getApprovalColumn(columnIndex++, 2),
            getApprovalDateColumn(columnIndex++, 2),
            getApprovalColumn(columnIndex++, 3),
            getApprovalDateColumn(columnIndex++, 3),
            getDescriptionColumn(columnIndex++),
            getInvoiceStatusColumn(columnIndex++),
            getPaymentStatusColumn(columnIndex++),
            getCASResponseColumn(columnIndex++),
            getTagsColumn(columnIndex++),
            getNoteColumn(columnIndex++),
            getAccountDistributionColumn(columnIndex++),
            getFsbNotifiedColumn(columnIndex++),
            getCancelledColumn(columnIndex++),
            getCancelledByColumn(columnIndex++),
            getCancelledOnColumn(columnIndex++),
        ]

        return columns.map((column) => ({ ...column, targets: [column.index], orderData: [column.index, 0] }));
    }

    function getPaymentReferenceColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:PaymentID'),
            name: 'referenceNumber',
            data: 'referenceNumber',
            className: 'data-table-header',
            index: columnIndex,
            render: function (data, _, row) {
                if (row.errorSummary != null && row.errorSummary !== '') {
                    return `${data} <i class="fa fa-flag error-icon" data-toggle="tooltip" title="${row.errorSummary}"></i>`;
                } else {
                    return data;
                }
            }
        };
    }

    function getApplicantNameColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:ApplicantName'),
            name: 'applicantName',
            data: 'payeeName',
            className: 'data-table-header',
            refreshData: true,
            index: columnIndex,
            render: function (data, type, row) {
                let applicantName = (typeof data !== 'string' || data.trim() === '') ? 'Applicant Name' : data;

                if (type === 'sort' || type === 'filter') {
                    return applicantName;
                }

                const safeApplicantName = $.fn.dataTable.render.text().display(applicantName);

                if (type === 'display' && abp.auth.isGranted('GrantApplicationManagement.Applicants.ViewList')) {
                    const applicantId = row?.applicantId;
                    const isGuid = applicantId && guidPattern.test(applicantId);

                    if (isGuid) {
                        return `<a href="/GrantApplicants/Details?ApplicantId=${encodeURIComponent(applicantId)}">${safeApplicantName}</a>`;
                    }

                    return safeApplicantName;
                }

                return applicantName;
            },
        };
    }

    function getSubmissionConfirmationCodeColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:SubmissionConfirmationCode'),
            name: 'submissionConfirmationCode',
            data: 'submissionConfirmationCode',
            className: 'data-table-header text-nowrap',
            index: columnIndex,
            render: function (data, type, row) {
                let code = (typeof data !== 'string' || data.trim() === '') ? '' : data;

                if (type === 'sort' || type === 'filter') {
                    return code;
                }

                const safeCode = $.fn.dataTable.render.text().display(code);

                if (type === 'display' && abp.auth.isGranted('GrantApplicationManagement.Applications')) {
                    const applicationId = row?.correlationId;
                    const isGuid = applicationId && guidPattern.test(applicationId);

                    if (isGuid) {
                        return `<a href="/GrantApplications/Details?ApplicationId=${encodeURIComponent(applicationId)}">${safeCode}</a>`;
                    }

                    return safeCode;
                }

                return code || null;
            }
        };
    }

    function getSupplierNumberColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:SupplierNumber'),
            name: 'supplierNumber',
            data: 'supplierNumber',
            visible: true,
            className: 'data-table-header',
            index: columnIndex,
        };
    }

    function getSupplierNameColumn(columnIndex) {
        return {
            title: 'Supplier Name',
            name: 'supplierName',
            data: 'supplierName',
            visible: false,
            className: 'data-table-header',
            index: columnIndex,
        };
    }

    function getSiteNumberColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:SiteNumber'),
            name: 'siteNumber',
            data: 'site',
            className: 'data-table-header',
            refreshData: true,
            index: columnIndex,
            render: function (data) {
                return data?.number;
            }
        };
    }
    function getContractNumberColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:ContractNumber'),
            name: 'contactNumber',
            data: 'contractNumber',
            className: 'data-table-header',
            index: columnIndex,

        };
    }

    function getInvoiceNumberColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:InvoiceNumber'),
            name: 'invoiceNumber',
            data: 'invoiceNumber',
            className: 'data-table-header',
            index: columnIndex,
        };
    }

    function getPayGroupColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:PayGroup'),
            name: 'payGroup',
            data: 'site',
            className: 'data-table-header',
            refreshData: true,
            index: columnIndex,
            render: function (data) {
                if (!data) return '';
                switch (data.paymentGroup) {
                    case 1:
                        return 'EFT';
                    case 2:
                        return 'Cheque';
                    default:
                        return 'Unknown PaymentGroup';
                }
            }
        };
    }

    function getAmountColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:Amount'),
            name: 'amount',
            data: 'amount',
            className: 'data-table-header  currency-display',
            index: columnIndex,
            render: function (data) {
                return formatter.format(data);
            },
        };
    }

    function getStatusColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:Status'),
            name: 'status',
            data: 'status',
            className: 'data-table-header',
            index: columnIndex,
            render: function (data, type, row) {
                const statusText = data ? l(`Enum:PaymentRequestStatus.${data}`) : '';

                if (type === 'sort' || type === 'filter') {
                    return statusText;
                }

                const safeStatus = $.fn.dataTable.render.text().display(statusText);

                if (type === 'display') {
                    let statusColor = getStatusTextColor(data);
                    return `<span style="color:${statusColor};">` + safeStatus + '</span>';
                }

                return statusText;
            }
        };
    }

    function getRequestedonColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:RequestedOn'),
            name: 'requestedOn',
            data: 'creationTime',
            className: 'data-table-header text-nowrap',
            index: columnIndex,
            render: function (data, type) {
                return DateUtils.formatUtcDateToLocal(data, type);
            }
        };
    }
    function getUpdatedOnColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:UpdatedOn'),
            name: 'updatedOn',
            data: 'lastModificationTime',
            className: 'data-table-header text-nowrap',
            index: columnIndex,
            render: function(data, type) {
                return DateUtils.formatUtcDateToLocal(data, type);
            }
        };
    }
    function getPaidOnColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:PaidOn'),
            name: 'paidOn',
            data: 'paymentDate',
            className: 'data-table-header text-nowrap',
            index: columnIndex,
            render: function (data) {
                if (!data) return null;
                // Check if date is in DD-MMM-YYYY format
                if (/^\d{2}-[A-Z]{3}-\d{4}$/.test(data)) {
                    // Parse and reformat
                    const date = luxon.DateTime.fromFormat(data, 'dd-MMM-yyyy');
                    return date.toFormat('yyyy-MM-dd');
                }
                // Use default render for other formats
                return DataTable.render.date('YYYY-MM-DD', abp.localization.currentCulture.name)(data);
            }
        };
    }

    function getCASResponseColumn(columnIndex) {
        // Add button to view response modal
        return {
            title: l('ApplicationPaymentListTable:CASResponse'),
            name: 'CASResponse',
            data: 'casResponse',
            className: 'data-table-header notexport',
            index: columnIndex,
            render: function (data) {
                if (data + "" !== "undefined" && data?.length > 0) {
                    const escaped = data
                        .replace(/\\/g, '\\\\')
                        .replace(/'/g, "\\'")
                        .replace(/&/g, '&amp;')
                        .replace(/"/g, '&quot;')
                        .replace(/</g, '&lt;')
                        .replace(/>/g, '&gt;');
                    return '<button class="btn btn-light info-btn" type="button" onclick="openCasResponseModal(\'' + escaped + '\');">View Response<i class="fl fl-mapinfo"></i></button>';
                }
                return null;
            }
        };
    }

    function getPaymentRequesterColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:PaymentRequesterName'),
            name: 'paymentRequesterName',
            data: 'creatorUser',
            className: 'data-table-header',
            refreshData: true,
            index: columnIndex,
            render: function (data) {
                return formatName(data);
            }
        };
    }

    function getApprovalColumn(columnIndex, level) {
        return {
            title: l(`ApplicationPaymentListTable:L${level}ApproverName`),
            name: `l${level}ApproverName`,
            data: 'expenseApprovals',
            className: 'data-table-header',
            refreshData: true,
            index: columnIndex,
            render: function (data) {
                const approval = getExpenseApprovalsDetails(data, level);
                return formatName(approval?.decisionUser);
            }
        };
    }

    function formatName(userData) {
        return typeof userData !== 'undefined' && userData !== null ? `${userData?.name} ${userData?.surname}` : "";
    }

    function getApprovalDateColumn(columnIndex, level) {
        return {
            title: l(`ApplicationPaymentListTable:L${level}ApprovalDate`),
            name: `l${level}ApprovalDate`,
            data: 'expenseApprovals',
            className: 'data-table-header text-nowrap',
            refreshData: true,
            index: columnIndex,
            render: function (data, type) {
                let approval = getExpenseApprovalsDetails(data, level);
                const approvalDate = approval?.decisionDate;
                return DateUtils.formatUtcDateToLocal(approvalDate, type);
            }
        };
    }

    function getDescriptionColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:Description'),
            name: 'paymentDescription',
            data: 'description',
            className: 'data-table-header',
            index: columnIndex

        };
    }

    function getInvoiceStatusColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:InvoiceStatus'),
            name: 'invoiceStatus',
            data: 'invoiceStatus',
            className: 'data-table-header',
            index: columnIndex,
            render: function (data) {
                if (data + "" !== "undefined" && data?.length > 0) {
                    return data;
                } else {
                    return null;
                }
            }
        };
    }

    function getPaymentStatusColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:PaymentStatus'),
            name: 'paymentStatus',
            data: 'paymentStatus',
            className: 'data-table-header',
            index: columnIndex,
            render: function (data) {
                if (data + "" !== "undefined" && data?.length > 0) {
                    return data;
                } else {
                    return null;
                }
            }
        };
    }

    function getBatchNameColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:BatchName'),
            name: 'batchName',
            data: 'batchName',
            className: 'data-table-header',
            index: columnIndex,
            render: function (data) {
                if (data + "" !== "undefined" && data?.length > 0) {
                    return data;
                } else {
                    return null;
                }
            }
        };
    }

    function getTagsColumn(columnIndex) {
        return {
            title: 'Tags',
            name: 'paymentTags',
            data: 'paymentTags',
            className: '',
            refreshData: true,
            index: columnIndex,
            render: function (data) {
                let tagNames = (data ?? [])
                    .filter(x => x?.tag?.name)
                    .map(x => x.tag.name);
                return tagNames.join(', ') ?? '';
            }
        }
    }

    function getNoteColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:Note'),
            name: 'note',
            data: 'note',
            className: 'data-table-header',
            index: columnIndex

        };
    }

    function getAccountDistributionColumn(columnIndex) {
        return {
            title: 'Account Code',
            name: 'accountCodingDisplay',
            data: 'accountCodingDisplay',
            className: 'data-table-header',
            refreshData: true,
            index: columnIndex,
            render: function (data) {
                if (data + "" !== "undefined" && data?.length > 0) {
                    return data;
                } else {
                    return "";
                }
            }
        };
    }

    function getFsbNotifiedColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:FsbApNotified'),
            name: 'fsbApNotified',
            data: 'fsbApNotified',
            className: 'data-table-header',
            index: columnIndex,
            render: function (data, type, row) {                
                if (data) {
                    return data;
                }
                // Show placeholder for null/empty
                return nullPlaceholder;
            }
        };
    }

    function getCategoryColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:Category'),
            name: 'category',
            data: 'category',
            refreshData: true,
            className: 'data-table-header',
            index: columnIndex,
            render: function (data) {
                return data ?? nullPlaceholder;
            }
        };
    }

    function getExpenseApprovalsDetails(expenseApprovals, type) {
        return (expenseApprovals ?? []).find(x => x.type == type);
    }

    $('#search').on('input', function () {
        if (isRestoringState) {
            return;
        }
        let table = $('#PaymentRequestListTable').DataTable();
        table.search($(this).val()).draw();
    });

    paymentRequestStatusModal.onResult(function () {

        abp.notify.success(
            isApprove ? 'The payment request/s has been successfully approved' : 'The payment request/s has been successfully declined',
            'Payment Requests'
        );
        dataTable.ajax.reload(null, false);
        $(".select-all-payments").prop("checked", false);
        payment_approve_buttons.disable();
        payment_check_status_buttons.disable();
        history_button.disable();
        if (cancel_button) cancel_button.disable();
        selectedPaymentIds = [];
        PubSub.publish("deselect_batchpayment_application", "reset_data");
    });

    function getStatusTextColor(status) {
        switch (status) {
            case "L1Pending":
                return "#053662";

            case "L1Declined":
                return "#CE3E39";

            case "L2Pending":
                return "#053662";

            case "L2Declined":
                return "#CE3E39";

            case "L3Pending":
                return "#053662";

            case "L3Declined":
                return "#CE3E39";

            case "Submitted":
                return "#5595D9";

            case "HistoricalPayment":
            case "Paid":
                return "#42814A";

            case "Failed":
                return "#CE3E39";

            case "Cancelled":
                return "#6c757d";

            default:
                return "#053662";
        }
    }

    $('.select-all-payments').on('click', function () {
        if ($(this).is(':checked')) {
            dataTable.rows({ 'page': 'current' }).select();
        }
        else {
            dataTable.rows({ 'page': 'current' }).deselect();
        }
    });

    PubSub.subscribe(
        'refresh_payment_list',
        (msg, data) => {
            dataTable.ajax.reload(null, false);
            $(".select-all-payments").prop("checked", false);
            payment_approve_buttons.disable();
            payment_check_status_buttons.disable();
            history_button.disable();
            if (cancel_button) cancel_button.disable();
            selectedPaymentIds = [];
            PubSub.publish("deselect_batchpayment_application", "reset_data");
            PubSub.publish('clear_selected_payment');
        }
    );

});

function getCancelledColumn(columnIndex) {
    return {
        title: 'Cancelled',
        name: 'cancelled',
        data: null,
        className: 'data-table-header',
        index: columnIndex,
        render: function (data, type, row) {
            return row.status === 'Cancelled' ? 'Cancelled' : '';
        }
    };
}

function getCancelledByColumn(columnIndex) {
    return {
        title: 'Cancelled By',
        name: 'cancelledBy',
        data: 'cancelledBy',
        className: 'data-table-header',
        index: columnIndex,
        render: function (data) {
            return data ?? '';
        }
    };
}

function getCancelledOnColumn(columnIndex) {
    return {
        title: 'Cancelled On',
        name: 'cancelledOn',
        data: 'cancelledOn',
        className: 'data-table-header',
        index: columnIndex,
        render: function (data, type) {
            return DateUtils.formatUtcDateToLocal(data, type);
        }
    };
}

let casPaymentResponseModal = new abp.ModalManager({
    viewUrl: '../PaymentRequests/CasPaymentRequestResponse'
});

function checkEnableHistoryButton(dataTable, history_button) {
    if (dataTable.rows({ selected: true }).indexes().length == 1) {
        history_button.enable();
    } else {
        history_button.disable();
    }
}

function openCasResponseModal(casResponse) {
    casPaymentResponseModal.open({
        casResponse: casResponse
    });
}