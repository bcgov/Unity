﻿$(function () {
    const formatter = createNumberFormatter();
    const l = abp.localization.getResource('GrantManager');
    let dt = $('#GrantApplicationsTable');
    let dataTable;

    const listColumns = getColumns();
    const defaultVisibleColumns = ['select',
        'applicantName',
        'referenceNo',
        'category',
        'submissionDate',
        'projectName',
        'subsector',
        'totalProjectBudget',
        'assignees',
        'status',
        'requestedAmount',
        'approvedAmount',
        'economicRegion',
        'regionalDistrict',
        'community',
        'orgNumber',
        'orgBookStatus'];

    //For stateRestore label in modal
    let languageSetValues = {
        buttons: {
            stateRestore: 'View %d'
        },
        stateRestore:
        {
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
    }

    let actionButtons = [
        {
            extend: 'csv',
            text: 'Export',
            className: 'custom-table-btn flex-none btn btn-secondary',
            exportOptions: {
                columns: ':visible:not(.notexport)',
                orthogonal: 'fullName',
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
                { extend: 'removeAllStates', text: 'Delete All Views' },
                {
                    extend: 'spacer',
                    style: 'bar',
                }
            ]
        }
    ];

    let responseCallback = function (result) {
        return {
            recordsTotal: result.totalCount,
            recordsFiltered: result.totalCount,
            data: result.items
        };
    };

    dataTable = initializeDataTable({
        dt,
        defaultVisibleColumns,
        listColumns,
        maxRowsPerPage: 10,
        defaultSortColumn: 4,
        dataEndpoint: unity.grantManager.grantApplications.grantApplication.getList,
        data: {},
        responseCallback,
        actionButtons,
        serverSideEnabled: false,
        pagingEnabled: true,
        reorderEnabled: true,
        languageSetValues,
        dataTableName: 'GrantApplicationsTable',
        dynamicButtonContainerId: 'dynamicButtonContainerId'
    });

    dataTable.on('search.dt', () => handleSearch());

    dataTable.on('select', function (e, dt, type, indexes) {

        if (indexes?.length) {
            indexes.forEach(index => {
                $("#row_" + index).prop("checked", true);
                if ($(".chkbox:checked").length == $(".chkbox").length) {
                    $(".select-all-applications").prop("checked", true);
                }
                selectApplication(type, index, 'select_application');
            });
        }

    });

    dataTable.on('deselect', function (e, dt, type, indexes) {
        if (indexes?.length) {
            indexes.forEach(index => {
                selectApplication(type, index, 'deselect_application');
                $("#row_" + index).prop("checked", false);
                if ($(".chkbox:checked").length != $(".chkbox").length) {
                    $(".select-all-applications").prop("checked", false);
                }
            });
        }
    });

    $('#search').on('input', function () {
        let table = $('#GrantApplicationsTable').DataTable();
        table.search($(this).val()).draw();
    });

    //For savedStates
    $('.grp-savedStates').text('Save View');
    $('.grp-savedStates').closest('.btn-group').addClass('cstm-save-view');

    function selectApplication(type, indexes, action) {
        if (type === 'row') {
            let data = dataTable.row(indexes).data();
            PubSub.publish(action, data);
        }
    }

    function handleSearch() {
        let filter = $('.dataTables_filter input').val();
        console.info(filter);
    }

    function getColumns() {
        return [
            getSelectColumn('Select Application', 'rowCount','applications'),
            getApplicantNameColumn(),
            getApplicationNumberColumn(),
            getCategoryColumn(),
            getSubmissionDateColumn(),
            getProjectNameColumn(),
            getSectorColumn(),
            getSubSectorColumn(),
            getTotalProjectBudgetColumn(),
            getAssigneesColumn(),
            getStatusColumn(),
            getRequestedAmountColumn(),
            getApprovedAmountColumn(),
            getEconomicRegionColumn(),
            getRegionalDistrictColumn(),
            getCommunityColumn(),
            getOrganizationNumberColumn(),
            getOrgBookStatusColumn(),
            getProjectStartDateColumn(),
            getProjectEndDateColumn(),
            getProjectedFundingTotalColumn(),
            getTotalProjectBudgetPercentageColumn(),
            getTotalPaidAmountColumn(),
            getElectoralDistrictColumn(),
            getApplicantElectoralDistrictColumn(),
            getForestryOrNonForestryColumn(),
            getForestryFocusColumn(),
            getAcquisitionColumn(),
            getCityColumn(),
            getCommunityPopulationColumn(),
            getLikelihoodOfFundingColumn(),
            getSubStatusColumn(),
            getTagsColumn(),
            getTotalScoreColumn(),
            getAssessmentResultColumn(),
            getRecommendedAmountColumn(),
            getDueDateColumn(),
            getOwnerColumn(),
            getDecisionDateColumn(),
            getProjectSummaryColumn(),
            getOrganizationTypeColumn(),
            getOrganizationNameColumn(),
            getDueDiligenceStatusColumn(),
            getDeclineRationaleColumn(),
            getContactFullNameColumn(),
            getContactTitleColumn(),
            getContactEmailColumn(),
            getContactBusinessPhoneColumn(),
            getContactCellPhoneColumn(),
            getSectorSubSectorIndustryDescColumn(),
            getSigningAuthorityFullNameColumn(),
            getSigningAuthorityTitleColumn(),
            getSigningAuthorityEmailColumn(),
            getSigningAuthorityBusinessPhoneColumn(),
            getSigningAuthorityCellPhoneColumn(),
            getPlaceColumn(),
            getRiskRankingColumn(),
            getNotesColumn(),
            getRedStopColumn(),
            getIndigenousColumn(),
            getFyeDayColumn(),
            getFyeMonthColumn(),
            getApplicantIdColumn(),
            getPayoutColumn()
        ]
            .map((column) => ({ ...column, targets: [column.index], orderData: [column.index, 0] }));
    }

    function getApplicantNameColumn() {
        return {
            title: 'Applicant Name',
            data: 'applicant.applicantName',
            name: 'applicantName',
            className: 'data-table-header',
            index: 1
        }
    }

    function getApplicationNumberColumn() {
        return {
            title: 'Submission #',
            data: 'referenceNo',
            name: 'referenceNo',
            className: 'data-table-header text-nowrap',
            render: function (data, type, row) {                
                return `<a href="/GrantApplications/Details?ApplicationId=${row.id}">${data}</a>`;
            },
            index: 2
        }
    }

    function getCategoryColumn() {
        return {
            title: 'Category',
            data: 'category',
            name: 'category',
            className: 'data-table-header',
            index: 3
        }
    }

    function getSubmissionDateColumn() {
        return {
            title: l('SubmissionDate'),
            data: 'submissionDate',
            name: 'submissionDate',
            className: 'data-table-header',
            render: DataTable.render.date('YYYY-MM-DD', abp.localization.currentCulture.name),
            index: 4
        }
    }

    function getProjectNameColumn() {
        return {
            title: 'Project Name',
            data: 'projectName',
            name: 'projectName',
            className: 'data-table-header',
            index: 5
        }
    }

    function getSectorColumn() {
        return {
            title: 'Sector',
            name: 'sector',
            data: 'applicant.sector',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 6
        }
    }

    function getSubSectorColumn() {
        return {
            title: 'SubSector',
            name: 'subsector',
            data: 'applicant.subSector',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 7
        }
    }

    function getTotalProjectBudgetColumn() {
        return {
            title: 'Total Project Budget',
            name: 'totalProjectBudget',
            data: 'totalProjectBudget',
            className: 'data-table-header currency-display',
            render: function (data) {
                return formatter.format(data);
            },
            index: 8
        }
    }

    function getAssigneesColumn() {
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
            index: 9
        }
    }

    function getDutyText(data) {
        return data.duty ? (" [" + data.duty + "]") : '';
    }

    function getStatusColumn() {
        return {
            title: l('GrantApplicationStatus'),
            data: 'status',
            name: 'status',
            className: 'data-table-header',
            index: 10
        }
    }

    function getRequestedAmountColumn() {
        return {
            title: l('RequestedAmount'),
            data: 'requestedAmount',
            name: 'requestedAmount',
            className: 'data-table-header currency-display',
            render: function (data) {
                return formatter.format(data);
            },
            index: 11
        }
    }

    function getApprovedAmountColumn() {
        return {
            title: 'Approved Amount',
            name: 'approvedAmount',
            data: 'approvedAmount',
            className: 'data-table-header currency-display',
            render: function (data) {
                return formatter.format(data);
            },
            index: 12
        }
    }

    function getEconomicRegionColumn() {
        return {
            title: 'Economic Region',
            name: 'economicRegion',
            data: 'economicRegion',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 13
        }
    }

    function getRegionalDistrictColumn() {
        return {
            title: 'Regional District',
            name: 'regionalDistrict',
            data: 'regionalDistrict',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 14
        }
    }

    function getCommunityColumn() {
        return {
            title: 'Community',
            name: 'community',
            data: 'community',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 15
        }
    }

    function getOrganizationNumberColumn() {
        return {
            title: l('ApplicantInfoView:ApplicantInfo.OrgNumber'),
            name: 'orgNumber',
            data: 'applicant.orgNumber',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                return data ?? '';
            },
            index: 16
        }
    }

    function getOrgBookStatusColumn() {
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
            index: 17
        }
    }

    function getProjectStartDateColumn() {
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
            index: 18
        }
    }

    function getProjectEndDateColumn() {
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
            index: 19
        }
    }

    function getProjectedFundingTotalColumn() {
        return {
            title: 'Projected Funding Total',
            name: 'projectFundingTotal',
            data: 'projectFundingTotal',
            className: 'data-table-header currency-display',
            render: function (data) {
                return formatter.format(data) ?? '';
            },
            index: 20
        }
    }

    function getTotalProjectBudgetPercentageColumn() {
        return {
            title: '% of Total Project Budget',
            name: 'percentageTotalProjectBudget',
            data: 'percentageTotalProjectBudget',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 21
        }
    }

    function getTotalPaidAmountColumn() {
        return {
            title: 'Total Paid Amount $',
            name: 'totalPaidAmount',
            data: 'totalPaidAmount',
            className: 'data-table-header currency-display',
            render: function (data) {
                return '';
            },
            index: 22
        }
    }

    function getElectoralDistrictColumn() {
        return {
            title: 'Project Electoral District',
            name: 'electoralDistrict',
            data: 'electoralDistrict',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 23
        }
    }

    function getForestryOrNonForestryColumn() {
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
            index: 24
        }
    }

    function getForestryFocusColumn() {
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
            index: 25
        }
    }

    function getAcquisitionColumn() {
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
            index: 26
        }
    }

    function getCityColumn() {
        return {
            title: 'City',
            name: 'city',
            data: 'city',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';

            },
            index: 27
        }
    }

    function getCommunityPopulationColumn() {
        return {
            title: 'Community Population',
            name: 'communityPopulation',
            data: 'communityPopulation',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 28
        }
    }

    function getLikelihoodOfFundingColumn() {
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
            index: 29
        }
    }

    function getSubStatusColumn() {
        return {
            title: 'Sub-Status',
            name: 'subStatusDisplayValue',
            data: 'subStatusDisplayValue',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 30
        }
    }

    function getTagsColumn() {
        return {
            title: 'Tags',
            name: 'applicationTag',
            data: 'applicationTag',
            className: '',
            render: function (data) {
                return data.replace(/,/g, ', ') ?? '';
            },
            index: 31
        }
    }

    function getTotalScoreColumn() {
        return {
            title: 'Total Score',
            name: 'totalScore',
            data: 'totalScore',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 32
        }
    }

    function getAssessmentResultColumn() {
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
            index: 33
        }
    }

    function getRecommendedAmountColumn() {
        return {
            title: 'Recommended Amount',
            name: 'recommendedAmount',
            data: 'recommendedAmount',
            className: 'data-table-header currency-display',
            render: function (data) {
                return formatter.format(data) ?? '';
            },
            index: 34
        }
    }

    function getDueDateColumn() {
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
            index: 35
        }
    }

    function getOwnerColumn() {
        return {
            title: 'Owner',
            name: 'Owner',
            data: 'owner',
            className: 'data-table-header',
            render: function (data) {
                return data != null ? data.fullName : '';
            },
            index: 36
        }
    }

    function getDecisionDateColumn() {
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
            index: 37
        }
    }

    function getProjectSummaryColumn() {
        return {
            title: 'Project Summary',
            name: 'projectSummary',
            data: 'projectSummary',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 38
        }
    }

    function getOrganizationTypeColumn() {
        return {
            title: 'Organization Type',
            name: 'organizationType',
            data: 'organizationType',
            className: 'data-table-header',
            render: function (data) {
                return getFullType(data) ?? '';
            },
            index: 39
        }
    }

    function getOrganizationNameColumn() {
        return {
            title: l('Summary:Application.OrganizationName'),
            name: 'organizationName',
            data: 'organizationName',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 40
        }
    }
    function getDueDiligenceStatusColumn() {
        return {
            title: 'Due Diligence Status',
            name: 'dueDiligenceStatus',
            data: 'dueDiligenceStatus',
            className: 'data-table-header',
            render: function (data) {
                return titleCase(data ?? '') ?? '';
            },
            index: 41
        }
    }

    function getDeclineRationaleColumn() {
        return {
            title: 'Decline Rationale',
            name: 'declineRationale',
            data: 'declineRational',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 42
        }
    }

    function getContactFullNameColumn() {
        return {
            title: 'Contact Full Name',
            name: 'contactFullName',
            data: 'contactFullName',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 43
        }
    }
    function getContactTitleColumn() {
        return {
            title: 'Contact Title',
            name: 'contactTitle',
            data: 'contactTitle',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 44
        }
    }
    function getContactEmailColumn() {
        return {
            title: 'Contact Email',
            name: 'contactEmail',
            data: 'contactEmail',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 45
        }
    }
    function getContactBusinessPhoneColumn() {
        return {
            title: 'Contact Business Phone',
            name: 'contactBusinessPhone',
            data: 'contactBusinessPhone',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 46
        }
    }
    function getContactCellPhoneColumn() {
        return {
            title: 'Contact Cell Phone',
            name: 'contactCellPhone',
            data: 'contactCellPhone',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 47
        }
    }

    function getSectorSubSectorIndustryDescColumn() {
        return {
            title: 'Other Sector/Sub/Industry Description',
            name: 'sectorSubSectorIndustryDesc',
            data: 'applicant.sectorSubSectorIndustryDesc',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 48
        }
    }

    function getSigningAuthorityFullNameColumn() {
        return {
            title: 'Signing Authority Full Name',
            name: 'signingAuthorityFullName',
            data: 'signingAuthorityFullName',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 49
        }
    }
    function getSigningAuthorityTitleColumn() {
        return {
            title: 'Signing Authority Title',
            name: 'signingAuthorityTitle',
            data: 'signingAuthorityTitle',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 50
        }
    }
    function getSigningAuthorityEmailColumn() {
        return {
            title: 'Signing Authority Email',
            name: 'signingAuthorityEmail',
            data: 'signingAuthorityEmail',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 51
        }
    }
    function getSigningAuthorityBusinessPhoneColumn() {
        return {
            title: 'Signing Authority Business Phone',
            name: 'signingAuthorityBusinessPhone',
            data: 'signingAuthorityBusinessPhone',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 52
        }
    }
    function getSigningAuthorityCellPhoneColumn() {
        return {
            title: 'Signing Authority Cell Phone',
            name: 'signingAuthorityCellPhone',
            data: 'signingAuthorityCellPhone',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 53
        }
    }
    function getPlaceColumn() {
        return {
            title: 'Place',
            name: 'place',
            data: 'place',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 54
        }
    }

    function getRiskRankingColumn() {
        return {
            title: 'Risk Ranking',
            name: 'riskranking',
            data: 'riskRanking',
            className: 'data-table-header',
            render: function (data) {
                return titleCase(data ?? '') ?? '';
            },
            index: 55
        }
    }

    function getNotesColumn() {
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
            index: 56
        }
    }

    function getRedStopColumn() {
        return {
            title: 'Red-Stop',
            name: 'redstop',
            data: 'applicant.redStop',
            className: 'data-table-header',
            render: function (data) {
                return convertToYesNo(data);
            },
            index: 57
        }
    }

    function getIndigenousColumn() {
        return {
            title: 'Indigenous',
            name: 'indigenous',
            data: 'applicant.indigenousOrgInd',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 58
        }
    }

    function getFyeDayColumn() {
        return {
            title: 'FYE Day',
            name: 'fyeDay',
            data: 'applicant.fiscalDay',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 59
        }
    }

    function getFyeMonthColumn() {
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
            index: 60
        }
    }

    function getApplicantIdColumn() {
        return {
            title: 'Applicant Id',
            name: 'applicantId',
            data: 'applicant.unityApplicantId',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 61
        }
    }

    function getPayoutColumn() {
        return {
            title: 'Payout',
            name: 'paymentInfo',
            data: 'paymentInfo',
            className: 'data-table-header',
            render: function (data) {
                return payoutDefinition(data?.approvedAmount ?? 0, data?.totalPaid ?? 0);
            },
            index: 62
        }
    }

    function getApplicantElectoralDistrictColumn() {
        return {
            title: 'Applicant Electoral District',
            name: 'applicantElectoralDistrict',
            data: 'applicant.electoralDistrict',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 63
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


    window.addEventListener('resize', () => {
    });

    PubSub.subscribe(
        'refresh_application_list',
        (msg, data) => {
            dataTable.ajax.reload(null, false);
            $(".select-all-applications").prop("checked", false);
            PubSub.publish('clear_selected_application');
        }
    );

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

    $('.select-all-applications').click(function () {
        if ($(this).is(':checked')) {
            dataTable.rows({ 'page': 'current' }).select();
        }
        else {
            dataTable.rows({ 'page': 'current' }).deselect();
        }
    });
});
function payoutDefinition(approvedAmount, totalPaid) {
    if ((approvedAmount > 0 && totalPaid > 0) && (approvedAmount  === totalPaid)) {
        return 'Fully Paid';
    } else if (totalPaid === 0) {
        return '';
    } else {
        return 'Partially Paid';
    }
}