$(function () {
    const entityType = $('#audit-entity-type');
    const changeType = $('#audit-change-type');
    const quickDateRange = $('#audit-quick-date-range');
    const customDateInputs = $('#audit-custom-date-inputs');
    const fromDate = $('#audit-from-date');
    const toDate = $('#audit-to-date');
    const search = $('#audit-search');
    let auditTable;

    initializeDates();
    initializeTable();
    loadEntityTypes();

    entityType.add(changeType).on('change', reloadTable);
    quickDateRange.on('change', function () {
        const range = $(this).val();
        customDateInputs.toggle(range === 'custom');
        if (range !== 'custom') {
            setDateRange(getDateRange(range));
        }
        loadEntityTypes();
        reloadTable();
    });
    fromDate.add(toDate).on('change', function () {
        quickDateRange.val('custom');
        customDateInputs.show();
        reloadTable();
    });
    search.on('change', reloadTable);

    function initializeDates() {
        setDateRange(getDateRange('last6months'));
        const today = formatDate(new Date());
        fromDate.attr('max', today);
        toDate.attr('max', today);
    }

    function initializeTable() {
        if ($.fn.dataTable.isDataTable($('#AuditLogsTable')[0])) {
            auditTable = $('#AuditLogsTable').DataTable();
            return;
        }

        auditTable = $('#AuditLogsTable').DataTable(abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: true,
            searching: false,
            order: [[0, 'desc']],
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.history.auditLog.getList,
                function () {
                    const dates = getDateFilters();
                    return {
                        startTime: dates.startTime,
                        endTime: dates.endTime,
                        entityTypeFullName: entityType.val() || null,
                        changeType: changeType.val() ? Number(changeType.val()) : null,
                        filter: search.val() || null
                    };
                }
            ),
            columnDefs: [
                { title: 'Change time', data: 'changeTime', render: formatDateTime },
                { title: 'Entity', data: 'entityTypeFullName' },
                { title: 'Property', data: 'propertyName' },
                { title: 'Original value', data: 'originalValue' },
                { title: 'New value', data: 'newValue' },
                { title: 'Change', data: 'changeType', render: formatChangeType },
                { title: 'Name', data: 'userFirstName' },
                { title: 'Surname', data: 'userSurname' },
                { title: 'Service', data: 'serviceName' },
                { title: 'Method', data: 'methodName' },
                { title: 'URL', data: 'url' }
            ],
            processing: true
        }));

        if (typeof $.fn.dataTable.FilterRow === 'function') {
            new $.fn.dataTable.FilterRow(auditTable.settings()[0], {
                buttonId: 'audit-filter-button',
                buttonText: 'Filter',
                buttonTextActive: 'Filter*',
                enablePopover: $.fn.popover !== 'undefined'
            });
        }
    }

    function loadEntityTypes() {
        unity.grantManager.history.auditLog.getEntityTypeFullNames(getDateFilters()).then(function (types) {
            entityType.find('option:not(:first)').remove();
            types.forEach(function (type) {
                entityType.append($('<option>', { value: type, text: type }));
            });
        });
    }

    function reloadTable() {
        if (auditTable) {
            auditTable.ajax.reload(null, true);
        }
    }

    function getDateFilters() {
        return {
            startTime: fromDate.val() ? new Date(fromDate.val()).toISOString() : null,
            endTime: toDate.val() ? new Date(`${toDate.val()}T23:59:59.999`).toISOString() : null
        };
    }

    function setDateRange(range) {
        fromDate.val(range?.fromDate ?? '');
        toDate.val(range?.toDate ?? '');
    }

    function formatDateTime(data) {
        return data ? luxon.DateTime.fromISO(data, { locale: abp.localization.currentCulture.name }).toLocaleString(luxon.DateTime.DATETIME_MED) : '';
    }

    function formatChangeType(data) {
        return ['Created', 'Updated', 'Deleted'][data] ?? data;
    }

    function formatDate(date) {
        return date.getFullYear() + '-' + String(date.getMonth() + 1).padStart(2, '0') + '-' + String(date.getDate()).padStart(2, '0');
    }

    function getDateRange(rangeType) {
        const today = new Date();
        const toDateValue = formatDate(new Date());
        let fromDateValue;

        switch (rangeType) {
            case 'today':
                fromDateValue = toDateValue;
                break;
            case 'last7days':
                fromDateValue = formatDate(new Date(today.setDate(today.getDate() - 7)));
                break;
            case 'last30days':
                fromDateValue = formatDate(new Date(today.setDate(today.getDate() - 30)));
                break;
            case 'last3months':
                fromDateValue = formatDate(new Date(today.setMonth(today.getMonth() - 3)));
                break;
            case 'last6months':
                fromDateValue = formatDate(new Date(today.setMonth(today.getMonth() - 6)));
                break;
            case 'alltime':
                return { fromDate: null, toDate: null };
            default:
                return { fromDate: null, toDate: null };
        }

        return { fromDate: fromDateValue, toDate: toDateValue };
    }
});
