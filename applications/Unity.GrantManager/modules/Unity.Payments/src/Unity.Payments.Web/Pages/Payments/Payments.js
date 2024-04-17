$(function () {
    const formatter = createNumberFormatter();
    const l = abp.localization.getResource('Payments');
    const maxRowsPerPage = 15;
    let dt = $('#BatchPaymentRequestTable');
    let dataTable;
  

    const listColumns = getColumns();
    dataTable = initializeDataTable();

 
    dataTable.button().add(1, {
        text: 'Manage Columns',
        extend: 'collection',
        buttons: getColumnToggleButtonsSorted(),
        className: 'btn btn-light custom-table-btn cln-visible'
    });

    dataTable.buttons().container().prependTo('#dynamicButtonContainerForPayment');
    dataTable.on('search.dt', () => handleSearch());

   

    const UIElements = {
        searchBar: $('#search-bar'),
        btnToggleFilter: $('#btn-toggle-filter'),
    };
    init();
    function init() {
        $('.custom-table-btn').removeClass('dt-button buttons-csv buttons-html5');
        $('.csv-download').prepend('<i class="fl fl-export"></i>');
        $('.cln-visible').prepend('<i class="fl fl-settings"></i>');
        bindUIEvents();
        /* ClearFilter UIElements.clearFilter.html("<span class='x-mark'>X</span>" + UIElements.clearFilter.html()); */
        dataTable.search('').columns().search('').draw();
    }

    function bindUIEvents() {
        UIElements.btnToggleFilter.on('click', toggleFilterRow);
        /* ClearFilter UIElements.filterIcon.on('click', $('#dtFilterRow').toggleClass('hidden')); */
        /* ClearFilter UIElements.clearFilter.on('click', clearFilter); */
    }

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

    function toggleFilterRow() {
        $('#dtFilterRow').toggleClass('hidden');
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

    function createNumberFormatter() {
        return new Intl.NumberFormat('en-CA', {
            style: 'currency',
            currency: 'CAD',
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        });
    }

    function initializeDataTable() {
        return dt.DataTable(
            abp.libs.datatables.normalizeConfiguration({
                fixedHeader: {
                    header: true,
                    footer: false,
                    headerOffset: 0
                },
                serverSide: false,
                paging: true,
                order: [[4, 'desc']],
                searching: true,
                pageLength: maxRowsPerPage,
                scrollX: true,
                ajax: abp.libs.datatables.createAjax(
                    unity.payments.batchPaymentRequests.batchPaymentRequest.getList
                ),
                select: {
                    style: 'multiple',
                    selector: 'td:not(:nth-child(8))',
                },
                colReorder: true,
                orderCellsTop: true,
                //fixedHeader: true,
                stateSave: true,
                stateDuration: 0,
                dom: 'Bfrtip',
                buttons: [
                    {
                        extend: 'csv',
                        text: 'Export',
                        className: 'btn btn-light custom-table-btn csv-download',
                        exportOptions: {
                            columns: ':visible:not(.notexport)',
                            orthogonal: 'fullName',
                        }
                    }
                ],
                drawCallback: function () {
                    let $api = this.api();
                    let pages = $api.page.info().pages;
                    let rows = $api.data().length;

                    // Tailor the settings based on the row count
                    if (rows <= maxRowsPerPage) {
                        $('.dataTables_info').css('display', 'none');
                        $('.dataTables_paginate').css('display', 'none');
                        $('.dataTables_length').css('display', 'none');
                    } else if (pages === 1) {
                        // With this current length setting, not more than 1 page, hide pagination
                        $('.dataTables_info').css('display', 'none');
                        $('.dataTables_paginate').css('display', 'none');
                    } else {
                        // SHow everything
                        $('.dataTables_info').css('display', 'block');
                        $('.dataTables_paginate').css('display', 'block');
                    }
                    setTableHeighDynamic();
                },
                initComplete: function () {
                    updateFilter();
                },
                columns: listColumns,
                columnDefs: [
                    {
                        targets: getColumnsVisibleByDefault(),
                        visible: true
                    },
                    {
                        targets: '_all',
                        visible: false // Hide all other columns initially
                    }
                ],
            })
        );
    }

    function getColumnsVisibleByDefault() {
        const columnNames = ['select',
            'batchNumber',
            'totalAmount',
            'status',
            'requestedon',
            'l1ApprovalDate',
            'l2ApprovalDate',
            'paidOn'
        ];
        return columnNames
            .map((name) => {
                console.log(name, getColumnByName(name))
                return getColumnByName(name).index;
            });
    }

    function getColumnByName(name) {
        return listColumns.find(obj => obj.name === name);
    }

    function getColumnToggleButtonsSorted() {
        let exludeIndxs = [0];
        return listColumns
            .map((obj) => ({ title: obj.title, data: obj.data, visible: obj.visible, index: obj.index }))
            .filter(obj => !exludeIndxs.includes(obj.index))
            .sort((a, b) => a.title.localeCompare(b.title))
            .map(a => ({
                text: a.title,
                id: 'managecols-' + a.index,
                action: function (e, dt, node, config) {
                    toggleManageColumnButton(config);
                    if (isColumnVisToggled(a.title)) {
                        node.addClass('dt-button-active');
                    } else {
                        node.removeClass('dt-button-active');
                    }

                },
                className: 'dt-button dropdown-item buttons-columnVisibility' + isColumnVisToggled(a.title)
            }));
    }

    function isColumnVisToggled(title) {
        let column = findColumnByTitle(title);
        if (column.visible())
            return ' dt-button-active';
        else
            return null;
    }

    function toggleManageColumnButton(config) {
        let column = findColumnByTitle(config.text);
        column.visible(!column.visible());
    }

    function findColumnByTitle(title) {
        let columnIndex = dataTable
            .columns()
            .header()
            .map(c => $(c).text())
            .indexOf(title);
        return dataTable.column(columnIndex);
    }

    function getColumns() {
        return [
            getSelectColumn(),
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
    function getSelectColumn() {
        return {
            title: '<span class="btn btn-secondary btn-light fl fl-filter" title="Toggle Filter" id="btn-toggle-filter"></span>',
            orderable: false,
            className: 'notexport',
            data: 'rowCount',
            name: 'select',
            render: function (data) {
                return '<div class="select-checkbox" title="Select Application" ></div>';
            },
            index: 0
        }
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

    window.addEventListener('resize', setTableHeighDynamic);
    function setTableHeighDynamic() {
        let tableHeight = $("#BatchPaymentRequestTable")[0].clientHeight;
        let docHeight = document.body.clientHeight;
        let tableOffset = 425;

        if ((tableHeight + tableOffset) > docHeight) {
            $("#BatchPaymentRequestTable_wrapper .dataTables_scrollBody").css({ height: docHeight - tableOffset });
        } else {
            $("#BatchPaymentRequestTable_wrapper .dataTables_scrollBody").css({ height: tableHeight + 10 });
        }
    }
    dataTable.on('column-reorder.dt', function (e, settings) {
        updateFilter();
    });
    dataTable.on('column-visibility.dt', function (e, settings, deselectedcolumn, state) {
        updateFilter();
    });

    function updateFilter() {
        let optionsOpen = false;
        $("#tr-filter").each(function () {
            if ($(this).is(":visible"))
                optionsOpen = true;
        })
        $('.tr-toggle-filter').remove();
        let newRow = $("<tr class='tr-toggle-filter' id='tr-filter'>");

        dataTable
            .columns()
            .every(function () {
                let column = this;
                if (column.visible()) {
                    let title = column.header().textContent;
                    if (title) {
                        let newCell = $("<td>").append("<input type='text' class='form-control input-sm custom-filter-input' placeholder='" + title + "'>");
                        newCell.find("input").on("keyup", function () {
                            if (column.search() !== this.value) {
                                column.search(this.value).draw();
                            }
                        });

                        newRow.append(newCell);

                    }
                    else {
                        let newCell = $("<td>");
                        newRow.append(newCell);
                    }
                }


            });
        $("#BatchPaymentRequestTable thead").after(newRow);
        if (optionsOpen) {
            $(".tr-toggle-filter").show();
        }
    }

    PubSub.subscribe(
        'refresh_application_list',
        (msg, data) => {
            dataTable.ajax.reload(null, false);
            PubSub.publish('clear_payment_application');
        }
    );
});
