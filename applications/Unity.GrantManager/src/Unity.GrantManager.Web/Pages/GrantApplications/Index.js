$(function () {
    const formatter = createNumberFormatter();
    const l = abp.localization.getResource('GrantManager');
    const maxRowsPerPage = 15;
    let dt = $('#GrantApplicationsTable');
    let dataTable;
    /* let mapTitles = new Map(); = not used */
    /* commented out clear filter functionality - needs to be looked at again or deleted */

    const listColumns = getColumns(); //init columns before table init
    dataTable = initializeDataTable();
    dataTable.buttons().container().prependTo('#dynamicButtonContainerId');
    dataTable.on('search.dt', () => handleSearch());

    /* Removed for now - to be added/looked at later
    $('#dynamicButtonContainerId').prepend($('.csv-download:eq(0)'));
    $('#dynamicButtonContainerId').prepend($('.cln-visible:eq(0)'));
    */

    const UIElements = {
        searchBar: $('#search-bar'),
        btnToggleFilter: $('#btn-toggle-filter'),
        /* ClearFilter filterIcon: $("i.fl.fl-filter"), */
        /* ClearFilter clearFilter: $('#btn-clear-filter') */
    };    
    init();
    function init() {
        $('.custom-table-btn').removeClass('dt-button buttons-csv buttons-html5');
        $('.csv-download').prepend('<i class="fl fl-export"></i>');
        $('.cln-visible').prepend('<i class="fl fl-settings"></i>');
        bindUIEvents();
        /* ClearFilter UIElements.clearFilter.html("<span class='x-mark'>X</span>" + UIElements.clearFilter.html()); */
        dataTable.search('').columns().search('').draw();
    }

    function bindUIEvents() {
        UIElements.btnToggleFilter.on('click', toggleFilterRow);
        /* ClearFilter UIElements.filterIcon.on('click', $('#dtFilterRow').toggleClass('hidden')); */
        /* ClearFilter UIElements.clearFilter.on('click', clearFilter); */
    }

    dataTable.on('select', function (e, dt, type, indexes) {
        selectApplication(type, indexes, 'select_application');
    });

    dataTable.on('deselect', function (e, dt, type, indexes) {
        selectApplication(type, indexes, 'deselect_application');
    });

    function selectApplication(type, indexes, action) {
        if (type === 'row') {
            let data = dataTable.row(indexes).data();
            PubSub.publish(action, data);
        }
    }

    function toggleFilterRow() {
        $('#dtFilterRow').toggleClass('hidden');
    }

    /* Clear filter button removed - to review if needed again
    function clearFilter() {        
        $(".filter-input").each(function () {
            if (this.value != "") {
                this.value = "";
                dataTable
                    .columns(mapTitles.get(this.placeholder))
                    .search(this.value)
                    .draw();
            }
        });

        $('#btn-clear-filter')[0].disabled = true;
    }
    */

    function handleSearch() {
        let filterValue = $('.dataTables_filter input').val();
        if (filterValue.length > 0) {
            $('#externalLink').prop('disabled', true);
            $('#applicationLink').prop('disabled', true);
            Array.from(document.getElementsByClassName('selected')).forEach(
                function (element, index, array) {
                    element.classList.toggle('selected');
                }
            );
            PubSub.publish("deselect_application", "reset_data");
        }
    }

    function createNumberFormatter() {
        return new Intl.NumberFormat('en-CA', {
            style: 'currency',
            currency: 'CAD',
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        });
    }

    function initializeDataTable() {
        return dt.DataTable(
            abp.libs.datatables.normalizeConfiguration({
                fixedHeader: {
                    header: true,
                    footer: false,
                    headerOffset: 0
                },
                serverSide: false,
                paging: true,
                order: [[4, 'desc']],
                searching: true,
                pageLength: maxRowsPerPage,
                scrollX: true,
                ajax: abp.libs.datatables.createAjax(
                    unity.grantManager.grantApplications.grantApplication.getList
                ),
                select: {
                    style: 'multiple',
                    selector: 'td:not(:nth-child(8))',
                },
                colReorder: true,
                orderCellsTop: true,
                //fixedHeader: true,
                stateSave: true,
                stateDuration: 0,
                dom: 'Bfrtip',
                buttons: [
                    {
                        extend: 'csv',
                        text: 'Export',
                        className: 'btn btn-light custom-table-btn csv-download',
                        exportOptions: {
                            columns: ':visible:not(.notexport)',
                            orthogonal: 'fullName',
                        }
                    },
                    {
                        extend: 'colvis',
                        text: 'Manage Columns',
                        columns: getColumnsForManageList(),
                        className: 'btn btn-light custom-table-btn cln-visible',
                    }
                ],
                drawCallback: function () {
                    let $api = this.api();
                    let pages = $api.page.info().pages;
                    let rows = $api.data().length;

                    // Tailor the settings based on the row count
                    if (rows <= maxRowsPerPage) {
                        $('.dataTables_info').css('display', 'none');
                        $('.dataTables_paginate').css('display', 'none');
                        $('.dataTables_length').css('display', 'none');
                    } else if (pages === 1) {
                        // With this current length setting, not more than 1 page, hide pagination
                        $('.dataTables_info').css('display', 'none');
                        $('.dataTables_paginate').css('display', 'none');
                    } else {
                        // SHow everything
                        $('.dataTables_info').css('display', 'block');
                        $('.dataTables_paginate').css('display', 'block');
                    }
                    setTableHeighDynamic();
                },
                initComplete: function () {
                    updateFilter();
                },
                columns: listColumns,
                columnDefs: [
                    {
                        targets: getColumnsVisibleByDefault(),
                        visible: true
                    },
                    {
                        targets: '_all',
                        visible: false // Hide all other columns initially
                    }
                ],
            })
        );
    }

    function getColumnsVisibleByDefault() {
        const columnNames = ['select',
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
            'orgBookStatus'
        ];
        return columnNames
            .map((name) => getColumnByName(name).index);        
    }

    function getColumnByName(name) {
        return listColumns.find(obj => obj.name === name);
    }

    function getColumnsForManageList() {
        let exludeIndxs = [0];
        return listColumns
            .map((obj) => ({ title: obj.title, data: obj.data, visible: obj.visible, index: obj.index }))
            .filter(obj => !exludeIndxs.includes(obj.index))
            .sort((a, b) => a.title.localeCompare(b.title))
            .map(a => a.index);        
    }

    function getColumns() {
        return [
            getSelectColumn(),
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
            getPercentageTotalProjectBudgetColumn(),
            getOrganizationTypeColumn(),
            getOrganizationNameColumn(),
            getDueDiligenceStatusColumn(),
            getDeclineRationaleColumn(),         
            getContactFullNameColumn(),
            getContactTitleColumn(),
            getContactEmailColumn(),
            getContactBusinessPhoneColumn(),
            getContactCellPhoneColumn()
        ]
        .map((column) => ({ ...column, targets: [column.index], orderData: [column.index, 0] }));
    }

    function getSelectColumn() {
        return {
            title: '<span class="btn btn-secondary btn-light fl fl-filter" title="Toggle Filter" id="btn-toggle-filter"></span>',
            orderable: false,
            className: 'notexport',
            data: 'rowCount',
            name: 'select',
            render: function (data) {
                return '<div class="select-checkbox" title="Select Application" ></div>';
            },
            index: 0
        }
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
            title: 'Application #',
            data: 'referenceNo',
            name: 'referenceNo',
            className: 'data-table-header',
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
            render: function (data) {
                return luxon.DateTime.fromISO(data, {
                    locale: abp.localization.currentCulture.name,
                }).toLocaleString();
            },
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
                return data ?? '{Sector}';
            },
            index: 6
        }
    }

    function getSubSectorColumn() {
        return {
            title: 'SubSector',
            name: 'subsector',
            data: 'applicant.subsector',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '{SubSector}';
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
                    displayText = type === 'fullName' ? getNames(data) : data[0].fullName;
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

    function getStatusColumn() {
        return {
            title: l('GrantApplicationStatus'),
            data: 'status',
            name: 'status',
            className: 'data-table-header',
            render: function (data, type, row) {
                let fill = row.assessmentReviewCount > 0 ? 'fas' : 'far';
                return `<span class="d-flex align-items-center"><i class="${fill} fa-bookmark text-primary"></i><span class="ps-2 flex-fill">${row.status}</span></span>`;
            },
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
                return data ?? '{Region}';
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
                return data ?? '{Regional District}';
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
                return data ?? '{Community}';
            },
            index: 15
        }
    }

    function getOrganizationNumberColumn() {
        return {
            title: 'Organization Number',
            name: 'orgNumber',
            data: 'applicant.orgNumber',
            className: 'data-table-header',
            visible: false,
            render: function (data) {
                return data ?? '{Organization Number}';
            },
            index: 16
        }
    }

    function getOrgBookStatusColumn() {
        return {
            title: 'Org Book Status',
            name: 'orgBookStatus',
            data: 'orgBookStatus',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '{Org Book Status}';
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
                }).toUTC().toLocaleString() : '{Project Start Date}';
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
                }).toUTC().toLocaleString() : '{Project End Date}';
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
                return formatter.format(data) ?? '{Projected Funding Total}';
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
                return data ?? '{% of Total Project Budget}';
            },
            index: 21
        }
    }

    function getTotalPaidAmountColumn() {
        return {
            title: 'Total Paid Amount $',
            name: 'projectFundingTotal',
            data: 'projectFundingTotal',
            className: 'data-table-header currency-display',
            render: function (data) {
                return formatter.format(data) ?? '{Total Paid Amount $}';
            },
            index: 22
        }
    }

    function getElectoralDistrictColumn() {
        return {
            title: 'Electoral District',
            name: 'electoralDistrict',
            data: 'electoralDistrict',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '{Electoral District}';
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
                    return '{Forestry or Non-Forestry}';
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
                    } else {
                        return '{Forestry Focus}';
                    }
                }
                else {
                    return '{Forestry Focus}';
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
                    return '{Acquisition}';
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
                return data ?? '{city}';

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
                return data ?? '{Community Population}';
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
                    return '{Likelihood of Funding}';
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
                return data ?? '{SubStatus}';
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
                return data.replace(/,/g, ', ') ?? '{Tags}';
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
                return data ?? '{Total Score}';
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
                    return '{Assessment Result}';
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
                return formatter.format(data) ?? '{Recommended Amount}';
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
                }).toUTC().toLocaleString() : '{Due Date}';
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
                return data != null ? data.fullName : '{Owner}';
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
                }).toUTC().toLocaleString() : '{Decision Date}';
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
                return data ?? '{ProjectSummary}';
            },
            index: 38
        }
    }

    function getPercentageTotalProjectBudgetColumn() {
        return {
            title: '% of Total Project Budget',
            name: 'percentageTotalProjectBudget',
            data: 'percentageTotalProjectBudget',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 39
        }
    }

    function getOrganizationTypeColumn() {
        return {
            title: 'Organization Type',
            name: 'organizationType',
            data: 'organizationType',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '';
            },
            index: 40
        }
    }

    function getOrganizationNameColumn() {
        return {
            title: 'Organization Name',
            name: 'organizationName',
            data: 'organizationName',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '{OrgName}';
            },
            index: 41
        }
    }
    function getDueDiligenceStatusColumn() {
        return {
            title: 'Due Diligence Status',
            name: 'dueDiligenceStatus',
            data: 'dueDiligenceStatus',
            className: 'data-table-header',
            render: function (data) {
                return titleCase(data ?? '') ?? '{DueDiligenceStatus}';
            },
            index: 42
        }
    }

    function getDeclineRationaleColumn() {
        return { 
            title: 'Decline Rationale',
            name: 'declineRationale',
            data: 'declineRational',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '{DeclineRationale}';
            },
            index: 43
        }
    }

    function getContactFullNameColumn() {
        return {
            title: 'Contact Full Name',
            name: 'contactFullName',
            data: 'contactFullName',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '{ContactFullName}';
            },
            index: 44
        }
    }
    function getContactTitleColumn() {
        return {
            title: 'Contact Title',
            name: 'contactTitle',
            data: 'contactTitle',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '{ContactTitle}';
            },
            index: 45
        }
    }
    function getContactEmailColumn() {
        return {
            title: 'Contact Email',
            name: 'contactEmail',
            data: 'contactEmail',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '{ContactEmail}';
            },
            index: 46
        }
    }
    function getContactBusinessPhoneColumn() {
        return {
            title: 'Contact Business Phone',
            name: 'contactBusinessPhone',
            data: 'contactBusinessPhone',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '{ContactBusinessPhone}';
            },
            index: 47
        }
    }
    function getContactCellPhoneColumn() {
        return {
            title: 'Contact Cell Phone',
            name: 'contactCellPhone',
            data: 'contactCellPhone',
            className: 'data-table-header',
            render: function (data) {
                return data ?? '{ContactCellPhone}';
            },
            index: 48
        }
    }
    window.addEventListener('resize', setTableHeighDynamic);
    function setTableHeighDynamic() {
        let tableHeight = $("#GrantApplicationsTable")[0].clientHeight;
        let docHeight = document.body.clientHeight;
        let tableOffset = 425;

        if ((tableHeight + tableOffset) > docHeight) {
            $("#GrantApplicationsTable_wrapper .dataTables_scrollBody").css({ height: docHeight - tableOffset });
        } else {
            $("#GrantApplicationsTable_wrapper .dataTables_scrollBody").css({ height: tableHeight + 10 });
        }
    }
    dataTable.on('column-reorder.dt', function (e, settings) {
        updateFilter();
    });
    dataTable.on('column-visibility.dt', function (e, settings, deselectedcolumn, state) {
        updateFilter();
    });

    function updateFilter() {
        let optionsOpen = false;
        $("#tr-filter").each(function () {
            if ($(this).is(":visible"))
                optionsOpen = true;
        })
        $('.tr-toggle-filter').remove();
        let newRow = $("<tr class='tr-toggle-filter' id='tr-filter'>");

        dataTable
            .columns()
            .every(function () {
                let column = this;
                if (column.visible()) {
                    let title = column.header().textContent;
                    if (title) {
                        let newCell = $("<td>").append("<input type='text' class='form-control input-sm custom-filter-input' placeholder='" + title + "'>");
                        newCell.find("input").on("keyup", function () {
                            if (column.search() !== this.value) {
                                column.search(this.value).draw();
                            }
                        });

                        newRow.append(newCell);

                    }
                    else {
                        let newCell = $("<td>");
                        newRow.append(newCell);
                    }
                }


            });
        $("#GrantApplicationsTable thead").after(newRow);
        if (optionsOpen) {
            $(".tr-toggle-filter").show();
        }
    }

    PubSub.subscribe(
        'refresh_application_list',
        (msg, data) => {
            dataTable.ajax.reload(null, false);
            PubSub.publish('clear_selected_application');
        }
    );

    function getNames(data) {
        let name = '';
        data.forEach((d, index) => {
            name = name + ' ' + d.fullName;

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
});
