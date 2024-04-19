$(function () {
    const formatter = createNumberFormatter();
    const l = abp.localization.getResource('Payments');
    let dt = $('#BatchPaymentRequestTable');
    let dataTable;

    const listColumns = getColumns();
    const defaultVisibleColumns = ['select',
        'batchNumber',
        'totalAmount',
        'status',
        'requestedon',
        'l1ApprovalDate',
        'l2ApprovalDate',
        'paidOn'];

    dataTable = initializeDataTable(dt,
        defaultVisibleColumns,
        listColumns, 15, 4, unity.payments.batchPaymentRequests.batchPaymentRequest.getList, 'dynamicButtonContainerForPayment');

    dataTable.on('search.dt', () => handleSearch());

    dataTable.on('select', function (e, dt, type, indexes) {
        selectApplication(type, indexes, 'select_payment_application');
    });

    dataTable.on('deselect', function (e, dt, type, indexes) {
        selectApplication(type, indexes, 'deselect_payment_application');
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
            PubSub.publish("deselect_payment_application", "reset_data");
        }
    }

    function getColumns() {
        return [
            getSelectColumn('Select Batch'),
            getBatchNumberColumn(),
            getTotalAmountColumn(),
            getStatusColumn(),
            getRequestedonColumn(),
            getL1ApprovalDateColumn(),
            getL2ApprovalDateColumn(),
            getL3ApprovalDateColumn(),
            getPaidOnColumn(),
        ]
    }

    function getBatchNumberColumn() {
        return {
            title: l('ApplicationPaymentTable:BatchNumber'),
            name: 'batchNumber',
            data: 'batchNumber',
            className: 'data-table-header',
            index: 1,
        };
    }
    function getTotalAmountColumn() {
        return {
            title: l('ApplicationPaymentTable:TotalAmount'),
            name: 'totalAmount',
            data: 'paymentRequests',
            className: 'data-table-header',
            index: 2,
            render: function (data) {
                return formatter.format(getTotalRequestedAmount(data));
            }
        };
    }
    function getStatusColumn() {
        return {
            title: l('ApplicationPaymentTable:Status'),
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
    function getRequestedonColumn() {
        return {
            title: l('ApplicationPaymentTable:RequestedOn'),
            name: 'requestedon',
            data: 'creationTime',
            className: 'data-table-header',
            index: 4,
            render: function (data) {
                return formatDate(data);
            }
        };
    }
    function getL1ApprovalDateColumn() {
        return {
            title: l('ApplicationPaymentTable:L1ApprovalDate'),
            name: 'l1ApprovalDate',
            data: 'creationTime',
            className: 'data-table-header',
            index: 5,
            render: function (data) {
                return '';
            }
        };
    }
    function getL2ApprovalDateColumn() {
        return {
            title: l('ApplicationPaymentTable:L2ApprovalDate'),
            name: 'l2ApprovalDate',
            data: 'creationTime',
            className: 'data-table-header',
            index: 6,
            render: function (data) {
                return '';
            }
        };
    }
    function getL3ApprovalDateColumn() {
        return {
            title: l('ApplicationPaymentTable:L3ApprovalDate'),
            name: 'l3ApprovalDate',
            data: 'creationTime',
            className: 'data-table-header',
            index: 7,
            render: function (data) {
                return '';
            }
        };
    }
    function getPaidOnColumn() {
        return {
            title: l('Paid On'),
            name: 'paidOn',
            data: 'batchNumber',
            className: 'data-table-header',
            index: 8,
            render: function (data) {
                return '';
            }
        };

    }
    function getTotalRequestedAmount(data) {
        return data.reduce((n, { amount }) => n + amount, 0);
    }

    function formatDate(data) {
        return data != null ? luxon.DateTime.fromISO(data, {
            locale: abp.localization.currentCulture.name,
        }).toUTC().toLocaleString() : '{Not Available}';
    }

    window.addEventListener('resize', setTableHeighDynamic(dt.id));

    PubSub.subscribe(
        'refresh_application_list',
        (msg, data) => {
            dataTable.ajax.reload(null, false);
            PubSub.publish('clear_payment_application');
        }
    );
});
