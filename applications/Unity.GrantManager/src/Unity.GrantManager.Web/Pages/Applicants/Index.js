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
            getSupplierIdColumn(columnIndex++),
            getSiteIdColumn(columnIndex++),
            getMatchPercentageColumn(columnIndex++),
            getIsDuplicatedColumn(columnIndex++),
            getElectoralDistrictColumn(columnIndex++),
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
            index: columnIndex
        }
    }

    function getUnityApplicantIdColumn(columnIndex) {
        return {
            title: 'Unity Applicant ID',
            data: 'unityApplicantId',
            name: 'unityApplicantId',
            className: 'data-table-header text-nowrap',
            render: function (data, type, row) {
                return `<a href="/GrantApplicants/Details?ApplicantId=${row.id}">${data}</a>`;
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
                if (data != null && data == 'ACTIVE') {
                    return 'Active';
                } else if (data != null && data == 'HISTORICAL') {
                    return 'Historical';
                } else {
                    return data ?? '';
                }
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
                    locale: abp.localization.currentCulture.name,
                }).toUTC().toLocaleString() : '';
            },
            index: columnIndex
        }
    }

    function getSupplierIdColumn(columnIndex) {
        return {
            title: 'Supplier ID',
            data: 'supplierId',
            name: 'supplierId',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        }
    }

    function getSiteIdColumn(columnIndex) {
        return {
            title: 'Site ID',
            data: 'siteId',
            name: 'siteId',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        }
    }

    function getMatchPercentageColumn(columnIndex) {
        return {
            title: 'Match Percentage',
            data: 'matchPercentage',
            name: 'matchPercentage',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                if (data != null) {
                    return data + '%';
                }
                return '';
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

    function getElectoralDistrictColumn(columnIndex) {
        return {
            title: 'Electoral District',
            data: 'electoralDistrict',
            name: 'electoralDistrict',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        }
    }

    function getCreationTimeColumn(columnIndex) {
        return {
            title: 'Creation Time',
            data: 'creationTime',
            name: 'creationTime',
            className: 'data-table-header',
            visible: false,
            render: DataTable.render.date('YYYY-MM-DD', abp.localization.currentCulture.name),
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
            render: DataTable.render.date('YYYY-MM-DD', abp.localization.currentCulture.name),
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
                        dt.order([26, 'desc']).draw(); // Sort by creationTime descending

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

    // Initialize DataTable with server-side processing
    dataTable = initializeDataTable({
        dt,
        defaultVisibleColumns,
        listColumns,
        maxRowsPerPage: 10,
        defaultSortColumn: 26, // Sort by creationTime (column 26) descending
        dataEndpoint: unity.grantManager.applicants.applicant.getList,
        data: {},
        responseCallback,
        actionButtons,
        serverSideEnabled: true, // Important: Enable server-side processing
        pagingEnabled: true,
        reorderEnabled: true,
        languageSetValues,
        dataTableName: 'ApplicantsTable',
        dynamicButtonContainerId: 'dynamicButtonContainerId'
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

    function getFullType(code) {
        const companyTypes = [
            { code: "BC", name: "BC Company" },
            { code: "CP", name: "Cooperative" },
            { code: "GP", name: "General Partnership" },
            { code: "S", name: "Society" },
            { code: "SP", name: "Sole Proprietorship" },
            { code: "A", name: "Extraprovincial Company" },
            { code: "B", name: "Extraprovincial" },
            { code: "BEN", name: "Benefit Company" },
            { code: "C", name: "Continuation In" },
            { code: "CC", name: "BC Community Contribution Company" },
            { code: "CS", name: "Continued In Society" },
            { code: "CUL", name: "Continuation In as a BC ULC" },
            { code: "EPR", name: "Extraprovincial Registration" },
            { code: "FI", name: "Financial Institution" },
            { code: "FOR", name: "Foreign Registration" },
            { code: "LIB", name: "Public Library Association" },
            { code: "LIC", name: "Licensed (Extra-Pro)" },
            { code: "LL", name: "Limited Liability Partnership" },
            { code: "LLC", name: "Limited Liability Company" },
            { code: "LP", name: "Limited Partnership" },
            { code: "MF", name: "Miscellaneous Firm" },
            { code: "PA", name: "Private Act" },
            { code: "PAR", name: "Parish" },
            { code: "QA", name: "CO 1860" },
            { code: "QB", name: "CO 1862" },
            { code: "QC", name: "CO 1878" },
            { code: "QD", name: "CO 1890" },
            { code: "QE", name: "CO 1897" },
            { code: "REG", name: "Registraton (Extra-pro)" },
            { code: "ULC", name: "BC Unlimited Liability Company" },
            { code: "XCP", name: "Extraprovincial Cooperative" },
            { code: "XL", name: "Extrapro Limited Liability Partnership" },
            { code: "XP", name: "Extraprovincial Limited Partnership" },
            { code: "XS", name: "Extraprovincial Society" }
        ];
        const match = companyTypes.find(entry => entry.code === code);
        return match ? match.name : "Unknown";
    }
});