$(function () {
    const l = abp.localization.getResource('GrantManager');
    const defaultQuickDateRange = 'last6months';
    const FilterDesc = { Default: 'Filter', With_Filter: 'Filter*' };
    let recDt = null;
    let auditDt = null;
    let filterData = {};
    let auditSearchTimer = null;

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

    function handleAuditSearchInput() {
        clearTimeout(auditSearchTimer);
        auditSearchTimer = setTimeout(reloadAuditTable, 300);
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
                ajax: abp.libs.datatables.createAjax(
                    unity.grantManager.history.auditLog.getList,
                    function () {
                        const dates = getAuditDateFilters();
                        return {
                            startTime: dates.startTime,
                            endTime: dates.endTime,
                            entityTypeFullName: UIElements.auditEntityType.val() || null,
                            changeType: UIElements.auditChangeType.val() ? Number(UIElements.auditChangeType.val()) : null,
                            filter: UIElements.auditSearch.val() || null,
                            propertyName: getAuditColumnSearch(2),
                            serviceName: getAuditColumnSearch(8),
                            methodName: getAuditColumnSearch(9)
                        };
                    }
                ),
                columnDefs: [
                    { title: 'Change time', data: 'changeTime', render: formatAuditDate },
                    { title: 'Entity', data: 'entityName', name: 'entityName', searchable: true, orderable: false, render: formatAuditEntityLink },
                    { title: 'Property', data: 'propertyName', render: $.fn.dataTable.render.text() },
                    { title: 'Original value', data: 'originalValue', render: $.fn.dataTable.render.text() },
                    { title: 'New value', data: 'newValue', render: $.fn.dataTable.render.text() },
                    { title: 'Change', data: 'changeType', render: formatChangeType },
                    { title: 'Name', data: 'userFirstName', render: $.fn.dataTable.render.text() },
                    { title: 'Surname', data: 'userSurname', render: $.fn.dataTable.render.text() },
                    { title: 'Service', data: 'serviceName', render: $.fn.dataTable.render.text() },
                    { title: 'Method', data: 'methodName', render: $.fn.dataTable.render.text() },
                    { title: 'URL', data: 'url', render: $.fn.dataTable.render.text() }
                ],
                processing: true
            })
        );

        if ($.fn.dataTable.FilterRow !== undefined) {
            new $.fn.dataTable.FilterRow(auditDt.settings()[0], { // NOSONAR - False positive flag on S1848
                buttonId: 'audit-filter-button',
                buttonText: FilterDesc.Default,
                buttonTextActive: FilterDesc.With_Filter,
                enablePopover: $.fn.popover !== undefined
            });
        }

    }

    function getAuditColumnSearch(index) {
        return auditDt ? auditDt.column(index).search() || null : null;
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
