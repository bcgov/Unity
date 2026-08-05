$(function () {
    let dt = $('#ExceptionLogsTable');

    const UIElements = {
        fromDate: $('#FromDate'),
        toDate: $('#ToDate'),
        severity: $('#Severity')
    };

    const listColumns = getColumns();

    const exceptionLogsTable = initializeDataTable({
        dt,
        listColumns,
        defaultVisibleColumns: [],
        defaultSortColumn: { name: 'creationTime', dir: 'desc' },
        dataEndpoint: unity.grantManager.logs.exceptionLog.getList,
        data: function () {
            return {
                fromDate: UIElements.fromDate.val(),
                toDate: UIElements.toDate.val(),
                severity: UIElements.severity.val()
            };
        },
        actionButtons: commonTableActionButtons('Exception Logs'),
        serverSideEnabled: true,
        pagingEnabled: true,
        reorderEnabled: true,
        languageSetValues: {},
        dynamicButtonContainerId: 'dynamicButtonContainerId',
        externalSearchId: 'search-exception-logs',
        fixedHeaders: true
    });

    UIElements.fromDate.on('change', reloadTable);
    UIElements.toDate.on('change', reloadTable);
    UIElements.severity.on('change', reloadTable);

    function reloadTable() {
        exceptionLogsTable.ajax.reload();
    }

    // Marks rows that have expandable exception details, so only those get the pointer cursor.
    exceptionLogsTable.on('draw', function () {
        exceptionLogsTable.rows().every(function () {
            $(this.node()).toggleClass('has-details', hasDetails(this.data()));
        });
    });

    // Row click toggles the exception details child row (instead of a <details> element).
    exceptionLogsTable.on('click', 'tbody tr', function (e) {
        if ($(e.target).closest('a').length) {
            return;
        }

        const row = exceptionLogsTable.row(this);
        const rowData = row.data();
        if (!rowData || !hasDetails(rowData)) {
            return;
        }

        if (row.child.isShown()) {
            row.child.hide();
        } else {
            row.child(buildExceptionDetails(rowData), 'exception-log-details-row').show();
        }
    });
});

// ============================ Columns ============================

function getColumns() {
    let columnIndex = 0;
    return [
        getCreatedColumn(columnIndex++),
        getTextColumn(columnIndex++, 'Severity', 'severity', { minWidth: true }),
        getTextColumn(columnIndex++, 'Type', 'notificationType', { minWidth: true }),
        getTitleColumn(columnIndex++),
        getTextColumn(columnIndex++, 'Source', 'source'),
        getTextColumn(columnIndex++, 'Count', 'occurrenceCount'),
        getFallbackTextColumn(columnIndex++, 'User', 'userName', '(none)'),
        getFallbackTextColumn(columnIndex++, 'Tenant', 'tenantName', '(host)'),
        getTextColumn(columnIndex++, 'Author', 'blameAuthor'),
        getTextColumn(columnIndex++, 'Ticket', 'ticketReference'),
        getPrColumn(columnIndex++)
    ];
}

function getCreatedColumn(columnIndex) {
    return {
        title: 'Created',
        name: 'creationTime',
        data: 'creationTime',
        className: 'data-table-header text-nowrap',
        index: columnIndex,
        render: function (data, type) {
            return DateUtils.formatUtcDateToLocal(data, type, {
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit'
            });
        }
    };
}

// Title text comes straight from the data binding; the tooltip (full message) is set as a
// plain attribute on the cell rather than built as an HTML string.
function getTitleColumn(columnIndex) {
    return {
        title: 'Title',
        name: 'title',
        data: 'title',
        className: 'data-table-header log-title',
        index: columnIndex,
        createdCell: function (cell, cellData, rowData) {
            if (rowData.message) {
                $(cell).attr('title', rowData.message);
            }
        }
    };
}

// Markup for the link lives in the #exception-log-pr-link-template <template> in Index.cshtml;
// this just clones it and fills in the href/label.
function getPrColumn(columnIndex) {
    return {
        title: 'PR',
        name: 'pullRequestUrl',
        data: 'pullRequestUrl',
        className: 'data-table-header',
        index: columnIndex,
        createdCell: function (cell, cellData, rowData) {
            const $cell = $(cell).empty();
            if (!cellData) {
                return;
            }
            const link = cloneTemplate('exception-log-pr-link-template').querySelector('a');
            link.href = cellData;
            link.textContent = rowData.pullRequestNumber ? '#' + rowData.pullRequestNumber : cellData;
            $cell.append(link);
        }
    };
}

function getTextColumn(columnIndex, title, dataField, options = {}) {
    return {
        title: title,
        name: dataField,
        data: dataField,
        // width: '1%' + nowrap is the standard trick to shrink an auto-layout table column
        // down to its content width instead of letting it stretch to fill the table.
        className: 'data-table-header' + (options.minWidth ? ' text-nowrap' : ''),
        width: options.minWidth ? '1%' : undefined,
        index: columnIndex
    };
}

function getFallbackTextColumn(columnIndex, title, dataField, fallback) {
    return {
        title: title,
        name: dataField,
        data: dataField,
        className: 'data-table-header',
        defaultContent: fallback,
        index: columnIndex
    };
}

// ============================ Exception details child row ============================

function hasDetails(rowData) {
    return !!(rowData && (rowData.exceptionMessage || rowData.stackExcerpt));
}

// Markup lives in the #exception-log-details-template <template> in Index.cshtml; this only
// fills in field values and removes the rows/fields that have no data.
function buildExceptionDetails(data) {
    const root = cloneTemplate('exception-log-details-template').querySelector('.exception-log-details');

    setDetailField(root, 'exceptionType', data.exceptionType);
    setDetailField(root, 'exceptionMessage', data.exceptionMessage);
    setDetailField(root, 'location', data.sourceLine ? data.sourceFile + ':' + data.sourceLine : data.sourceFile);
    setDetailField(root, 'correlationId', data.correlationId);
    setDetailField(root, 'commit', joinNonEmpty(data.commitSha, data.environment ? '(' + data.environment + ')' : ''));
    setDetailField(root, 'blameCommit', data.blameCommitMessage ? joinNonEmpty(data.blameCommitSha, '—', data.blameCommitMessage) : '');
    setDetailField(root, 'pullRequestTitle', data.pullRequestTitle);

    const stackField = root.querySelector('[data-field="stackExcerpt"]');
    if (data.stackExcerpt) {
        stackField.textContent = data.stackExcerpt;
    } else {
        stackField.remove();
    }

    return root;
}

// Removes the whole row when there's no value; otherwise fills the field's text content.
function setDetailField(root, fieldName, value) {
    const row = root.querySelector('[data-row="' + fieldName + '"]');
    if (!value) {
        row.remove();
        return;
    }
    row.querySelector('[data-field="' + fieldName + '"]').textContent = value;
}

function joinNonEmpty(...parts) {
    return parts.filter(Boolean).join(' ');
}

function cloneTemplate(templateId) {
    return document.getElementById(templateId).content.cloneNode(true);
}
