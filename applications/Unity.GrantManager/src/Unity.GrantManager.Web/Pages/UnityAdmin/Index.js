$(function () {
    const l = abp.localization.getResource('GrantManager');
    const defaultQuickDateRange = 'last6months';
    const FilterDesc = { Default: 'Filter', With_Filter: 'Filter*' };
    let recDt = null;
    let filterData = {};

    const UIElements = {
        reconciliationReportMenu: $('#reconciliation-report-menu-item'),
        reconciliationReportDiv: $('#reconciliation-report-div'),
        reconciliationTable: $('#ReconciliationTable'),
        tenantFilter: $('#ReconciliationTenantFilter'),
        quickDateRange: $('#quickDateRange'),
        customDateInputs: $('#customDateInputs'),
        submittedFromDate: $('#submittedFromDate'),
        submittedToDate: $('#submittedToDate'),
    };

    init();

    function init() {
        initializeDateFilters();
        bindUIElements();
        initializeDataTable();
    }

    function bindUIElements() {
        UIElements.reconciliationReportMenu.on('click', menuItemClick);
        UIElements.tenantFilter.on('change', handleTenantChange);
        UIElements.quickDateRange.on('change', handleQuickDateRangeChange);
        UIElements.submittedFromDate.on('change', handleCustomDateChange);
        UIElements.submittedToDate.on('change', handleCustomDateChange);
    }

    // ── Side menu ──
    function removeActiveClassFromMenuItems() {
        UIElements.reconciliationReportMenu.removeClass('active');
    }

    function menuItemClick(e) {
        removeActiveClassFromMenuItems();
        $(e.currentTarget).addClass('active');

        UIElements.reconciliationReportDiv.addClass('hide');
        UIElements.reconciliationReportDiv.removeClass('hide');

        if (recDt) {
            recDt.columns.adjust().draw();
        }
    }

    function initializeDateFilters() {
        let range = getDateRange(defaultQuickDateRange);
        setDateRangeFilters(defaultQuickDateRange, range);

        let today = formatDate(new Date());
        UIElements.submittedToDate.attr('max', today);
        UIElements.submittedFromDate.attr('max', today);
    }

    function runRecTableReload() {
        const dt = UIElements.reconciliationTable.DataTable();
        dt.ajax.reload(null, true);
    }

    function handleTenantChange() {
        runRecTableReload();
    }

    function handleQuickDateRangeChange() {
        let selectedRange = $(this).val();

        if (selectedRange === 'custom') {
            toggleCustomDateInputs(true);
            return;
        }

        toggleCustomDateInputs(false);
        let range = getDateRange(selectedRange);
        setDateRangeFilters(selectedRange, range);
        runRecTableReload();
    }

    function handleCustomDateChange() {
        UIElements.quickDateRange.val('custom');
        toggleCustomDateInputs(true);
        runRecTableReload();
    }



    function setDateRangeFilters(quickDateRange, range) {
        UIElements.quickDateRange.val(quickDateRange);

        if (range) {
            UIElements.submittedFromDate.val(range.fromDate ?? '');
            UIElements.submittedToDate.val(range.toDate ?? '');
        }
    }

    function toggleCustomDateInputs(show) {
        if (show) {
            UIElements.customDateInputs.show();
        } else {
            UIElements.customDateInputs.hide();
        }
    }
    function getActiveDateFilters() {
        let fromVal = UIElements.submittedFromDate.val();
        let toVal = UIElements.submittedToDate.val();
        return {
            dateFrom: fromVal ? new Date(fromVal) : null,
            dateTo: toVal ? new Date(toVal) : null
        };
    }

    function initializeDataTable() {
        recDt = $('#ReconciliationTable').DataTable(
            abp.libs.datatables.normalizeConfiguration({
                serverSide: false,
                paging: true,
                order: [[0, 'asc']],
                searching: true,
                externalSearchInputId: '#search',
                scrollX: true,
                ajax: abp.libs.datatables.createAjax(
                    unity.grantManager.intakes.submission.getSubmissionsList,
                    function () {
                        let dates = getActiveDateFilters();
                        return {
                            returnAllSubmissions: false,
                            tenantName: UIElements.tenantFilter.val() || null,
                            dateFrom: dates.dateFrom ? dates.dateFrom.toISOString() : null,
                            dateTo: dates.dateTo ? dates.dateTo.toISOString() : null
                        };
                    }
                ),
                columnDefs: [
                    {
                        title: l('Submission #'),
                        data: 'confirmationId',
                        render: function (data) {
                            return data;
                        }
                    },
                    {
                        title: l('Submitter'),
                        data: 'createdBy',
                        render: function (data) {
                            return data;
                        }
                    },
                    {
                        title: l('Chefs Form Name'),
                        data: 'form',
                        render: function (data) {
                            return data;
                        }
                    },
                    {
                        title: l('Category'),
                        data: 'category',
                        render: function (data) {
                            return data;
                        }
                    },
                    {
                        title: l('Created Date'),
                        data: 'createdAt',
                        render: function (data) {
                            return luxon
                                .DateTime
                                .fromISO(data, {
                                    locale: abp.localization.currentCulture.name
                                }).toLocaleString();
                        }
                    },
                    {
                        title: l('GrantApplicationStatus'),
                        data: 'status',
                        render: function (data, type, row) {
                            return (row.formSubmissionStatusCode === 'SUBMITTED') ? 'Missing' : 'Draft';
                        }
                    },
                ],
                processing: true,
                stateSaveParams: function (settings, data) {
                    let searchValue = $(settings.oInit.externalSearchInputId).val();
                    data.search.search = searchValue;

                    let hasFilter = data.columns.some(value => value.search.search !== '') || searchValue !== '';
                    $('#btn-toggle-filter').text(hasFilter ? FilterDesc.With_Filter : FilterDesc.Default);
                },
                stateLoadParams: function (settings, data) {
                    $(settings.oInit.externalSearchInputId).val(data.search.search);

                    data.columns.forEach((column, index) => {
                        if (settings.aoColumns[index] + '' != 'undefined') {
                            const title = settings.aoColumns[index].sTitle;
                            const value = column.search.search;
                            filterData[title] = value;
                        }
                    });
                }
            })
        );

        // Initialize FilterRow plugin on the button
        if ($.fn.dataTable.FilterRow !== 'undefined') {
            new $.fn.dataTable.FilterRow(recDt.settings()[0], { // NOSONAR - False positive flag on S1848
                buttonId: 'btn-toggle-filter',
                buttonText: FilterDesc.Default,
                buttonTextActive: FilterDesc.With_Filter,
                enablePopover: $.fn.popover !== 'undefined'
            });
        }

        $('#search').on('input', function () {
            let table = $('#ReconciliationTable').DataTable();
            table.search($(this).val()).draw();
        });
    }
});

function formatDate(date) {
    let year = date.getFullYear();
    let month = String(date.getMonth() + 1).padStart(2, '0');
    let day = String(date.getDate()).padStart(2, '0');
    return year + '-' + month + '-' + day;
}

function getDateRange(rangeType) {
    let today = new Date();
    let toDate = formatDate(new Date());
    let fromDate;

    switch (rangeType) {
        case 'today':
            fromDate = toDate;
            break;
        case 'last7days':
            fromDate = formatDate(new Date(today.setDate(today.getDate() - 7)));
            break;
        case 'last30days':
            fromDate = formatDate(new Date(today.setDate(today.getDate() - 30)));
            break;
        case 'last3months':
            fromDate = formatDate(new Date(today.setMonth(today.getMonth() - 3)));
            break;
        case 'last6months':
            fromDate = formatDate(new Date(today.setMonth(today.getMonth() - 6)));
            break;
        case 'alltime':
            return { fromDate: null, toDate: null };
        case 'custom':
        default:
            return null;
    }

    return { fromDate, toDate };
}
