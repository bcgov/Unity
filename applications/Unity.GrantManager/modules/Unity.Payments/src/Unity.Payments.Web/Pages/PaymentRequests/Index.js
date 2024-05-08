$(function () {  
    const l = abp.localization.getResource('Payments');
    let dt = $('#PaymentRequestListTable');
    let dataTable;
    const listColumns = getColumns();
    const defaultVisibleColumns = [
        'id',
        'creationTime',
    ];

    let actionButtons = [
        {
            extend: 'csv',
            text: 'Export',
            className: 'custom-table-btn flex-none btn btn-secondary',
            exportOptions: {
                columns: ':visible:not(.notexport)',
                orthogonal: 'fullName',
            }
        },
        {
            text: 'Approve',
            className: 'custom-table-btn flex-none btn btn-secondary',
            action: function (e, dt, node, config) {
                alert('Approve Button activated');
            }
        },
        {
            text: 'Decline',
            className: 'custom-table-btn flex-none btn btn-secondary',
            action: function (e, dt, node, config) {
                alert('Decline Button activated');
            }
        },
        {
            text: 'History',
            className: 'custom-table-btn flex-none btn btn-secondary',
            action: function (e, dt, node, config) {
                alert('History Button activated');
            }
        }
    ];

    dataTable = initializeDataTable(dt,
        defaultVisibleColumns,
        listColumns, 15, 4, unity.payments.paymentRequests.paymentRequest.getList, {}, actionButtons, 'dynamicButtonContainerId');

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
            getContactNumberColumn(),
            getInvoiceNumberColumn(),
            getPayGroupColumn(),
            getAmountColumn(),
            getStatusColumn(),
            getDescriptionColumn(),
            getRequestedonColumn(),
            getUpdatedOnColumn(),
            getPaidOnColumn(),
            getCASCommentsColumn(),
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
            data: 'applicantName',
            className: 'data-table-header',
            index: 2,
            render: function (data) {
                return '';
            }
        };
    }

    function getSupplierNumberColumn() {
        return {
            title: l('ApplicationPaymentListTable:SupplierNumber'),
            name: 'supplierNumber',
            data: 'supplierNumber',
            className: 'data-table-header',
            index: 3,
            render: function (data) {
                return '';
            }
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
    function getContactNumberColumn() {
        return {
            title: l('ApplicationPaymentListTable:ContactNumber'),
            name: 'contactNumber',
            data: 'contactNumber',
            className: 'data-table-header',
            index: 5,
            render: function (data) {
                return '';
            }
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
            data: 'payGroup',
            className: 'data-table-header',
            index: 7,
            render: function (data) {
                return '';
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
            index: 12,
            render: function (data) {
                return formatDate(data);
            }
        };
    }
    function getCASCommentsColumn() {
        return {
            title: l('ApplicationPaymentListTable:CASComments'),
            name: 'cASComments',
            data: 'casComments',
            className: 'data-table-header',
            index: 12,
            render: function (data) {
                return formatDate(data);
            }
        };
    }
  

    function formatDate(data) {
        return data != null ? luxon.DateTime.fromISO(data, {
            locale: abp.localization.currentCulture.name,
        }).toUTC().toLocaleString() : '{Not Available}';
    }

    /* the resizer needs looking at again after ux2 refactor 
     window.addEventListener('resize', setTableHeighDynamic('PaymentRequestListTable'));
    */

    PubSub.subscribe(
        'refresh_application_list',
        (msg, data) => {
            dataTable.ajax.reload(null, false);
            PubSub.publish('clear_payment_application');
        }
    );
});
