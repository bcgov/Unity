$(function () {
    const l = abp.localization.getResource('GrantManager');
    const defaultQuickDateRange = 'last6months';
    const FilterDesc = { Default: 'Filter', With_Filter: 'Filter*' };
    let recDt = null;
    let auditDt = null;
    let filterData = {};

    const UIElements = {
        reconciliationReportMenu: $('#reconciliation-report-menu-item'),
        reconciliationReportDiv: $('#reconciliation-report-div'),
        backgroundJobsMenu: $('#background-jobs-menu-item'),
        backgroundJobsDiv: $('#background-jobs-div'),
        auditLogsMenu: $('#audit-logs-menu-item'),
        auditLogsDiv: $('#audit-logs-div'),
        auditLogSettingsMenu: $('#audit-log-settings-menu-item'),
        auditLogSettingsDiv: $('#audit-log-settings-div'),
        aiPromptsMenu: $('#ai-prompts-menu-item'),
        aiPromptsDiv: $('#ai-prompts-div'),
        endpointsMenu: $('#endpoints-menu-item'),
        endpointsDiv: $('#endpoints-div'),
        exceptionLogsMenu: $('#exception-logs-menu-item'),
        exceptionLogsDiv: $('#exception-logs-div'),
        auditTable: $('#AuditLogsTable'),
        auditSearch: $('#audit-search'),
        auditEntityType: $('#audit-entity-type'),
        auditChangeType: $('#audit-change-type'),
        auditQuickDateRange: $('#audit-quick-date-range'),
        auditCustomDateInputs: $('#audit-custom-date-inputs'),
        auditFromDate: $('#audit-from-date'),
        auditToDate: $('#audit-to-date'),
        auditFilterButton: $('#audit-filter-button'),
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
        initializeAuditDateFilters();
        initializeAuditDataTable();
        loadAuditEntityTypes();
    }

    function bindUIElements() {
        UIElements.reconciliationReportMenu.on('click', menuItemClick);
        UIElements.backgroundJobsMenu.on('click', menuItemClick);
        UIElements.auditLogsMenu.on('click', menuItemClick);
        UIElements.auditLogSettingsMenu.on('click', menuItemClick);
        UIElements.aiPromptsMenu.on('click', menuItemClick);
        UIElements.endpointsMenu.on('click', menuItemClick);
        UIElements.exceptionLogsMenu.on('click', menuItemClick);
        UIElements.tenantFilter.on('change', handleTenantChange);
        UIElements.quickDateRange.on('change', handleQuickDateRangeChange);
        UIElements.submittedFromDate.on('change', handleCustomDateChange);
        UIElements.submittedToDate.on('change', handleCustomDateChange);
        UIElements.auditQuickDateRange.on('change', handleAuditQuickDateRangeChange);
        UIElements.auditFromDate.on('change', handleAuditDateChange);
        UIElements.auditToDate.on('change', handleAuditDateChange);
        UIElements.auditEntityType.on('change', reloadAuditTable);
        UIElements.auditChangeType.on('change', reloadAuditTable);
        UIElements.auditSearch.on('input', handleAuditSearchInput);
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
        UIElements.auditLogsMenu.removeClass('active');
        UIElements.auditLogSettingsMenu.removeClass('active');
        UIElements.aiPromptsMenu.removeClass('active');
        UIElements.endpointsMenu.removeClass('active');
        UIElements.exceptionLogsMenu.removeClass('active');
    }

    function hideAllContentSections() {
        UIElements.reconciliationReportDiv.addClass('hide');
        UIElements.backgroundJobsDiv.addClass('hide');
        UIElements.auditLogsDiv.addClass('hide');
        UIElements.auditLogSettingsDiv.addClass('hide');
        UIElements.aiPromptsDiv.addClass('hide');
        UIElements.endpointsDiv.addClass('hide');
        UIElements.exceptionLogsDiv.addClass('hide');
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
        } else if ($(e.currentTarget).attr('id') === 'audit-logs-menu-item') {
            UIElements.auditLogsDiv.removeClass('hide');
            if (auditDt) {
                auditDt.columns.adjust().draw(false);
            }
        } else if ($(e.currentTarget).attr('id') === 'audit-log-settings-menu-item') {
            UIElements.auditLogSettingsDiv.removeClass('hide');
        } else if ($(e.currentTarget).attr('id') === 'ai-prompts-menu-item') {
            UIElements.aiPromptsDiv.removeClass('hide');
            adjustVisibleDataTables();
        } else if ($(e.currentTarget).attr('id') === 'endpoints-menu-item') {
            UIElements.endpointsDiv.removeClass('hide');
            adjustVisibleDataTables();
        } else if ($(e.currentTarget).attr('id') === 'exception-logs-menu-item') {
            UIElements.exceptionLogsDiv.removeClass('hide');
            adjustVisibleDataTables();
        }
    }

    function adjustVisibleDataTables() {
        $.fn.dataTable.tables({ visible: true, api: true }).columns.adjust().draw(false);
    }

    // Size the scroll body so the table footer sits at the bottom of the viewport,
    // matching initializeDataTable({ fixedHeaders: true }) used by the other admin tables.
    function enableScrollResize(dt) {
        if ($.fn.dataTable.ScrollResize) {
            dt.settings()[0]._scrollResize = new $.fn.dataTable.ScrollResize(dt);
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
                scrollY: 'calc(100vh - 325px)', // Initial value – ScrollResize recalculates dynamically
                scrollCollapse: true,
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

        enableScrollResize(recDt);

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

    function initializeAuditDateFilters() {
        const range = getDateRange('last6months');
        setAuditDateRange(range);
        const today = formatDate(new Date());
        UIElements.auditToDate.attr('max', today);
        UIElements.auditFromDate.attr('max', today);
    }

    function setAuditDateRange(range) {
        UIElements.auditFromDate.val(range?.fromDate ?? '');
        UIElements.auditToDate.val(range?.toDate ?? '');
    }

    function getAuditDateFilters() {
        return {
            startTime: UIElements.auditFromDate.val() ? new Date(`${UIElements.auditFromDate.val()}T00:00:00.000`).toISOString() : null,
            endTime: UIElements.auditToDate.val() ? new Date(`${UIElements.auditToDate.val()}T23:59:59.999`).toISOString() : null
        };
    }

    function handleAuditQuickDateRangeChange() {
        const selectedRange = $(this).val();
        if (selectedRange === 'custom') {
            UIElements.auditCustomDateInputs.show();
        } else {
            UIElements.auditCustomDateInputs.hide();
            setAuditDateRange(getDateRange(selectedRange));
        }
        loadAuditEntityTypes();
        reloadAuditTable();
    }

    function handleAuditDateChange() {
        UIElements.auditQuickDateRange.val('custom');
        UIElements.auditCustomDateInputs.show();
        loadAuditEntityTypes();
        reloadAuditTable();
    }

    // Client-side only: hides loaded rows that don't contain the search text, without a server round trip
    function handleAuditSearchInput() {
        if (!auditDt) {
            return;
        }

        const term = (UIElements.auditSearch.val() || '').trim().toLowerCase();
        const rows = $(auditDt.rows({ page: 'current' }).nodes());
        let visibleCount = 0;
        rows.each(function () {
            const isMatch = !term || $(this).text().toLowerCase().includes(term);
            $(this).toggle(isMatch);
            if (isMatch) {
                visibleCount++;
            }
        });

        // Paging and totals come from the server, so describe the search result in terms of the current page
        const container = $(auditDt.table().container());
        const info = container.find('.dt-info, .dataTables_info');
        info.find('.audit-search-info').remove();
        info.children().not('.audit-search-info').show();

        const body = $(auditDt.table().body());
        body.find('.audit-search-empty').remove();

        if (!term) {
            return;
        }

        if (info.children().length === 0) {
            info.wrapInner('<span></span>');
        }
        // Use the table's own language strings so the info and empty row read like a standard DataTables search
        const settings = auditDt.settings()[0];
        const lang = settings.oLanguage;
        info.children().hide();
        info.append($('<span>', {
            class: 'audit-search-info',
            text: formatAuditSearchInfo(settings, visibleCount ? lang.sInfo : lang.sInfoEmpty, visibleCount)
                + ' ' + formatAuditSearchInfo(settings, lang.sInfoFiltered, visibleCount, rows.length)
        }));

        if (visibleCount === 0 && rows.length > 0) {
            body.append($('<tr>', { class: 'audit-search-empty' }).append($('<td>', {
                class: 'dt-empty',
                colspan: auditDt.columns(':visible').count(),
                text: lang.sZeroRecords
            })));
        }
    }

    // Mirrors DataTables' own info macros, scoped to the rows on the current page
    function formatAuditSearchInfo(settings, template, count, max) {
        const total = max ?? count;
        const format = number => settings.fnFormatNumber(number);
        const entries = number => settings.api.i18n('entries', '', number);
        return (template || '')
            .replaceAll('_START_', format(count ? 1 : 0))
            .replaceAll('_END_', format(count))
            .replaceAll('_MAX_', format(total))
            .replaceAll('_TOTAL_', format(count))
            .replaceAll('_ENTRIES_', entries(count))
            .replaceAll('_ENTRIES-MAX_', entries(total))
            .replaceAll('_ENTRIES-TOTAL_', entries(count))
            .trim();
    }

    function reloadAuditTable() {
        if (auditDt) {
            auditDt.ajax.reload(null, true);
        }
    }

    function loadAuditEntityTypes() {
        unity.grantManager.history.auditLog.getEntityTypeFullNames(getAuditDateFilters()).then(function (types) {
            UIElements.auditEntityType.find('option:not(:first)').remove();
            types.forEach(function (type) {
                UIElements.auditEntityType.append($('<option>', { value: type, text: type }));
            });
        });
    }

    function initializeAuditDataTable() {
        if ($.fn.dataTable.isDataTable(UIElements.auditTable[0])) {
            auditDt = UIElements.auditTable.DataTable();
            return;
        }

        auditDt = UIElements.auditTable.DataTable(
            abp.libs.datatables.normalizeConfiguration({
                serverSide: true,
                paging: true,
                order: [[0, 'desc']],
                searching: false,
                scrollX: true,
                scrollY: 'calc(100vh - 325px)', // Initial value – ScrollResize recalculates dynamically
                scrollCollapse: true,
                ajax: abp.libs.datatables.createAjax(
                    unity.grantManager.history.auditLog.getList,
                    function () {
                        const dates = getAuditDateFilters();
                        return {
                            startTime: dates.startTime,
                            endTime: dates.endTime,
                            entityTypeFullName: UIElements.auditEntityType.val() || null,
                            changeType: UIElements.auditChangeType.val() ? Number(UIElements.auditChangeType.val()) : null,
                            propertyName: getAuditColumnSearch(2),
                            serviceName: getAuditColumnSearch(7),
                            methodName: getAuditColumnSearch(8)
                        };
                    }
                ),
                columnDefs: [
                    { title: 'Change time', data: 'changeTime', render: formatAuditDate },
                    { title: 'Entity', data: 'entityName', name: 'entityName', searchable: true, orderable: false, render: formatAuditEntityLink },
                    { title: 'Property', data: 'propertyName', render: $.fn.dataTable.render.text() },
                    { title: 'Original value', data: 'originalValue', orderable: false, render: $.fn.dataTable.render.text() },
                    { title: 'New value', data: 'newValue', orderable: false, render: $.fn.dataTable.render.text() },
                    { title: 'Change', data: 'changeType', render: formatChangeType },
                    { title: 'User', data: 'userFirstName', render: formatAuditUser },
                    { title: 'Service', data: 'serviceName', orderable: false, render: $.fn.dataTable.render.text() },
                    { title: 'Method', data: 'methodName', orderable: false, render: $.fn.dataTable.render.text() },
                    { title: 'URL', data: 'url', orderable: false, render: $.fn.dataTable.render.text() }
                ],
                processing: true
            })
        );

        enableScrollResize(auditDt);

        if ($.fn.dataTable.FilterRow !== undefined) {
            new $.fn.dataTable.FilterRow(auditDt.settings()[0], { // NOSONAR - False positive flag on S1848
                buttonId: 'audit-filter-button',
                buttonText: FilterDesc.Default,
                buttonTextActive: FilterDesc.With_Filter,
                enablePopover: $.fn.popover !== undefined
            });
        }

        // Re-apply the search to each newly loaded page
        auditDt.on('draw', handleAuditSearchInput);
    }

    function getAuditColumnSearch(index) {
        return auditDt ? auditDt.column(index).search() || null : null;
    }

    function formatAuditUser(data, renderType, row) {
        const fullName = [row?.userFirstName, row?.userSurname].filter(Boolean).join(' ');
        return $('<span>', { text: fullName }).prop('outerHTML');
    }

    function formatAuditDate(data) {
        const formattedDate = data
            ? luxon.DateTime.fromISO(data, { locale: abp.localization.currentCulture.name }).toLocaleString(luxon.DateTime.DATETIME_MED)
            : '';
        return $('<span>', { class: 'audit-change-time', text: formattedDate }).prop('outerHTML');
    }

    function formatAuditEntityLink(data, renderType, row) {
        const entityId = String(row?.entityId ?? '').trim();
        const entityType = String(row?.entityTypeFullName ?? '');
        const entityName = String(data ?? row?.entityName ?? '').trim() || getAuditEntityName(entityType);
        if (!entityId) {
            return $('<span>', { text: entityName }).prop('outerHTML');
        }

        if (entityId.toLowerCase() === '00000000-0000-0000-0000-000000000000') {
            return $('<span>', { text: entityName }).prop('outerHTML');
        }

        let href = String(row?.entityUrl ?? '').trim() || null;

        if (!href && entityType.endsWith('.Applicant')) {
            href = '/Applicants/Details?ApplicantId=' + encodeURIComponent(entityId);
        }

        if (!href) {
            return $('<span>', {
                text: entityName,
                title: entityId
            }).prop('outerHTML');
        }

        return $('<a>', {
            href: href,
            text: entityName,
            title: entityType
        }).prop('outerHTML');
    }

});

function formatChangeType(data) {
    return ['Created', 'Updated', 'Deleted'][data] ?? data;
}

function getAuditEntityName(entityType) {
    const shortName = entityType.split('.').pop() || 'Entity';
    return shortName
        .replace(/([a-z])([A-Z])/g, '$1 $2')
        .replace(/Dto$/, '')
        .trim();
}

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
