$(function () {
    const l = abp.localization.getResource('Payments');
    let dt = $('#ApplicationPaymentRequestListTable');
    let dataTable;
    const listColumns = getColumns();
    const defaultVisibleColumns = [
        'id',
        'creationTime',
    ];

    let actionButtons = [
        {
            text: 'Edit & Resubmit',
            className: 'custom-table-btn flex-none btn btn-secondary',
            action: function (e, dt, node, config) {
                alert('Edit & Resubmit');
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
    let appId = document.getElementById('DetailsViewApplicationId').value;
    let inputAction = function (requestData, dataTableSettings) {
        const applicationId = appId
        return applicationId;
    }
    let responseCallback = function (result) {
        return {
            recordsTotal: result.length,
            recordsFiltered: result.length,
            data: result
        };
    };

    dataTable = initializeDataTable(dt,
        defaultVisibleColumns,
        listColumns, 15, 4, unity.payments.paymentRequests.paymentRequest.getListByApplicationId, inputAction, responseCallback, actionButtons, 'dynamicButtonContainerId');

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
            getAmountColumn(),
            getStatusColumn(),
            getRequestedonColumn(),
            getUpdatedOnColumn(),
            getPaidOnColumn(),
            getDescriptionColumn(),
            getCASResponseColumn(),
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
   

    function getAmountColumn() {
        return {
            title: l('ApplicationPaymentListTable:Amount'),
            name: 'amount',
            data: 'amount',
            className: 'data-table-header',
            index:2,
        };
    }



    function getStatusColumn() {
        return {
            title: l('ApplicationPaymentListTable:Status'),
            name: 'status',
            data: 'status',
            className: 'data-table-header',
            index: 3,
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
                        return "Awaiting Approval"
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
            index: 4,
        };
    }
    function getRequestedonColumn() {
        return {
            title: l('ApplicationPaymentListTable:RequestedOn'),
            name: 'requestedOn',
            data: 'creationTime',
            className: 'data-table-header',
            index: 5,
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
            index: 6,
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
            index: 7,
            render: function (data) {
                return formatDate(data);
            }
        };
    }
    function getCASResponseColumn() {
        return {
            title: l('ApplicationPaymentListTable:CASResponse'),
            name: 'cASResponse',
            data: 'casResponse',
            className: 'data-table-header',
            index: 8,
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

    $('#nav-payment-info-tab').one('click', function () {
        dataTable.columns.adjust();
    });

    $('#search').keyup(function () {
        let table = $('#ApplicationPaymentRequestListTable').DataTable();
        table.search($(this).val()).draw();
    });
});
