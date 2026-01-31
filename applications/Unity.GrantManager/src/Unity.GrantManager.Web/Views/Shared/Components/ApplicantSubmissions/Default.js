$(function () {
    // Check if createNumberFormatter exists
    if (typeof createNumberFormatter !== 'function') {
        console.error('createNumberFormatter is not defined. Ensure table-utils.js is loaded before this script');
        return;
    }

    const formatter = createNumberFormatter();
    const l = abp.localization.getResource('GrantManager');

    // Language configuration for SavedStates
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

    // Default visible columns
    const defaultVisibleColumns = [
        'select',
        'referenceNo',        // Submission #
        'category',           // Category
        'submissionDate',     // Submission Date
        'status',             // Status
        'requestedAmount',    // Requested Amount
        'approvedAmount'      // Approved Amount
    ];

    // Default sort column
    const defaultSortOrderColumn = {
        name: 'submissionDate',
        dir: 'desc'
    };

    // Action buttons configuration
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
            extend: 'savedStates',
            className: 'custom-table-btn flex-none btn btn-secondary grp-savedStates',
            config: {
                creationModal: true,
                splitSecondaries: [
                    { extend: 'updateState', text: '<i class="fa-regular fa-floppy-disk"></i> Update'},
                    { extend: 'renameState', text: '<i class="fa-regular fa-pen-to-square"></i> Rename'},
                    { extend: 'removeState', text: '<i class="fa-regular fa-trash-can"></i> Delete'}
                ]
            },
            buttons: [
                { extend: 'createState', text: 'Save As View' },
                {
                    text: "Reset to Default View",
                    action: function (e, dt, node, config) {
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
                        });
                        dt.colReorder.order(orderedIndexes);

                        $('#submissions-search, .custom-filter-input').val('');
                        dt.columns().search('');
                        dt.search('');
                        dt.order([[3, 'desc']]).draw(); // submissionDate column

                        // Close the dropdown
                        dt.buttons('.grp-savedStates')
                            .container()
                            .find('.dt-button-collection')
                            .hide();
                        $('div.dt-button-background').trigger('click');
                    }
                },
                { extend: 'removeAllStates', text: 'Delete All Views' }
            ]
        }
    ];

    // Parse embedded data
    const submissionsDataJson = $('#ApplicantSubmissions_Data').val();
    const submissionsData = submissionsDataJson ? JSON.parse(submissionsDataJson) : [];

    // Get all columns
    const listColumns = getColumns();

    // Response callback - same pattern as Application List
    let responseCallback = function (result) {
        return {
            recordsTotal: result.totalCount,
            recordsFiltered: result.totalCount,
            data: formatItems(result.items)
        };
    };

    let formatItems = function (items) {
        const newData = items.map((item, index) => {
            return {
                ...item,
                rowCount: index
            };
        });
        return newData;
    };

    // Mock service that returns embedded data (simulating API endpoint)
    // Must return a jQuery Deferred object (not native Promise) for ABP compatibility
    const mockDataService = {
        getList: function() {
            let deferred = $.Deferred();
            deferred.resolve({
                items: submissionsData,
                totalCount: submissionsData.length
            });
            return deferred.promise();
        }
    };

    // Initialize DataTable - same pattern as Application List
    const dataTable = initializeDataTable({
        dt: $('#ApplicantSubmissionsTable'),
        defaultVisibleColumns: defaultVisibleColumns,
        listColumns: listColumns,
        maxRowsPerPage: 10,
        defaultSortColumn: defaultSortOrderColumn,
        dataEndpoint: mockDataService.getList,
        data: function () {
            return {};
        },
        responseCallback: responseCallback,
        actionButtons: actionButtons,
        serverSideEnabled: false,
        pagingEnabled: true,
        reorderEnabled: true,
        languageSetValues: languageSetValues,
        dataTableName: 'ApplicantSubmissionsTable',
        dynamicButtonContainerId: 'submissionsDynamicButtonContainerId'
    });

    // External search binding
    dataTable.externalSearch('#submissions-search', { delay: 300 });

    // Open button handling
    function updateOpenButtonState() {
        const selectedRows = dataTable.rows({ selected: true }).data();
        const $openBtn = $('#openSubmissionBtn');

        if (selectedRows.length === 1) {
            $openBtn.prop('disabled', false).show();
        } else {
            $openBtn.prop('disabled', true).hide();
        }
    }

    dataTable.on('select deselect', function () {
        updateOpenButtonState();
    });

    $('#openSubmissionBtn').on('click', function () {
        const selectedRows = dataTable.rows({ selected: true }).data();
        if (selectedRows.length === 1) {
            window.location.href = `/GrantApplications/Details?ApplicationId=${selectedRows[0].id}`;
        }
    });

    // Initialize button state
    updateOpenButtonState();

    // Column getter functions (from Application List)
    function getColumns() {
        let columnIndex = 0;
        const sortedColumns = [
            getSelectColumn(columnIndex++),
            getReferenceNoColumn(columnIndex++),
            getCategoryColumn(columnIndex++),
            getSubmissionDateColumn(columnIndex++),
            getStatusColumn(columnIndex++),
            getRequestedAmountColumn(columnIndex++),
            getApprovedAmountColumn(columnIndex++),
            getApplicantNameColumn(columnIndex++),
            getProjectNameColumn(columnIndex++),
            getSectorColumn(columnIndex++),
            getSubSectorColumn(columnIndex++),
            getTotalProjectBudgetColumn(columnIndex++),
            getAssigneesColumn(columnIndex++),
            getEconomicRegionColumn(columnIndex++),
            getRegionalDistrictColumn(columnIndex++),
            getCommunityColumn(columnIndex++),
            getOrganizationNumberColumn(columnIndex++),
            getOrgBookStatusColumn(columnIndex++),
            getProjectStartDateColumn(columnIndex++),
            getProjectEndDateColumn(columnIndex++),
            getProjectedFundingTotalColumn(columnIndex++),
            getTotalProjectBudgetPercentageColumn(columnIndex++),
            getTotalPaidAmountColumn(columnIndex++),
            getElectoralDistrictColumn(columnIndex++),
            getApplicantElectoralDistrictColumn(columnIndex++),
            getForestryOrNonForestryColumn(columnIndex++),
            getForestryFocusColumn(columnIndex++),
            getAcquisitionColumn(columnIndex++),
            getCityColumn(columnIndex++),
            getCommunityPopulationColumn(columnIndex++),
            getLikelihoodOfFundingColumn(columnIndex++),
            getSubStatusColumn(columnIndex++),
            getTagsColumn(columnIndex++),
            getTotalScoreColumn(columnIndex++),
            getAssessmentResultColumn(columnIndex++),
            getRecommendedAmountColumn(columnIndex++),
            getDueDateColumn(columnIndex++),
            getOwnerColumn(columnIndex++),
            getDecisionDateColumn(columnIndex++),
            getProjectSummaryColumn(columnIndex++),
            getOrganizationTypeColumn(columnIndex++),
            getOrganizationNameColumn(columnIndex++),
            getBusinessNumberColumn(columnIndex++),
            getDueDiligenceStatusColumn(columnIndex++),
            getDeclineRationaleColumn(columnIndex++),
            getContactFullNameColumn(columnIndex++),
            getContactTitleColumn(columnIndex++),
            getContactEmailColumn(columnIndex++),
            getContactBusinessPhoneColumn(columnIndex++),
            getContactCellPhoneColumn(columnIndex++),
            getSectorSubSectorIndustryDescColumn(columnIndex++),
            getSigningAuthorityFullNameColumn(columnIndex++),
            getSigningAuthorityTitleColumn(columnIndex++),
            getSigningAuthorityEmailColumn(columnIndex++),
            getSigningAuthorityBusinessPhoneColumn(columnIndex++),
            getSigningAuthorityCellPhoneColumn(columnIndex++),
            getPlaceColumn(columnIndex++),
            getRiskRankingColumn(columnIndex++),
            getNotesColumn(columnIndex++),
            getRedStopColumn(columnIndex++),
            getIndigenousColumn(columnIndex++),
            getFyeDayColumn(columnIndex++),
            getFyeMonthColumn(columnIndex++),
            getApplicantIdColumn(columnIndex++),
            getPayoutColumn(columnIndex++),
            getNonRegisteredOrganizationNameColumn(columnIndex++),
            getUnityApplicationIdColumn(columnIndex++)
        ].map((column) => ({ ...column, targets: [column.index], orderData: [column.index, 0] }))
            .sort((a, b) => a.index - b.index);
        return sortedColumns;
    }

    // Select column
    function getSelectColumn(columnIndex) {
        return {
            title: '',
            data: 'rowCount',
            name: 'select',
            orderable: false,
            className: 'notexport dt-checkboxes-cell',
            checkboxes: {
                selectRow: true,
                selectAllRender: '<input type="checkbox" class="form-check-input checkbox-select chkbox">',
            },
            render: function (data, type, row) {
                return '<input type="checkbox" class="form-check-input checkbox-select chkbox row-checkbox" id="row-' + data + '">';
            },
            index: columnIndex
        };
    }

    // Submission # (referenceNo) - clickable link to Application Details
    function getReferenceNoColumn(columnIndex) {
        return {
            title: 'Submission #',
            data: 'referenceNo',
            name: 'referenceNo',
            className: 'data-table-header text-nowrap',
            render: function (data, type, row) {
                return `<a href="/GrantApplications/Details?ApplicationId=${row.id}">${data || ''}</a>`;
            },
            index: columnIndex
        };
    }

    // All other column definitions copied from Application List
    function getApplicantNameColumn(columnIndex) {
        return {
            title: 'Applicant Name',
            data: 'applicant.applicantName',
            name: 'applicantName',
            className: 'data-table-header',
            index: columnIndex
        };
    }

    function getCategoryColumn(columnIndex) {
        return {
            title: 'Category',
            data: 'category',
            name: 'category',
            className: 'data-table-header',
            index: columnIndex
        };
    }

    function getSubmissionDateColumn(columnIndex) {
        return {
            title: l('SubmissionDate'),
            data: 'submissionDate',
            name: 'submissionDate',
            className: 'data-table-header',
            index: columnIndex,
            render: function (data, type) {
                const formattedDate = DateUtils.formatUtcDateToLocal(data, type);
                return formattedDate ? String(formattedDate) : '';
            }
        };
    }

    function getProjectNameColumn(columnIndex) {
        return {
            title: 'Project Name',
            data: 'projectName',
            name: 'projectName',
            className: 'data-table-header',
            index: columnIndex
        };
    }

    function getSectorColumn(columnIndex) {
        return {
            title: 'Sector',
            name: 'sector',
            data: 'applicant.sector',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getSubSectorColumn(columnIndex) {
        return {
            title: 'SubSector',
            name: 'subsector',
            data: 'applicant.subSector',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getTotalProjectBudgetColumn(columnIndex) {
        return {
            title: 'Total Project Budget',
            name: 'totalProjectBudget',
            data: 'totalProjectBudget',
            className: 'data-table-header currency-display',
            render: function (data) {
                return formatter.format(data);
            },
            index: columnIndex
        };
    }

    function getAssigneesColumn(columnIndex) {
        return {
            title: l('Assignee'),
            data: 'assignees',
            name: 'assignees',
            className: 'dt-editable',
            render: function (data, type, row) {
                let displayText = ' ';

                if (data != null && data.length == 1) {
                    displayText = type === 'fullName' ? getNames(data) : (data[0].fullName + getDutyText(data[0]));
                } else if (data.length > 1) {
                    displayText = getNames(data);
                }

                return `<span class="d-flex align-items-center dt-select-assignees">

                                <span class="ps-2 flex-fill" data-toggle="tooltip" title="`
                    + getNames(data) + '">' + displayText + '</span>' +
                    `</span>`;
            },
            index: columnIndex
        };
    }

    function getStatusColumn(columnIndex) {
        return {
            title: l('GrantApplicationStatus'),
            data: 'status',
            name: 'status',
            className: 'data-table-header',
            index: columnIndex
        };
    }

    function getRequestedAmountColumn(columnIndex) {
        return {
            title: l('RequestedAmount'),
            data: 'requestedAmount',
            name: 'requestedAmount',
            className: 'data-table-header currency-display',
            render: function (data) {
                return formatter.format(data);
            },
            index: columnIndex
        };
    }

    function getApprovedAmountColumn(columnIndex) {
        return {
            title: 'Approved Amount',
            name: 'approvedAmount',
            data: 'approvedAmount',
            className: 'data-table-header currency-display',
            render: function (data) {
                return formatter.format(data);
            },
            index: columnIndex
        };
    }

    function getEconomicRegionColumn(columnIndex) {
        return {
            title: 'Economic Region',
            name: 'economicRegion',
            data: 'economicRegion',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getRegionalDistrictColumn(columnIndex) {
        return {
            title: 'Regional District',
            name: 'regionalDistrict',
            data: 'regionalDistrict',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getCommunityColumn(columnIndex) {
        return {
            title: 'Community',
            name: 'community',
            data: 'community',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getOrganizationNumberColumn(columnIndex) {
        return {
            title: l('ApplicantInfoView:ApplicantInfo.OrgNumber'),
            name: 'orgNumber',
            data: 'applicant.orgNumber',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getOrgBookStatusColumn(columnIndex) {
        return {
            title: 'Org Book Status',
            name: 'orgBookStatus',
            data: 'applicant.orgStatus',
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
        };
    }

    function getProjectStartDateColumn(columnIndex) {
        return {
            title: 'Project Start Date',
            name: 'projectStartDate',
            data: 'projectStartDate',
            className: 'data-table-header',
            render: function (data) {
                return data != null ? luxon.DateTime.fromISO(data, {
                    locale: abp.localization.currentCulture.name,
                }).toUTC().toLocaleString() : '';
            },
            index: columnIndex
        };
    }

    function getProjectEndDateColumn(columnIndex) {
        return {
            title: 'Project End Date',
            name: 'projectEndDate',
            data: 'projectEndDate',
            className: 'data-table-header',
            render: function (data) {
                return data != null ? luxon.DateTime.fromISO(data, {
                    locale: abp.localization.currentCulture.name,
                }).toUTC().toLocaleString() : '';
            },
            index: columnIndex
        };
    }

    function getProjectedFundingTotalColumn(columnIndex) {
        return {
            title: 'Projected Funding Total',
            name: 'projectFundingTotal',
            data: 'projectFundingTotal',
            className: 'data-table-header currency-display',
            render: function (data) {
                return formatter.format(data) ?? '';
            },
            index: columnIndex
        };
    }

    function getTotalProjectBudgetPercentageColumn(columnIndex) {
        return {
            title: '% of Total Project Budget',
            name: 'percentageTotalProjectBudget',
            data: 'percentageTotalProjectBudget',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getTotalPaidAmountColumn(columnIndex) {
        return {
            title: 'Total Paid Amount $',
            name: 'totalPaidAmount',
            data: 'paymentInfo',
            className: 'data-table-header currency-display',
            render: function (data) {
                let totalPaid = data?.totalPaid ?? '';
                return formatter.format(totalPaid);
            },
            index: columnIndex
        };
    }

    function getElectoralDistrictColumn(columnIndex) {
        return {
            title: 'Project Electoral District',
            name: 'electoralDistrict',
            data: 'electoralDistrict',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getApplicantElectoralDistrictColumn(columnIndex) {
        return {
            title: 'Applicant Electoral District',
            name: 'applicantElectoralDistrict',
            data: 'applicantElectoralDistrict',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getForestryOrNonForestryColumn(columnIndex) {
        return {
            title: 'Forestry or Non-Forestry',
            name: 'forestryOrNonForestry',
            data: 'forestry',
            className: 'data-table-header',
            render: function (data) {
                if (data != null)
                    return data == 'FORESTRY' ? 'Forestry' : 'Non Forestry';
                else
                    return '';
            },
            index: columnIndex
        };
    }

    function getForestryFocusColumn(columnIndex) {
        return {
            title: 'Forestry Focus',
            name: 'forestryFocus',
            data: 'forestryFocus',
            className: 'data-table-header',
            render: function (data) {
                if (data) {
                    if (data == 'PRIMARY') {
                        return 'Primary processing'
                    }
                    else if (data == 'SECONDARY') {
                        return 'Secondary/Value-Added/Not Mass Timber'
                    } else if (data == 'MASS_TIMBER') {
                        return 'Mass Timber';
                    } else if (data != '') {
                        return data;
                    } else {
                        return '';
                    }
                }
                else {
                    return '';
                }
            },
            index: columnIndex
        };
    }

    function getAcquisitionColumn(columnIndex) {
        return {
            title: 'Acquisition',
            name: 'acquisition',
            data: 'acquisition',
            className: 'data-table-header',
            render: function (data) {
                if (data) {
                    return titleCase(data);
                }
                else {
                    return '';
                }
            },
            index: columnIndex
        };
    }

    function getCityColumn(columnIndex) {
        return {
            title: 'City',
            name: 'city',
            data: 'city',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getCommunityPopulationColumn(columnIndex) {
        return {
            title: 'Community Population',
            name: 'communityPopulation',
            data: 'communityPopulation',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getLikelihoodOfFundingColumn(columnIndex) {
        return {
            title: 'Likelihood of Funding',
            name: 'likelihoodOfFunding',
            data: 'likelihoodOfFunding',
            className: 'data-table-header',
            render: function (data) {
                if (data != null) {
                    return titleCase(data);
                }
                else {
                    return '';
                }
            },
            index: columnIndex
        };
    }

    function getSubStatusColumn(columnIndex) {
        return {
            title: 'Sub-Status',
            name: 'subStatusDisplayValue',
            data: 'subStatusDisplayValue',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getTagsColumn(columnIndex) {
        return {
            title: 'Tags',
            name: 'applicationTag',
            data: 'applicationTag',
            className: '',
            render: function (data) {
                if (data && Array.isArray(data)) {
                    let tagNames = data
                        .filter(x => x?.tag?.name)
                        .map(x => x.tag.name);
                    return tagNames.join(', ') ?? '';
                }
                return '';
            },
            index: columnIndex
        };
    }

    function getTotalScoreColumn(columnIndex) {
        return {
            title: 'Total Score',
            name: 'totalScore',
            data: 'totalScore',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getAssessmentResultColumn(columnIndex) {
        return {
            title: 'Assessment Result',
            name: 'assessmentResult',
            data: 'assessmentResultStatus',
            className: 'data-table-header',
            render: function (data) {
                if (data != null) {
                    return titleCase(data);
                }
                else {
                    return '';
                }
            },
            index: columnIndex
        };
    }

    function getRecommendedAmountColumn(columnIndex) {
        return {
            title: 'Recommended Amount',
            name: 'recommendedAmount',
            data: 'recommendedAmount',
            className: 'data-table-header currency-display',
            render: function (data) {
                return formatter.format(data) ?? '';
            },
            index: columnIndex
        };
    }

    function getDueDateColumn(columnIndex) {
        return {
            title: 'Due Date',
            name: 'dueDate',
            data: 'dueDate',
            className: 'data-table-header',
            render: function (data) {
                return data != null ? luxon.DateTime.fromISO(data, {
                    locale: abp.localization.currentCulture.name,
                }).toUTC().toLocaleString() : '';
            },
            index: columnIndex
        };
    }

    function getOwnerColumn(columnIndex) {
        return {
            title: 'Owner',
            name: 'Owner',
            data: 'owner',
            className: 'data-table-header',
            render: function (data) {
                return data != null ? data.fullName : '';
            },
            index: columnIndex
        };
    }

    function getDecisionDateColumn(columnIndex) {
        return {
            title: 'Decision Date',
            name: 'finalDecisionDate',
            data: 'finalDecisionDate',
            className: 'data-table-header',
            render: function (data) {
                return data != null ? luxon.DateTime.fromISO(data, {
                    locale: abp.localization.currentCulture.name,
                }).toUTC().toLocaleString() : '';
            },
            index: columnIndex
        };
    }

    function getProjectSummaryColumn(columnIndex) {
        return {
            title: 'Project Summary',
            name: 'projectSummary',
            data: 'projectSummary',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getOrganizationTypeColumn(columnIndex) {
        return {
            title: 'Organization Type',
            name: 'organizationType',
            data: 'organizationType',
            className: 'data-table-header',
            render: function (data) {
                return getFullType(data) ?? '';
            },
            index: columnIndex
        };
    }

    function getOrganizationNameColumn(columnIndex) {
        return {
            title: l('Summary:Application.OrganizationName'),
            name: 'organizationName',
            data: 'organizationName',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getBusinessNumberColumn(columnIndex) {
        return {
            title: l('Summary:Application.BusinessNumber'),
            name: 'businessNumber',
            data: 'applicant.businessNumber',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getNonRegisteredOrganizationNameColumn(columnIndex) {
        return {
            title: l('Summary:Application.NonRegOrgName'),
            name: 'nonRegOrgName',
            data: 'nonRegOrgName',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getUnityApplicationIdColumn(columnIndex) {
        return {
            title: 'Unity Application Id',
            name: 'unityApplicationId',
            data: 'unityApplicationId',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getDueDiligenceStatusColumn(columnIndex) {
        return {
            title: 'Due Diligence Status',
            name: 'dueDiligenceStatus',
            data: 'dueDiligenceStatus',
            className: 'data-table-header',
            render: function (data) {
                return titleCase(data ?? '') ?? '';
            },
            index: columnIndex
        };
    }

    function getDeclineRationaleColumn(columnIndex) {
        return {
            title: 'Decline Rationale',
            name: 'declineRationale',
            data: 'declineRational',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getContactFullNameColumn(columnIndex) {
        return {
            title: 'Contact Full Name',
            name: 'contactFullName',
            data: 'contactFullName',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getContactTitleColumn(columnIndex) {
        return {
            title: 'Contact Title',
            name: 'contactTitle',
            data: 'contactTitle',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getContactEmailColumn(columnIndex) {
        return {
            title: 'Contact Email',
            name: 'contactEmail',
            data: 'contactEmail',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getContactBusinessPhoneColumn(columnIndex) {
        return {
            title: 'Contact Business Phone',
            name: 'contactBusinessPhone',
            data: 'contactBusinessPhone',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getContactCellPhoneColumn(columnIndex) {
        return {
            title: 'Contact Cell Phone',
            name: 'contactCellPhone',
            data: 'contactCellPhone',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getSectorSubSectorIndustryDescColumn(columnIndex) {
        return {
            title: 'Other Sector/Sub/Industry Description',
            name: 'sectorSubSectorIndustryDesc',
            data: 'applicant.sectorSubSectorIndustryDesc',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getSigningAuthorityFullNameColumn(columnIndex) {
        return {
            title: 'Signing Authority Full Name',
            name: 'signingAuthorityFullName',
            data: 'signingAuthorityFullName',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getSigningAuthorityTitleColumn(columnIndex) {
        return {
            title: 'Signing Authority Title',
            name: 'signingAuthorityTitle',
            data: 'signingAuthorityTitle',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getSigningAuthorityEmailColumn(columnIndex) {
        return {
            title: 'Signing Authority Email',
            name: 'signingAuthorityEmail',
            data: 'signingAuthorityEmail',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getSigningAuthorityBusinessPhoneColumn(columnIndex) {
        return {
            title: 'Signing Authority Business Phone',
            name: 'signingAuthorityBusinessPhone',
            data: 'signingAuthorityBusinessPhone',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getSigningAuthorityCellPhoneColumn(columnIndex) {
        return {
            title: 'Signing Authority Cell Phone',
            name: 'signingAuthorityCellPhone',
            data: 'signingAuthorityCellPhone',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getPlaceColumn(columnIndex) {
        return {
            title: 'Place',
            name: 'place',
            data: 'place',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getRiskRankingColumn(columnIndex) {
        return {
            title: 'Risk Ranking',
            name: 'riskranking',
            data: 'riskRanking',
            className: 'data-table-header',
            render: function (data) {
                return titleCase(data ?? '') ?? '';
            },
            index: columnIndex
        };
    }

    function getNotesColumn(columnIndex) {
        return {
            title: 'Notes',
            name: 'notes',
            data: 'notes',
            className: 'data-table-header multi-line',
            width: "20rem",
            createdCell: function (td) {
                $(td).css('min-width', '20rem');
            },
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getRedStopColumn(columnIndex) {
        return {
            title: 'Red-Stop',
            name: 'redstop',
            data: 'applicant.redStop',
            className: 'data-table-header',
            render: function (data) {
                return convertToYesNo(data);
            },
            index: columnIndex
        };
    }

    function getIndigenousColumn(columnIndex) {
        return {
            title: 'Indigenous',
            name: 'indigenous',
            data: 'applicant.indigenousOrgInd',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getFyeDayColumn(columnIndex) {
        return {
            title: 'FYE Day',
            name: 'fyeDay',
            data: 'applicant.fiscalDay',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getFyeMonthColumn(columnIndex) {
        return {
            title: 'FYE Month',
            name: 'fyeMonth',
            data: 'applicant.fiscalMonth',
            className: 'data-table-header',
            render: function (data) {
                if (data) {
                    return titleCase(data);
                }
                else {
                    return '';
                }
            },
            index: columnIndex
        };
    }

    function getApplicantIdColumn(columnIndex) {
        return {
            title: 'Applicant Id',
            name: 'applicantId',
            data: 'applicant.unityApplicantId',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: columnIndex
        };
    }

    function getPayoutColumn(columnIndex) {
        return {
            title: 'Payout',
            name: 'paymentInfo',
            data: 'paymentInfo',
            className: 'data-table-header',
            render: function (data) {
                return payoutDefinition(data?.approvedAmount ?? 0, data?.totalPaid ?? 0);
            },
            index: columnIndex
        };
    }

    // Helper functions
    function titleCase(str) {
        if (!str) return '';
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

    function payoutDefinition(approvedAmount, totalPaid) {
        if ((approvedAmount > 0 && totalPaid > 0) && (approvedAmount === totalPaid)) {
            return 'Fully Paid';
        } else if (totalPaid === 0) {
            return '';
        } else {
            return 'Partially Paid';
        }
    }

    function getNames(data) {
        let name = '';
        data.forEach((d, index) => {
            name = name + (' ' + d.fullName + getDutyText(d));
            if (index != (data.length - 1)) {
                name = name + ',';
            }
        });

        return name;
    }

    function getDutyText(data) {
        return data.duty ? (" [" + data.duty + "]") : '';
    }
});
