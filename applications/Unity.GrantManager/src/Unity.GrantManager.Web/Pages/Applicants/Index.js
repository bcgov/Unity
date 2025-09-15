$(function () {
    const formatter = createNumberFormatter();
    const l = abp.localization.getResource('GrantManager');
    let dt = $('#ApplicantsTable');
    let dataTable;

    // Default visible columns as per requirements
    const defaultVisibleColumns = [
        'select',
        'applicantName',
        'unityApplicantId',
        'orgName',
        'orgNumber',
        'orgStatus',
        'organizationType',
        'status',
        'redStop'
    ];

    // Get column definitions
    const listColumns = getColumns();

    // Format items with rowCount for selection
    let formatItems = function (items) {
        const newData = items.map((item, index) => {
            return {
                ...item,
                rowCount: index
            };
        });
        return newData;
    };

    // Basic column definitions - will be fully implemented in Stage 6
    function getColumns() {
        let columnIndex = 1;
        return [
            getSelectColumn('Select Applicant', 'rowCount', 'applicants'),
            {
                data: 'applicantName',
                name: 'applicantName',
                title: 'Applicant Name',
                index: columnIndex++
            },
            {
                data: 'unityApplicantId',
                name: 'unityApplicantId',
                title: 'Unity Applicant ID',
                index: columnIndex++
            },
            {
                data: 'orgName',
                name: 'orgName',
                title: 'Organization Name',
                index: columnIndex++
            },
            {
                data: 'orgNumber',
                name: 'orgNumber',
                title: 'Organization Number',
                index: columnIndex++
            },
            {
                data: 'orgStatus',
                name: 'orgStatus',
                title: 'Organization Status',
                index: columnIndex++
            },
            {
                data: 'organizationType',
                name: 'organizationType',
                title: 'Organization Type',
                index: columnIndex++
            },
            {
                data: 'status',
                name: 'status',
                title: 'Status',
                index: columnIndex++
            },
            {
                data: 'redStop',
                name: 'redStop',
                title: 'Red Stop',
                index: columnIndex++,
                render: function(data) {
                    return data ? 'Yes' : 'No';
                }
            }
        ];
    }

    // For stateRestore label in modal
    let languageSetValues = {
        buttons: {
            stateRestore: 'View %d'
        },
        stateRestore: {
            creationModal: {
                title: 'Create View',
                name: 'Name',
                button: 'Save',
            },
            emptyStates: 'No saved views',
            renameTitle: 'Rename View',
            renameLabel: 'New name for "%s"',
            removeTitle: 'Delete View',
            removeConfirm: 'Are you sure you want to delete "%s"?',
            removeSubmit: 'Delete',
            duplicateError: 'A view with this name already exists.',
            removeError: 'Failed to remove view.',
        }
    };

    let actionButtons = [
        {
            extend: 'csv',
            text: 'Export',
            className: 'custom-table-btn flex-none btn btn-secondary',
            exportOptions: {
                columns: ':visible:not(.notexport)',
                orthogonal: 'export'
            }
        }
        // Columns button is automatically added by initializeDataTable
    ];

    let responseCallback = function (result) {
        // Map the result to match DataTables expected format
        return {
            recordsTotal: result.totalCount,
            recordsFiltered: result.totalCount,
            data: formatItems(result.items)
        };
    };

    // Initialize DataTable with server-side processing
    dataTable = initializeDataTable({
        dt,
        defaultVisibleColumns,
        listColumns,
        maxRowsPerPage: 10,
        defaultSortColumn: 1, // Sort by applicantName by default
        dataEndpoint: unity.grantManager.applicants.applicant.getList,
        data: {},
        responseCallback,
        actionButtons,
        serverSideEnabled: true, // Important: Enable server-side processing
        pagingEnabled: true,
        reorderEnabled: true
    });

    // Handle row selection for future ActionBar functionality
    dataTable.on('select', function (e, dt, type, indexes) {
        handleRowSelection();
    });

    dataTable.on('deselect', function (e, dt, type, indexes) {
        handleRowSelection();
    });

    function handleRowSelection() {
        const selectedRows = dataTable.rows({ selected: true }).count();
        // TODO: Update ActionBar buttons based on selection when Stage 7 is implemented
        console.log('Selected rows:', selectedRows);
    }
});