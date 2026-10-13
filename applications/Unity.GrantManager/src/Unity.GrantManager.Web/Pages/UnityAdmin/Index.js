$(function () {
    const l = abp.localization.getResource('GrantManager');
    const defaultQuickDateRange = 'last6months';
    const FilterDesc = { Default: 'Filter', With_Filter: 'Filter*' };
    let recDt = null;
    let filterData = {};

    const UIElements = {
        reconciliationReportMenu: $('#reconciliation-report-menu-item'),
        reconciliationReportDiv: $('#reconciliation-report-div'),
        backgroundJobsMenu: $('#background-jobs-menu-item'),
        backgroundJobsDiv: $('#background-jobs-div'),
        reconciliationTable: $('#ReconciliationTable'),
        tenantFilter: $('#ReconciliationTenantFilter'),
        quickDateRange: $('#quickDateRange'),
        customDateInputs: $('#customDateInputs'),
        submittedFromDate: $('#submittedFromDate'),
        submittedToDate: $('#submittedToDate'),
        reconcileButton: $('#btn-reconcile'),
    };

    init();

    function init() {
        initializeDateFilters();
        bindUIElements();
        initializeDataTable();
    }

    function bindUIElements() {
        UIElements.reconciliationReportMenu.on('click', menuItemClick);
        UIElements.backgroundJobsMenu.on('click', menuItemClick);
        UIElements.tenantFilter.on('change', handleTenantChange);
        UIElements.quickDateRange.on('change', handleQuickDateRangeChange);
        UIElements.submittedFromDate.on('change', handleCustomDateChange);
        UIElements.submittedToDate.on('change', handleCustomDateChange);
        UIElements.reconcileButton.on('click', handleReconcileClick);
        // Delegated: scrollX clones the header, so the select-all checkbox is not a stable element
        $(document).on('click', '.select-all-reconciliation', function () {
            if ($(this).is(':checked')) {
                recDt.rows({ page: 'current', search: 'applied' }).select();
            } else {
                recDt.rows({ page: 'current', search: 'applied' }).deselect();
            }
        });
    }

    // ── Reconcile ──
    // Enables Reconcile only when rows are selected and keeps the header checkbox in step with the current page
    function syncSelectionState() {
        const selectedCount = recDt.rows({ selected: true }).count();
        const pageRows = recDt.rows({ page: 'current', search: 'applied' });
        const pageSelectedCount = recDt.rows({ page: 'current', search: 'applied', selected: true }).count();

        UIElements.reconcileButton.prop('disabled', selectedCount === 0);
        $('.select-all-reconciliation').prop('checked', pageRows.count() > 0 && pageSelectedCount === pageRows.count());
    }

    // Mirrors DataTables row selection onto the row checkboxes rendered by getSelectColumn
    function setRowCheckboxes(indexes, checked) {
        indexes.forEach(index => {
            $(recDt.row(index).node()).find('.chkbox').prop('checked', checked);
        });
    }

    // Same rule as the Status column: only submitted CHEFS submissions are Missing, everything else is a Draft
    function isMissingSubmission(row) {
        return row.formSubmissionStatusCode === 'SUBMITTED';
    }

    // Only Missing rows are reconciled; selected Drafts are skipped and reported back to the user
    function handleReconcileClick() {
        const selected = recDt.rows({ selected: true }).data().toArray();
        if (selected.length === 0) {
            return;
        }

        const missing = selected.filter(isMissingSubmission);
        const drafts = selected.filter(row => !isMissingSubmission(row));
        const draftIds = drafts.map(row => row.confirmationId).join(', ');

        if (missing.length === 0) {
            abp.notify.warn(l('Reconciliation:DraftsNotReconciled', drafts.length, draftIds));
            return;
        }

        let confirmMessage = l('Reconciliation:ConfirmReconcile', missing.length);
        if (drafts.length > 0) {
            confirmMessage += ' ' + l('Reconciliation:DraftsWillBeSkipped', drafts.length);
        }

        abp.message.confirm(confirmMessage, null, function (confirmed) {
            if (confirmed) {
                reconcileSubmissions(missing, drafts.length > 0 ? l('Reconciliation:DraftsNotReconciled', drafts.length, draftIds) : null);
            }
        });
    }

    // Sends the selected CHEFS submissions to the server, which runs each one through intake
    // (same as the CHEFS webhook), then reloads the list so reconciled rows drop off.
    // draftsMessage, when set, is shown alongside the results to report skipped Drafts.
    function reconcileSubmissions(rows, draftsMessage) {
        const input = {
            tenantName: UIElements.tenantFilter.val(),
            submissions: rows.map(row => ({
                submissionId: row.submissionId,
                formId: row.formId,
                formVersionId: row.formVersionId,
                confirmationId: row.confirmationId
            }))
        };

        abp.ui.setBusy('#reconciliation-report-div');
        unity.grantManager.intakes.submission.reconcileSubmissions(input)
            .then(showReconcileResults)
            .always(function () {
                if (draftsMessage) {
                    abp.notify.warn(draftsMessage);
                }
                abp.ui.clearBusy('#reconciliation-report-div');
                runRecTableReload();
            });
    }

    // One success toast listing reconciled Submission #s, one warning toast listing failures with reasons
    function showReconcileResults(results) {
        const reconciled = results.filter(r => r.success);
        const failed = results.filter(r => !r.success);

        if (reconciled.length > 0) {
            abp.notify.success(
                l('Reconciliation:Reconciled', reconciled.length, reconciled.map(r => r.confirmationId).join(', '))
            );
        }

        if (failed.length > 0) {
            abp.notify.warn(
                l('Reconciliation:NotReconciled', failed.length,
                    failed.map(r => r.confirmationId + ' (' + r.message + ')').join(', '))
            );
        }

        if (reconciled.length === 0 && failed.length === 0) {
            abp.notify.info(l('Reconciliation:NothingReconciled'));
        }
    }

    // ── Side menu ──
    function removeActiveClassFromMenuItems() {
        UIElements.reconciliationReportMenu.removeClass('active');
        UIElements.backgroundJobsMenu.removeClass('active');
    }

    function hideAllContentSections() {
        UIElements.reconciliationReportDiv.addClass('hide');
        UIElements.backgroundJobsDiv.addClass('hide');
    }

    function menuItemClick(e) {
        removeActiveClassFromMenuItems();
        hideAllContentSections();
        $(e.currentTarget).addClass('active');

        if ($(e.currentTarget).attr('id') === 'reconciliation-report-menu-item') {
            UIElements.reconciliationReportDiv.removeClass('hide');
            if (recDt) {
                recDt.columns.adjust().draw();
            }
        } else if ($(e.currentTarget).attr('id') === 'background-jobs-menu-item') {
            UIElements.backgroundJobsDiv.removeClass('hide');
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
                order: [[1, 'asc']],
                searching: true,
                select: {
                    style: 'multiple',
                    selector: 'td'
                },
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
                    getSelectColumn(l('Reconciliation:SelectSubmission'), 'submissionId', 'reconciliation'),
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
                            return isMissingSubmission(row) ? 'Missing' : 'Draft';
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

        recDt.on('select', function (e, dt, type, indexes) {
            if (type === 'row') {
                setRowCheckboxes(indexes, true);
                syncSelectionState();
            }
        });

        recDt.on('deselect', function (e, dt, type, indexes) {
            if (type === 'row') {
                setRowCheckboxes(indexes, false);
                syncSelectionState();
            }
        });

        recDt.on('draw', syncSelectionState);

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
