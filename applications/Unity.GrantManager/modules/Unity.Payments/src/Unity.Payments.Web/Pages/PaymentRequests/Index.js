$(function () {
    const l = abp.localization.getResource('Payments');
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
        'l1Approval',
        'l2Approval',
        'l3Approval',
        'CASResponse'
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

    dataTable = initializeDataTable(dt,
        defaultVisibleColumns,
        listColumns, 10, 9, unity.payments.paymentRequests.paymentRequest.getList, {}, responseCallback, actionButtons, 'dynamicButtonContainerId');

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
            if (abp.auth.isGranted('PaymentsPermissions.Payments.L1ApproveOrDecline') || abp.auth.isGranted('PaymentsPermissions.Payments.L2ApproveOrDecline') || abp.auth.isGranted('PaymentsPermissions.Payments.L3ApproveOrDecline')) {
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
        return [
            getSelectColumn('Select Application', 'rowCount', 'payments'),
            getPaymenReferenceColumn(),
            getApplicantNameColumn(),
            getSupplierNumberColumn(),
            getSiteNumberColumn(),
            getContractNumberColumn(),
            getInvoiceNumberColumn(),
            getPayGroupColumn(),
            getAmountColumn(),
            getStatusColumn(),
            getRequestedonColumn(),
            getUpdatedOnColumn(),
            getPaidOnColumn(),
            getL1ApprovalColumn(),
            getL2ApprovalColumn(),
            getL3ApprovalColumn(),
            getDescriptionColumn(),
            getInvoiceStatusColumn(),
            getPaymentStatusColumn(),
            getCASResponseColumn(),
        ]
    }

    function getPaymenReferenceColumn() {
        return {
            title: l('ApplicationPaymentListTable:PaymentID'),
            name: 'referenceNumber',
            data: 'referenceNumber',
            className: 'data-table-header',
            index: 0,
        };
    }
    function getApplicantNameColumn() {
        return {
            title: l('ApplicationPaymentListTable:ApplicantName'),
            name: 'applicantName',
            data: 'payeeName',
            className: 'data-table-header',
            index: 1,
        };
    }

    function getSupplierNumberColumn() {
        return {
            title: l('ApplicationPaymentListTable:SupplierNumber'),
            name: 'supplierNumber',
            data: 'supplierNumber',
            className: 'data-table-header',
            index: 2,
        };
    }
    function getSiteNumberColumn() {
        return {
            title: l('ApplicationPaymentListTable:SiteNumber'),
            name: 'siteNumber',
            data: 'site',
            className: 'data-table-header',
            index: 3,
            render: function (data) {
                return data?.number;
            }
        };
    }
    function getContractNumberColumn() {
        return {
            title: l('ApplicationPaymentListTable:ContractNumber'),
            name: 'contactNumber',
            data: 'contractNumber',
            className: 'data-table-header',
            index: 4,

        };
    }

    function getInvoiceNumberColumn() {
        return {
            title: l('ApplicationPaymentListTable:InvoiceNumber'),
            name: 'invoiceNumber',
            data: 'invoiceNumber',
            className: 'data-table-header',
            index: 5,
        };
    }
    function getPayGroupColumn() {
        return {
            title: l('ApplicationPaymentListTable:PayGroup'),
            name: 'payGroup',
            data: 'site',
            className: 'data-table-header',
            index: 6,
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

    function getAmountColumn() {
        return {
            title: l('ApplicationPaymentListTable:Amount'),
            name: 'amount',
            data: 'amount',
            className: 'data-table-header  currency-display',
            index: 7,
            render: function (data) {
                return formatter.format(data);
            },
        };
    }

    function getStatusColumn() {
        return {
            title: l('ApplicationPaymentListTable:Status'),
            name: 'status',
            data: 'status',
            className: 'data-table-header',
            index: 8,
            render: function (data) {

                let statusText = getStatusText(data);
                let statusColor = getStatusTextColor(data);
                return '<span style="color:' + statusColor + ';">' + statusText + '</span>';
            }
        };
    }


    function getRequestedonColumn() {
        return {
            title: l('ApplicationPaymentListTable:RequestedOn'),
            name: 'requestedOn',
            data: 'creationTime',
            className: 'data-table-header',
            index: 9,
            render: function (data) {
                return formatDate(data);
            }
        };
    }
    function getUpdatedOnColumn() {
        return {
            title: l('ApplicationPaymentListTable:UpdatedOn'),
            name: 'updatedOn',
            data: 'lastModificationTime',
            className: 'data-table-header',
            index: 10,
            render: function (data) {
                return formatDate(data);
            }
        };
    }
    function getPaidOnColumn() {
        return {
            title: l('ApplicationPaymentListTable:PaidOn'),
            name: 'paidOn',
            data: 'paidOn',
            className: 'data-table-header',
            index: 11,
            render: function (data) {
                return formatDate(data);
            }
        };
    }

    function getL1ApprovalColumn() {
        return {
            title: l('ApplicationPaymentListTable:L1ApprovalDate'),
            name: 'l1Approval',
            data: 'expenseApprovals',
            className: 'data-table-header',
            index: 13,
            render: function (data) {
                let approval = getExpenseApprovalsDetails(data, 1)
                return formatDate(approval?.decisionDate);
            }
        };
    }
    function getL2ApprovalColumn() {
        return {
            title: l('ApplicationPaymentListTable:L2ApprovalDate'),
            name: 'l2Approval',
            data: 'expenseApprovals',
            className: 'data-table-header',
            index: 14,
            render: function (data) {
                let approval = getExpenseApprovalsDetails(data, 2)
                return formatDate(approval?.decisionDate);
            }
        };
    }
    function getL3ApprovalColumn() {
        return {
            title: l('ApplicationPaymentListTable:L3ApprovalDate'),
            name: 'l3Approval',
            data: 'expenseApprovals',
            className: 'data-table-header',
            index: 15,
            render: function (data) {
                let approval = getExpenseApprovalsDetails(data, 3)
                return formatDate(approval?.decisionDate);
            }
        };
    }

    function getDescriptionColumn() {
        return {
            title: l('ApplicationPaymentListTable:Description'),
            name: 'paymentDescription',
            data: 'description',
            className: 'data-table-header',
            index: 16

        };
    }

    function getInvoiceStatusColumn() {
        return {
            title: l('ApplicationPaymentListTable:InvoiceStatus'),
            name: 'invoiceStatus',
            data: 'invoiceStatus',
            className: 'data-table-header',
            index: 17,
            render: function (data) {
                if (data + "" !== "undefined" && data?.length > 0) {
                    return data;
                } else {
                    return "";
                }
            }
        };
    }

    function getPaymentStatusColumn() {
        return {
            title: l('ApplicationPaymentListTable:PaymentStatus'),
            name: 'paymentStatus',
            data: 'paymentStatus',
            className: 'data-table-header',
            index: 18,
            render: function (data) {
                if (data + "" !== "undefined" && data?.length > 0) {
                    return data;
                } else {
                    return "";
                }
            }
        };
    }

    function getCASResponseColumn() {
        // Add button to view response modal
        return {
            title: l('ApplicationPaymentListTable:CASResponse'),
            name: 'CASResponse',
            data: 'casResponse',
            className: 'data-table-header',
            index: 12,
            render: function (data) {
                if (data + "" !== "undefined" && data?.length > 0) {
                    return '<button class="btn btn-light info-btn" type="button" onclick="openCasResponseModal(\'' + data + '\');">View Response<i class="fl fl-mapinfo"></i></button>';
                }
                return '{Not Available}';
            }
        };
    }

    function getExpenseApprovalsDetails(expenseApprovals, type) {
        return expenseApprovals.find(x => x.type == type);
    }

    function formatDate(data) {
        return data != null ? luxon.DateTime.fromISO(data, {
            locale: abp.localization.currentCulture.name,
        }).toUTC().toLocaleString() : '{Not Available}';
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


