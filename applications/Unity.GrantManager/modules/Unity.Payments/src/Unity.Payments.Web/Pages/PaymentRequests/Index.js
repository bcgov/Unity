$(function () {
    const l = abp.localization.getResource('Payments');
    const nullPlaceholder = '—';
    const formatter = createNumberFormatter();
    let dt = $('#PaymentRequestListTable');
    let dataTable;
    let isApprove = false;
    const listColumns = getColumns();
    const defaultVisibleColumns = [
        'referenceNumber',
        'applicantName',
        'supplierNumber',
        'creationTime',
        'siteNumber',
        'contactNumber',
        'invoiceNumber',
        'payGroup',
        'amount',
        'status',
        'requestedOn',
        'updatedOn',
        'paidOn',
        'l1ApproverName',
        'l2ApproverName',
        'l3ApproverName',
        'l1ApprovalDate',
        'l2ApprovalDate',
        'l3ApprovalDate',
        'CASResponse',
        'batchName',
        'paymentRequesterName'
    ];

    let paymentRequestStatusModal = new abp.ModalManager({
        viewUrl: 'PaymentApprovals/UpdatePaymentRequestStatus',
    });

    let selectedPaymentIds = [];

    let actionButtons = [
        {
            text: 'Approve',
            className: 'custom-table-btn flex-none btn btn-secondary payment-status',
            action: function (e, dt, node, config) {
                paymentRequestStatusModal.open({
                    paymentIds: JSON.stringify(selectedPaymentIds),
                    isApprove: true
                });
                isApprove = true;
            }
        },
        {
            text: 'Decline',
            className: 'custom-table-btn flex-none btn btn-secondary payment-status',
            action: function (e, dt, node, config) {
                paymentRequestStatusModal.open({
                    paymentIds: JSON.stringify(selectedPaymentIds),
                    isApprove: false
                });
                isApprove = false;
            }
        },
        {
            text: 'History',
            className: 'custom-table-btn flex-none btn btn-secondary history',
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
            extend: 'csv',
            text: 'Export',
            title: 'Payment Requests',
            className: 'custom-table-btn flex-none btn btn-secondary',
            exportOptions: {
                columns: ':visible:not(.notexport)',
                orthogonal: 'fullName',
            }
        },

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
        defaultSortColumn: 11,
        dataEndpoint: unity.payments.paymentRequests.paymentRequest.getList,
        data: {},
        responseCallback,
        actionButtons,
        pagingEnabled: true,
        reorderEnabled: true,
        languageSetValues: {},
        dataTableName: 'PaymentRequestListTable',
        dynamicButtonContainerId: 'dynamicButtonContainerId'});

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
    let history_button = dataTable.buttons(['.history']);    

    payment_approve_buttons.disable();
    dataTable.on('search.dt', () => handleSearch());

    function checkAllRowsHaveState(state) {
        return dataTable.rows('.selected').data().toArray().every(row => row.status === state);
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
        let isOnlySubmittedToCas = checkAllRowsHaveState('Submitted');
        if (dataTable.rows({ selected: true }).indexes().length > 0 && !isOnlySubmittedToCas) {
            if (abp.auth.isGranted('PaymentsPermissions.Payments.L1ApproveOrDecline')
                || abp.auth.isGranted('PaymentsPermissions.Payments.L2ApproveOrDecline')
                || abp.auth.isGranted('PaymentsPermissions.Payments.L3ApproveOrDecline')) {
                payment_approve_buttons.enable();

            } else {
                payment_approve_buttons.disable();
            }

            if (dataTable.rows({ selected: true }).indexes().length == 1) {
                history_button.enable();
            } else {
                history_button.disable();
            }
        }
        else {
            payment_approve_buttons.disable();
            history_button.enable();
        }
    }

    function handleSearch() {
        let filterValue = $('.dataTables_filter input').val();
        if (filterValue.length > 0) {

            Array.from(document.getElementsByClassName('selected')).forEach(
                function (element, index, array) {
                    element.classList.toggle('selected');
                }
            );
            PubSub.publish("deselect_batchpayment_application", "reset_data");
        }
    }

    function getColumns() {
        let columnIndex = 0;
        return [
            getSelectColumn('Select Application', 'rowCount', 'payments'),
            getPaymentReferenceColumn(columnIndex++),
            getApplicantNameColumn(columnIndex++),
            getSupplierNumberColumn(columnIndex++),
            getSupplierNameColumn(columnIndex++),
            getSiteNumberColumn(columnIndex++),
            getContractNumberColumn(columnIndex++),
            getInvoiceNumberColumn(columnIndex),
            getPayGroupColumn(columnIndex++),
            getAmountColumn(columnIndex++),
            getStatusColumn(columnIndex++),
            getRequestedonColumn(columnIndex++),
            getUpdatedOnColumn(columnIndex++),
            getPaidOnColumn(columnIndex++),
            getApprovalColumn(columnIndex++, 1),
            getApprovalColumn(columnIndex++, 2),
            getApprovalColumn(columnIndex++, 3),
            getApprovalDateColumn(columnIndex++, 1),
            getApprovalDateColumn(columnIndex++, 2),
            getApprovalDateColumn(columnIndex++, 3),
            getDescriptionColumn(columnIndex++),
            getInvoiceStatusColumn(columnIndex++),
            getPaymentStatusColumn(columnIndex++),
            getCASResponseColumn(columnIndex++),
            getBatchNameColumn(columnIndex++),
            getPaymentRequesterColumn(columnIndex++)
        ]
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
            index: columnIndex
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
            index: columnIndex,
            render: function (data) {
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
            render: function (data) {

                let statusText = getStatusText(data);
                let statusColor = getStatusTextColor(data);
                return '<span style="color:' + statusColor + ';">' + statusText + '</span>';
            }
        };
    }

    function getRequestedonColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:RequestedOn'),
            name: 'requestedOn',
            data: 'creationTime',
            className: 'data-table-header',
            index: columnIndex,
            render: function (data) {
                return formatDate(data);
            }
        };
    }
    function getUpdatedOnColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:UpdatedOn'),
            name: 'updatedOn',
            data: 'lastModificationTime',
            className: 'data-table-header',
            index: columnIndex,
            render: function (data) {
                return formatDate(data);
            }
        };
    }
    function getPaidOnColumn(columnIndex) {
        return {
            title: l('ApplicationPaymentListTable:PaidOn'),
            name: 'paidOn',
            data: 'paidOn',
            className: 'data-table-header',
            index: columnIndex,
            render: function (data) {
                return formatDate(data);
            }
        };
    }

    function getCASResponseColumn(columnIndex) {
        // Add button to view response modal
        return {
            title: l('ApplicationPaymentListTable:CASResponse'),
            name: 'CASResponse',
            data: 'casResponse',
            className: 'data-table-header',
            index: columnIndex,
            render: function (data) {
                if (data + "" !== "undefined" && data?.length > 0) {
                    return '<button class="btn btn-light info-btn" type="button" onclick="openCasResponseModal(\'' + data + '\');">View Response<i class="fl fl-mapinfo"></i></button>';
                }
                return '{Not Available}';
            }
        };
    }

    function getPaymentRequesterColumn(columnIndex) {
        return {
            title: l(`ApplicationPaymentListTable:PaymentRequesterName`),
            name: `paymentRequesterName`,
            data: 'expenseApprovals',
            className: 'data-table-header',
            index: columnIndex,
            render: function (data) {
                return formatName(data?.creatorUser);
            }
        };
    }

    function getApprovalColumn(columnIndex, level) {
        return {
            title: l(`ApplicationPaymentListTable:L${level}ApproverName`),
            name: `l${level}ApproverName`,
            data: 'expenseApprovals',
            className: 'data-table-header',
            index: columnIndex,
            render: function (data) {
                const approval = getExpenseApprovalsDetails(data, level);
                return formatName(approval?.lastModifierUser);
            }
        };
    }

    function formatName(userData) {
        return userData !== null ? `${userData?.name} ${userData?.surname}` : nullPlaceholder;
    }

    function getApprovalDateColumn(columnIndex, level) {
        return {
            title: l(`ApplicationPaymentListTable:L${level}ApprovalDate`),
            name: `l${level}ApprovalDate`,
            data: 'expenseApprovals',
            className: 'data-table-header',
            index: columnIndex,
            render: function (data) {
                let approval = getExpenseApprovalsDetails(data, level);
                return formatDate(approval?.decisionDate);
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
                    return "";
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
                    return "";
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
                    return nullPlaceholder;
                }
            }
        };
    }



    function getExpenseApprovalsDetails(expenseApprovals, type) {
        return expenseApprovals.find(x => x.type == type);
    }

    function formatDate(data) {
        return data != null ? luxon.DateTime.fromISO(data, {
            locale: abp.localization.currentCulture.name,
        }).toUTC().toLocaleString() : nullPlaceholder;
    }

    /* the resizer needs looking at again after ux2 refactor 
     window.addEventListener('resize', setTableHeighDynamic('PaymentRequestListTable'));
    */

    $('#search').on('input', function () {
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

        selectedPaymentIds = [];
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

            case "Paid":
                return "#42814A";

            case "PaymentFailed":
                return "#CE3E39";

            default:
                return "#053662";
        }
    }

    function getStatusText(status) {

        switch (status) {

            case "L1Pending":
                return "L1 Pending";

            case "L1Approved":
                return "L1 Approved";

            case "L1Declined":
                return "L1 Declined";

            case "L2Pending":
                return "L2 Pending";

            case "L2Approved":
                return "L2 Approved";

            case "L2Declined":
                return "L2 Declined";

            case "L3Pending":
                return "L3 Pending";

            case "L3Approved":
                return "L3 Approved";

            case "L3Declined":
                return "L3 Declined";

            case "Submitted":
                return "Submitted to CAS";

            case "Paid":
                return "Paid";

            case "PaymentFailed":
                return "Payment Failed";


            default:
                return "Created";
        }
    }

    $('.select-all-payments').click(function () {
        if ($(this).is(':checked')) {
            dataTable.rows({ 'page': 'current' }).select();
        }
        else {
            dataTable.rows({ 'page': 'current' }).deselect();
        }
    });
});


let casPaymentResponseModal = new abp.ModalManager({
    viewUrl: '../PaymentRequests/CasPaymentRequestResponse'
});

function openCasResponseModal(casResponse) {
    casPaymentResponseModal.open({
        casResponse: casResponse
    });
}


