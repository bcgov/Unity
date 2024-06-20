$(function () {  
    const l = abp.localization.getResource('Payments');
    let dt = $('#PaymentRequestListTable');
    let dataTable;
    let isApprove = false;
    const listColumns = getColumns();
    const defaultVisibleColumns = [
        'id',
        'creationTime',
    ];

    let paymentRequestStatusModal = new abp.ModalManager({
        viewUrl: 'PaymentApprovals/UpdatePaymentRequestStatus',
    });
    let selectedPaymentIds = [];
 
    let actionButtons = [
        
        {
            text: 'Approve',
            className: 'custom-table-btn flex-none btn btn-secondary payment-status-approve',
            action: function (e, dt, node, config) {
                paymentRequestStatusModal.open({
                    paymentIds: JSON.stringify(selectedPaymentIds),
                    isApprove : true
                });
                isApprove = true;
            }
        },
        {
            text: 'Decline',
            className: 'custom-table-btn flex-none btn btn-secondary payment-status-decline',
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
            className: 'custom-table-btn flex-none btn btn-secondary',
            action: function (e, dt, node, config) {
                alert('History Button activated');
            }
        },
        {
            text: 'Filter',
            className: 'custom-table-btn flex-none btn btn-secondary',
            id: "btn-toggle-filter",
            action: function (e, dt, node, config) {
                $(".tr-toggle-filter").toggle();
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
            data: result.items
        };
    };

    dataTable = initializeDataTable(dt,
        defaultVisibleColumns,
        listColumns, 15, 4, unity.payments.paymentRequests.paymentRequest.getList, {}, responseCallback, actionButtons, 'dynamicButtonContainerId');

    let approve_buttons = dataTable.buttons(['.payment-status-approve']);
    let decline_buttons = dataTable.buttons(['.payment-status-decline']);
    approve_buttons.disable();
    decline_buttons.disable();
    dataTable.on('search.dt', () => handleSearch());

    dataTable.on('select', function (e, dt, type, indexes) {
        selectApplication(type, indexes, 'select_batchpayment_application');
    });

    dataTable.on('deselect', function (e, dt, type, indexes) {
        selectApplication(type, indexes, 'deselect_batchpayment_application');
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

       if (dataTable.rows({ selected: true }).indexes().length === 0) {
           approve_buttons.disable();
           decline_buttons.disable();
       }
       else {
           if(abp.auth.isGranted('GrantApplicationManagement.Payments.Approve')) {
               approve_buttons.enable();
              
           }
           if(abp.auth.isGranted('GrantApplicationManagement.Payments.Decline')) {
               decline_buttons.enable();
           }
           
          
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
            getPaymentIdColumn(),
            getApplicantNameColumn(),
            getSupplierNumberColumn(),
            getSiteNumberColumn(),
            getContractNumberColumn(),
            getInvoiceNumberColumn(),
            getPayGroupColumn(),
            getAmountColumn(),
            getStatusColumn(),
            getDescriptionColumn(),
            getRequestedonColumn(),
            getUpdatedOnColumn(),
            getPaidOnColumn(),
            getCASResponseColumn(),
            getL1ApprovalColumn(),
            getL2ApprovalColumn(),
            getL3ApprovalColumn()
        ]
    }

    function getPaymentIdColumn() {
        return {
            title: l('ApplicationPaymentListTable:PaymentID'),
            name: 'id',
            data: 'id',
            className: 'data-table-header',
            index: 1,
        };
    }
    function getApplicantNameColumn() {
        return {
            title: l('ApplicationPaymentListTable:ApplicantName'),
            name: 'applicantName',
            data: 'payeeName',
            className: 'data-table-header',
            index: 2,
        };
    }

    function getSupplierNumberColumn() {
        return {
            title: l('ApplicationPaymentListTable:SupplierNumber'),
            name: 'supplierNumber',
            data: 'supplierNumber',
            className: 'data-table-header',
            index: 3,
        };
    }
    function getSiteNumberColumn() {
        return {
            title: l('ApplicationPaymentListTable:SiteNumber'),
            name: 'siteNumber',
            data: 'site',
            className: 'data-table-header',
            index: 4,
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
            index: 5,
 
        };
    }

    function getInvoiceNumberColumn() {
        return {
            title: l('ApplicationPaymentListTable:InvoiceNumber'),
            name: 'invoiceNumber',
            data: 'invoiceNumber',
            className: 'data-table-header',
            index: 6,
        };
    }
    function getPayGroupColumn() {
        return {
            title: l('ApplicationPaymentListTable:PayGroup'),
            name: 'payGroup',
            data: 'site',
            className: 'data-table-header',
            index: 7,
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
            className: 'data-table-header',
            index: 8,
        };
    }



    function getStatusColumn() {
        return {
            title: l('ApplicationPaymentListTable:Status'),
            name: 'status',
            data: 'status',
            className: 'data-table-header',
            index: 9,
            render: function (data) {
                switch (data) {
                    case 1:
                        return "Created";
                    case 2:
                        return "Submitted";
                    case 3:
                        return "Approved";
                    case 4:
                        return "Declined";
                    case 5:
                        return "Awaiting Approval";
                    case 6:
                        return "L1 Pending";
                    case 7:
                        return "L1 Approved";
                    case 8:
                        return "L1 Declined";
                    case 9:
                        return "L2 Pending";
                    case 10:
                        return "L2 Approved";
                    case 11:
                        return "L2 Declined";
                    case 12:
                        return "L3 Pending";
                    case 13:
                        return "L3 Approved";
                    case 14:
                        return "L3 Declined";
                    default:
                        return "Created";
                }
            }
        };
    }

    function getDescriptionColumn() {
        return {
            title: l('ApplicationPaymentListTable:Description'),
            name: 'description',
            data: 'description',
            className: 'data-table-header',
            index: 10,
        };
    }
    function getRequestedonColumn() {
        return {
            title: l('ApplicationPaymentListTable:RequestedOn'),
            name: 'requestedOn',
            data: 'creationTime',
            className: 'data-table-header',
            index: 11,
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
            index: 12,
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
            index: 13,
            render: function (data) {
                return formatDate(data);
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
            index: 14,
            render: function (data) {
                if(data+"" !== "undefined" && data.length > 0) {
                    return '<button class="btn btn-light info-btn" type="button" onclick="openCasResponseModal(\'' + data + '\');">View Response<i class="fl fl-mapinfo"></i></button>';
                }
                return  '{Not Available}';
            }
        };
    }
    function getL1ApprovalColumn() {
        return {
            title: l('ApplicationPaymentListTable:L1ApprovalDate'),
            name: 'l1Approval',
            data: 'expenseApprovals',
            className: 'data-table-header',
            index: 15,
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
            index: 14,
            render: function (data) {
                let approval = getExpenseApprovalsDetails(data, 3)
                return formatDate(approval?.decisionDate);
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



    $('#search').keyup(function () {
        let table = $('#PaymentRequestListTable').DataTable();
        table.search($(this).val()).draw();
    });

    paymentRequestStatusModal.onResult(function () {

        abp.notify.success(
            isApprove ? 'The payment request/s has been successfully approved' : 'The payment request/s has been successfully declined',
            'Payment Requests'
        );
        dataTable.ajax.reload(null, false);
        approve_buttons.disable();
        decline_buttons.disable();
        selectedPaymentIds = [];
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
