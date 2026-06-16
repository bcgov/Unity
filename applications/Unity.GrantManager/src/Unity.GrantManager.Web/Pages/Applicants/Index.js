$(function () {    
    let dt = $('#ApplicantsTable');
    let dataTable;
    const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
    const requestedFieldsStorageKey = 'Applicants_RequestedFields';
    const dtTextRenderer = $.fn.dataTable.render.text();
    const currentCultureName = abp.localization.currentCulture.name;

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
        'redStop',
        'creationTime'
    ];

    const defaultSortOrderColumn = {
        name: 'creationTime',
        dir: 'desc'
    };

    // Get column definitions
    const listColumns = getColumns();

    // Format items with rowCount for selection
    let formatItems = function (items) {
        items.forEach((item, index) => {
            item.rowCount = index;
        });
        return items;
    };

    // Complete column definitions following GrantApplications pattern
    function getColumns() {
        let columnIndex = 1;
        const columns = [
            {
                ...getSelectColumn('Select Applicant', 'rowCount', 'applicants'),
                orderable: false,
                searchable: false,
                index: 0
            },
            getApplicantNameColumn(columnIndex++),
            getUnityApplicantIdColumn(columnIndex++),
            getOrgNameColumn(columnIndex++),
            getOrgNumberColumn(columnIndex++),
            getOrgStatusColumn(columnIndex++),
            getOrganizationTypeColumn(columnIndex++),
            getStatusColumn(columnIndex++),
            getRedStopColumn(columnIndex++),
            getNonRegisteredBusinessNameColumn(columnIndex++),
            getNonRegOrgNameColumn(columnIndex++),
            getOrganizationSizeColumn(columnIndex++),
            getSectorColumn(columnIndex++),
            getSubSectorColumn(columnIndex++),
            getApproxNumberOfEmployeesColumn(columnIndex++),
            getIndigenousOrgIndColumn(columnIndex++),
            getSectorSubSectorIndustryDescColumn(columnIndex++),
            getFiscalMonthColumn(columnIndex++),
            getBusinessNumberColumn(columnIndex++),
            getFiscalDayColumn(columnIndex++),
            getStartedOperatingDateColumn(columnIndex++),
            getIsDuplicatedColumn(columnIndex++),            
            getCreationTimeColumn(columnIndex++),
            getLastModificationTimeColumn(columnIndex++)
        ];
        
        // Map columns with targets and orderData, but exclude select column from orderData
        const sortedColumns = columns.map((column) => ({
            ...column,
            targets: [column.index],
            // Only add orderData for non-select columns (index > 0)
            ...(column.index > 0 ? { orderData: [column.index] } : {})
        })).sort((a, b) => a.index - b.index);
        
        return sortedColumns;
    }

    function getApplicantNameColumn(columnIndex) {
        return {
            title: 'Applicant Name',
            data: 'applicantName',
            name: 'applicantName',
            className: 'data-table-header',
            index: columnIndex,
            render: function (data, type, row) {
                const applicantName = (typeof data !== 'string' || data.trim() === '') ? 'Applicant Name' : data;

                if (type !== 'display') {
                    return applicantName;
                }

                const safeApplicantName = dtTextRenderer.display(applicantName);
                const applicantId = row?.id;
                const isGuid = applicantId && guidPattern.test(applicantId);

                if (isGuid) {
                    return `<a href="/GrantApplicants/Details?ApplicantId=${encodeURIComponent(applicantId)}">${safeApplicantName}</a>`;
                }

                return safeApplicantName;
            }
        }
    }

    function getUnityApplicantIdColumn(columnIndex) {
        return {
            title: 'Applicant ID',
            data: 'unityApplicantId',
            name: 'unityApplicantId',
            className: 'data-table-header text-nowrap',
            render: function (data, type, row) {
                if (type !== 'display') {
                    return (data && String(data).trim() !== '') ? data : '';
                }
                const displayValue = (data && String(data).trim() !== '') ? data : 'blank';
                return `<a href="/GrantApplicants/Details?ApplicantId=${row.id}">${dtTextRenderer.display(displayValue)}</a>`;
            },
            index: columnIndex
        }
    }

    function getOrgNameColumn(columnIndex) {
        return {
            title: 'Organization Name',
            data: 'orgName',
            name: 'orgName',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        }
    }

    function getOrgNumberColumn(columnIndex) {
        return {
            title: 'Organization Number',
            data: 'orgNumber',
            name: 'orgNumber',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        }
    }

    function getOrgStatusColumn(columnIndex) {
        return {
            title: 'Organization Status',
            data: 'orgStatus',
            name: 'orgStatus',
            className: 'data-table-header',
            render: function (data) {
                if (data === 'ACTIVE') return 'Active';
                if (data === 'HISTORICAL') return 'Historical';
                return data ?? '';
            },
            index: columnIndex
        }
    }

    function getOrganizationTypeColumn(columnIndex) {
        return {
            title: 'Organization Type',
            data: 'organizationType',
            name: 'organizationType',
            className: 'data-table-header',
            render: function (data) {
                return getFullType(data) ?? '';
            },
            index: columnIndex
        }
    }

    function getStatusColumn(columnIndex) {
        return {
            title: 'Status',
            data: 'status',
            name: 'status',
            className: 'data-table-header',
            index: columnIndex
        }
    }

    function getRedStopColumn(columnIndex) {
        return {
            title: 'Red-Stop',
            data: 'redStop',
            name: 'redStop',
            className: 'data-table-header',
            render: function (data) {
                return convertToYesNo(data);
            },
            index: columnIndex
        }
    }

    function getNonRegisteredBusinessNameColumn(columnIndex) {
        return {
            title: 'Non-Registered Business Name',
            data: 'nonRegisteredBusinessName',
            name: 'nonRegisteredBusinessName',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        }
    }

    function getNonRegOrgNameColumn(columnIndex) {
        return {
            title: 'Non-Registered Organization Name',
            data: 'nonRegOrgName',
            name: 'nonRegOrgName',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        }
    }

    function getOrganizationSizeColumn(columnIndex) {
        return {
            title: 'Organization Size',
            data: 'organizationSize',
            name: 'organizationSize',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        }
    }

    function getSectorColumn(columnIndex) {
        return {
            title: 'Sector',
            data: 'sector',
            name: 'sector',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        }
    }

    function getSubSectorColumn(columnIndex) {
        return {
            title: 'SubSector',
            data: 'subSector',
            name: 'subSector',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        }
    }

    function getApproxNumberOfEmployeesColumn(columnIndex) {
        return {
            title: 'Approx. Number of Employees',
            data: 'approxNumberOfEmployees',
            name: 'approxNumberOfEmployees',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        }
    }

    function getIndigenousOrgIndColumn(columnIndex) {
        return {
            title: 'Indigenous',
            data: 'indigenousOrgInd',
            name: 'indigenousOrgInd',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        }
    }

    function getSectorSubSectorIndustryDescColumn(columnIndex) {
        return {
            title: 'Other Sector/Sub/Industry Description',
            data: 'sectorSubSectorIndustryDesc',
            name: 'sectorSubSectorIndustryDesc',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        }
    }

    function getFiscalMonthColumn(columnIndex) {
        return {
            title: 'FYE Month',
            data: 'fiscalMonth',
            name: 'fiscalMonth',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                if (data) {
                    return titleCase(data);
                } else {
                    return '';
                }
            },
            index: columnIndex
        }
    }

    function getBusinessNumberColumn(columnIndex) {
        return {
            title: 'Business Number',
            data: 'businessNumber',
            name: 'businessNumber',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        }
    }

    function getFiscalDayColumn(columnIndex) {
        return {
            title: 'FYE Day',
            data: 'fiscalDay',
            name: 'fiscalDay',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        }
    }

    function getStartedOperatingDateColumn(columnIndex) {
        return {
            title: 'Started Operating Date',
            data: 'startedOperatingDate',
            name: 'startedOperatingDate',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                return data != null ? luxon.DateTime.fromISO(data, {
                    locale: currentCultureName,
                }).toUTC().toLocaleString() : '';
            },
            index: columnIndex
        }
    }

    function getIsDuplicatedColumn(columnIndex) {
        return {
            title: 'Is Duplicated',
            data: 'isDuplicated',
            name: 'isDuplicated',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                return convertToYesNo(data);
            },
            index: columnIndex
        }
    }

    function getCreationTimeColumn(columnIndex) {
        return {
            title: 'Created Date',
            data: 'creationTime',
            name: 'creationTime',
            className: 'data-table-header',
            visible: false,
            render: DataTable.render.date('YYYY-MM-DD', currentCultureName),
            index: columnIndex
        }
    }

    function getLastModificationTimeColumn(columnIndex) {
        return {
            title: 'Last Modified',
            data: 'lastModificationTime',
            name: 'lastModificationTime',
            className: 'data-table-header',
            visible: false,
            render: DataTable.render.date('YYYY-MM-DD', currentCultureName),
            index: columnIndex
        }
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
            text: 'Filter',
            className: 'custom-table-btn flex-none btn btn-secondary',
            id: "btn-toggle-filter",
            action: function (e, dt, node, config) { },
            attr: {
                id: 'btn-toggle-filter'
            }
        },
        {
            extend: 'csv',
            text: 'Export',
            className: 'custom-table-btn flex-none btn btn-secondary',
            exportOptions: {
                columns: ':visible:not(.notexport)',
                orthogonal: 'export'
            }
        },
        {
            extend: 'savedStates',
            className: 'custom-table-btn flex-none btn btn-secondary grp-savedStates',
            config: {
                creationModal: true,
                splitSecondaries: [
                    { extend: 'updateState', text: '<i class="fa-regular fa-floppy-disk" ></i> Update'},
                    { extend: 'renameState', text: '<i class="fa-regular fa-pen-to-square" ></i> Rename'},
                    { extend: 'removeState', text: '<i class="fa-regular fa-trash-can" ></i> Delete'}
                ]
            },
            buttons: [
                { extend: 'createState', text: 'Save As View' },
                {
                    text: "Reset to Default View",
                    action: function (e, dt, node, config)
                    {
                        let dtInit = dt.init();
                        let initialSortOrder = dtInit?.order ?? [];
                        dt.columns().visible(false);

                        // List of all columns not including default columns
                        const allColumnNames = dt.settings()[0].aoColumns.map(col => col.name).filter(colName => !defaultVisibleColumns.includes(colName));
                        const orderedIndexes = [];

                        // Set the visible columns, and collect id's for the reorder
                        defaultVisibleColumns.forEach((colName) => {
                            const colIdx = dt.column(`${colName}:name`).index();
                            if (colIdx !== undefined && colIdx !== -1) {
                                dt.column(colIdx).visible(true);
                                orderedIndexes.push(colIdx);
                            }
                        });

                        // Column reorder only works if all columns included in new order, so get the rest of the columns
                        allColumnNames.forEach((colName) => {
                            const colIdx = dt.column(`${colName}:name`).index();
                            if (colIdx !== undefined && colIdx !== -1) {
                                orderedIndexes.push(colIdx);
                            }
                        })
                        dt.colReorder.order(orderedIndexes);

                        $('#search, .custom-filter-input').val('');
                        dt.columns().search('');
                        dt.search('');
                        dt.order(initialSortOrder).draw();

                        // Close the dropdown
                        dt.buttons('.grp-savedStates')
                            .container()
                            .find('.dt-button-collection')
                            .hide();
                        $('div.dt-button-background').trigger('click');
                    }
                },
                { extend: 'removeAllStates', text: 'Delete All Views' },
                {
                    extend: 'spacer',
                    style: 'bar',
                }
            ]
        }
    ];

    let responseCallback = function (result) {
        // Map the result to match DataTables expected format
        return {
            recordsTotal: result.totalCount,
            recordsFiltered: result.totalCount,
            data: formatItems(result.items)
        };
    };    

    function getRequestedFields() {
        let requestedFields;

        if (dataTable) {
            try {
                const cols = dataTable.settings()[0].aoColumns;
                requestedFields = cols
                    .filter(function (col, idx) { return dataTable.column(idx).visible(); })
                    .map(function (col) { return col.sName; })
                    .filter(function (name) { return !!name; });

                if (requestedFields.length > 0) {
                    localStorage.setItem(requestedFieldsStorageKey, JSON.stringify(requestedFields));
                }
            } catch {
                // DataTable may not be fully initialized yet.
            }
        }

        if (!requestedFields || requestedFields.length === 0) {
            try {
                const saved = localStorage.getItem(requestedFieldsStorageKey);
                if (saved) {
                    requestedFields = JSON.parse(saved);
                }
            } catch {
                // Ignore invalid localStorage values.
            }
        }

        if (!requestedFields || requestedFields.length === 0) {
            requestedFields = defaultVisibleColumns;
        }

        return requestedFields;
    }

    // Initialize DataTable with server-side processing
    dataTable = initializeDataTable({
        dt,
        defaultVisibleColumns,
        listColumns,
        maxRowsPerPage: 10,
        defaultSortColumn: defaultSortOrderColumn,
        dataEndpoint: unity.grantManager.applicants.applicant.getList,
        data: function () {
            return {
                requestedFields: getRequestedFields()
            };
        },
        responseCallback,
        actionButtons,
        deferRender: true,
        serverSideEnabled: false, // Switch to client-side processing to enable filter functionality
        pagingEnabled: true,
        reorderEnabled: true,
        languageSetValues,
        fixedHeaders: true,
        dataTableName: 'ApplicantsTable',
        dynamicButtonContainerId: 'dynamicButtonContainerId',
        // Add state handling to validate and clear corrupted states
        stateSaveCallback: function(settings, data) {
            try {
                localStorage.setItem('DataTables_' + settings.sInstance, JSON.stringify(data));
            } catch(e) {
                console.error('Failed to save DataTable state:', e);
            }
        },
        stateLoadCallback: function(settings) {
            try {
                const savedState = localStorage.getItem('DataTables_' + settings.sInstance);
                if (savedState) {
                    const state = JSON.parse(savedState);
                    // Validate that saved state column count matches current table
                    if (state.columns && state.columns.length !== listColumns.length) {
                        console.warn('Saved DataTable state has different column count (saved: ' + 
                            state.columns.length + ', current: ' + listColumns.length + 
                            '). Clearing invalid state.');
                        localStorage.removeItem('DataTables_' + settings.sInstance);
                        return null;
                    }
                    return state;
                }
            } catch(e) {
                console.error('Failed to load DataTable state, clearing:', e);
                try {
                    localStorage.removeItem('DataTables_' + settings.sInstance);
                } catch(removeError) {
                    console.error('Failed to remove corrupted state:', removeError);
                }
            }
            return null;
        }
    });


    // Handle row selection and publish events for ActionBar
    dataTable.on('select', function (e, dt, type, indexes) {
        if (type === 'row' && indexes?.length) {
            indexes.forEach(index => {
                $("#row_" + index).prop("checked", true);
                if ($(".chkbox:checked").length == $(".chkbox").length) {
                    $(".select-all-applicants").prop("checked", true);
                }
                const data = dataTable.row(index).data();
                PubSub.publish('select_applicant', data);
            });
        }
    });

    dataTable.on('deselect', function (e, dt, type, indexes) {
        if (type === 'row' && indexes?.length) {
            indexes.forEach(index => {
                $("#row_" + index).prop("checked", false);
                if ($(".chkbox:checked").length != $(".chkbox").length) {
                    $(".select-all-applicants").prop("checked", false);
                }
                const data = dataTable.row(index).data();
                PubSub.publish('deselect_applicant', data);
            });
        }
    });


    dataTable.on('column-visibility.dt', function () {
        getRequestedFields();
    });

    // Handle search from ActionBar
    $('#search').on('input', function () {
        dataTable.search($(this).val()).draw();
    });

    // For savedStates
    $('.grp-savedStates').text('Save View');
    $('.grp-savedStates').closest('.btn-group').addClass('cstm-save-view');

    // Subscribe to refresh events
    PubSub.subscribe('refresh_applicant_list', (msg, data) => {
        dataTable.ajax.reload(null, false);
        $(".select-all-applicants").prop("checked", false);
        PubSub.publish('clear_selected_applicant');
    });

    // Handle select-all checkbox functionality
    $('.select-all-applicants').click(function () {
        if ($(this).is(':checked')) {
            dataTable.rows({ 'page': 'current' }).select();
        } else {
            dataTable.rows({ 'page': 'current' }).deselect();
        }
    });



    // Helper functions for column rendering
    function titleCase(str) {
        str = str.toLowerCase().split(' ');
        for (let i = 0; i < str.length; i++) {
            str[i] = str[i].charAt(0).toUpperCase() + str[i].slice(1);
        }
        return str.join(' ');
    }

    function convertToYesNo(str) {
        switch (str) {
            case true:
                return "Yes";
            case false:
                return "No";
            default:
                return '';
        }
    }

    const COMPANY_TYPE_MAP = new Map([
        ["BC", "BC Company"],
        ["CP", "Cooperative"],
        ["GP", "General Partnership"],
        ["S", "Society"],
        ["SP", "Sole Proprietorship"],
        ["A", "Extraprovincial Company"],
        ["B", "Extraprovincial"],
        ["BEN", "Benefit Company"],
        ["C", "Continuation In"],
        ["CC", "BC Community Contribution Company"],
        ["CS", "Continued In Society"],
        ["CUL", "Continuation In as a BC ULC"],
        ["EPR", "Extraprovincial Registration"],
        ["FI", "Financial Institution"],
        ["FOR", "Foreign Registration"],
        ["LIB", "Public Library Association"],
        ["LIC", "Licensed (Extra-Pro)"],
        ["LL", "Limited Liability Partnership"],
        ["LLC", "Limited Liability Company"],
        ["LP", "Limited Partnership"],
        ["MF", "Miscellaneous Firm"],
        ["PA", "Private Act"],
        ["PAR", "Parish"],
        ["QA", "CO 1860"],
        ["QB", "CO 1862"],
        ["QC", "CO 1878"],
        ["QD", "CO 1890"],
        ["QE", "CO 1897"],
        ["REG", "Registration (Extra-pro)"],
        ["ULC", "BC Unlimited Liability Company"],
        ["XCP", "Extraprovincial Cooperative"],
        ["XL", "Extrapro Limited Liability Partnership"],
        ["XP", "Extraprovincial Limited Partnership"],
        ["XS", "Extraprovincial Society"]
    ]);

    function getFullType(code) {
        return COMPANY_TYPE_MAP.get(code) ?? 'Unknown';
    }
});
