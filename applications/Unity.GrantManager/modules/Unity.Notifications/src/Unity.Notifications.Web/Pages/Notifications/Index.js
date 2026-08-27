$(function () {
    const l = abp.localization.getResource('Notifications');
    const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
    let dt = $('#NotificationListTable');

    const defaultQuickDateRange = 'last6months';
    const STORAGE_KEYS = {
        quickRange: 'Notifications_QuickRange',
        fromDate: 'Notifications_FromDate',
        toDate: 'Notifications_ToDate'
    };
    let notificationTableFilters = { dateFrom: null, dateTo: null };
    // The Date From/To inputs are hidden by default and revealed by the Filter toggle
    // (or when 'Custom Ranges' is selected). The Quick Range dropdown is always visible.
    let dateInputsForcedByFilter = false;

    const UIElements = {
        quickDateRange: $('#quickDateRange'),
        inputFilter: $('.date-input-filter'),
        submittedFromInput: $('#submittedFromDate'),
        submittedToInput: $('#submittedToDate')
    };

    const listColumns = getColumns();
    const defaultVisibleColumns = [
        'submissionReferenceNo',
        'applicantName',
        'sentDateTime',
        'status',
        'fromAddress',
        'toAddress',
        'subject',
        'recipient',
        'emailTypeText'
    ];

    // Seed the date filters BEFORE the table's first load so it defaults to the last 6 months.
    initializeFilterDates();

    let actionButtons = [
        {
            text: l('Filter'),
            id: 'btn-toggle-filter',
            className: 'custom-table-btn flex-none btn btn-secondary',
            action: function (e, dt, node, config) { },
            attr: { id: 'btn-toggle-filter' }
        },
        {
            extend: 'csvNoPlaceholder',
            text: l('Export'),
            title: l('Menu:Notifications'),
            className: 'custom-table-btn flex-none btn btn-secondary'
        }
    ];

    const notificationsTable = initializeDataTable({
        dt,
        defaultVisibleColumns,
        listColumns,
        maxRowsPerPage: 15,
        defaultSortColumn: { name: 'sentDateTime', dir: 'desc' },
        dataEndpoint: unity.grantManager.notifications.notificationList.getList,
        data: function () {
            return {
                dateFrom: notificationTableFilters.dateFrom,
                dateTo: notificationTableFilters.dateTo
            };
        },
        responseCallback,
        actionButtons,
        pagingEnabled: true,
        reorderEnabled: true,
        languageSetValues: {},
        dataTableName: 'NotificationListTable',
        dynamicButtonContainerId: 'dynamicButtonContainerId',
        useNullPlaceholder: true,
        fixedHeaders: true
    });

    UIElements.quickDateRange.on('change', handleQuickDateRangeChange);
    UIElements.inputFilter.on('change', handleInputFilterChange);
    // Reveal the Date From/To inputs together with the per-column filter row. filterRow.js emits
    // "filterRow-visibility" when the user flips the "Show Filter Row" toggle in the Filter popover.
    notificationsTable.on('filterRow-visibility', function (e, visible) {
        dateInputsForcedByFilter = visible;
        updateDateInputsVisibility();
    });

    // ============================ Date filter ============================

    function initializeFilterDates() {
        let savedQuickRange = localStorage.getItem(STORAGE_KEYS.quickRange) || defaultQuickDateRange;
        let savedFromDate = localStorage.getItem(STORAGE_KEYS.fromDate);
        let savedToDate = localStorage.getItem(STORAGE_KEYS.toDate);

        let isCustomRange = savedQuickRange === 'custom';

        let range = isCustomRange
            ? { fromDate: savedFromDate || '', toDate: savedToDate || '' }
            : getDateRange(savedQuickRange);

        if (!isCustomRange && !range) {
            savedQuickRange = defaultQuickDateRange;
            range = getDateRange(savedQuickRange);
        }

        setDateRangeFilters(savedQuickRange, range);
        setDateRangeLocalStorage(savedQuickRange, range);

        const today = formatDate(new Date());
        UIElements.submittedFromInput.attr({ 'max': today });
        UIElements.submittedToInput.attr({ 'max': today });

        updateDateInputsVisibility();
    }

    function updateDateInputsVisibility() {
        const show = dateInputsForcedByFilter || UIElements.quickDateRange.val() === 'custom';
        $('#customDateInputs').css('display', show ? '' : 'none');
    }

    function handleQuickDateRangeChange() {
        const selectedRange = $(this).val();

        if (selectedRange === 'custom') {
            updateDateInputsVisibility();
            return;
        }

        const range = getDateRange(selectedRange);
        setDateRangeFilters(selectedRange, range);
        setDateRangeLocalStorage(selectedRange, range);
        updateDateInputsVisibility();
        notificationsTable.ajax.reload(null, true);
    }

    function handleInputFilterChange() {
        const $input = $(this);
        if (!validateDate($input.val(), $input)) return;

        syncFiltersFromInputs();

        // Manual dates blank the quick-range preset.
        UIElements.quickDateRange.val('custom');
        localStorage.setItem(STORAGE_KEYS.quickRange, 'custom');
        localStorage.setItem(STORAGE_KEYS.fromDate, notificationTableFilters.dateFrom || '');
        localStorage.setItem(STORAGE_KEYS.toDate, notificationTableFilters.dateTo || '');

        notificationsTable.ajax.reload(null, true);
    }

    function setDateRangeFilters(quickDateRange, range) {
        UIElements.quickDateRange.val(quickDateRange);

        if (range) {
            UIElements.submittedFromInput.val(range.fromDate ?? '');
            UIElements.submittedToInput.val(range.toDate ?? '');
            syncFiltersFromInputs();
        }
    }

    // Reads the From/To inputs into notificationTableFilters (the inputs are the source of truth).
    function syncFiltersFromInputs() {
        notificationTableFilters.dateFrom = UIElements.submittedFromInput.val() || null;
        notificationTableFilters.dateTo = UIElements.submittedToInput.val() || null;
    }

    function setDateRangeLocalStorage(quickDateRange, fromToRange) {
        localStorage.setItem(STORAGE_KEYS.quickRange, quickDateRange || defaultQuickDateRange);
        if (fromToRange) {
            if (fromToRange.fromDate) localStorage.setItem(STORAGE_KEYS.fromDate, fromToRange.fromDate);
            else localStorage.removeItem(STORAGE_KEYS.fromDate);
            if (fromToRange.toDate) localStorage.setItem(STORAGE_KEYS.toDate, fromToRange.toDate);
            else localStorage.removeItem(STORAGE_KEYS.toDate);
        }
    }

    function validateDate(dateValue, element) {
        if (!dateValue) return true;

        const selectedDate = new Date(dateValue);
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        if (selectedDate > today) {
            element.addClass('input-validation-error');
            abp.notify.error(l('NotificationList:DateCannotBeFuture'));
            return false;
        }

        element.removeClass('input-validation-error');
        return true;
    }

    // ============================ Columns ============================

    function getColumns() {
        let columnIndex = 0;
        const columns = [
            getNotificationIdColumn(columnIndex++),
            getSubmissionIdColumn(columnIndex++),
            getTextColumn(columnIndex++, l('NotificationList:ApplicantName'), 'applicantName'),
            getSentDateColumn(columnIndex++),
            getTextColumn(columnIndex++, l('NotificationList:Status'), 'status', { width: '7rem', className: 'data-table-header text-nowrap' }),
            getTextColumn(columnIndex++, l('NotificationList:From'), 'fromAddress'),
            getTextColumn(columnIndex++, l('NotificationList:To'), 'toAddress'),
            getTextColumn(columnIndex++, l('NotificationList:Subject'), 'subject'),
            getTextColumn(columnIndex++, l('NotificationList:Recipient'), 'recipient', { width: '8rem', className: 'data-table-header text-nowrap' }),
            getTextColumn(columnIndex++, l('NotificationList:EmailType'), 'emailTypeText', { width: '9rem', className: 'data-table-header text-nowrap' })
        ];
        return columns.map((column) => ({ ...column, targets: [column.index], orderData: [column.index, 0] }));
    }

    function getNotificationIdColumn(columnIndex) {
        return {
            title: l('NotificationList:NotificationId'),
            name: 'id',
            data: 'id',
            className: 'data-table-header',
            index: columnIndex,
            visible: false
        };
    }

    function getSubmissionIdColumn(columnIndex) {
        return {
            title: l('NotificationList:SubmissionId'),
            name: 'submissionReferenceNo',
            data: 'submissionReferenceNo',
            className: 'data-table-header text-nowrap',
            width: '8rem',
            index: columnIndex,
            render: function (data, type, row) {
                if (type !== 'display') {
                    return data || '';
                }
                const safe = $.fn.dataTable.render.text().display(data || '');
                const appId = row.applicationId;
                if (data && appId && guidPattern.test(appId)) {
                    return `<a href="/GrantApplications/Details?ApplicationId=${encodeURIComponent(appId)}">${safe}</a>`;
                }
                return safe || null;
            }
        };
    }

    function getSentDateColumn(columnIndex) {
        return {
            title: l('NotificationList:SentDate'),
            name: 'sentDateTime',
            data: 'sentDateTime',
            className: 'data-table-header text-nowrap',
            width: '8rem',
            index: columnIndex,
            render: function (data, type) {
                return DateUtils.formatUtcDateToLocal(data, type);
            }
        };
    }
});

function responseCallback(result) {
    return {
        recordsTotal: result.totalCount,
        recordsFiltered: result.totalCount,
        data: result.items
    };
}

function getTextColumn(columnIndex, title, dataField, options = {}) {
    return {
        title: title,
        name: dataField,
        data: dataField,
        className: options.className ?? 'data-table-header',
        ...(options.width ? { width: options.width } : {}),
        index: columnIndex
    };
}

function formatDate(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

// Returns { fromDate, toDate } (yyyy-mm-dd) for a preset. 'alltime' -> both null; 'custom' -> null.
function getDateRange(rangeType) {
    let today = new Date();
    const toDate = formatDate(new Date());
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
