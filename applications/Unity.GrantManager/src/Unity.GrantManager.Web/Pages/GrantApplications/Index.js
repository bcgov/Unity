$(function () {
    const formatter = createNumberFormatter();    
    const l = abp.localization.getResource('GrantManager');
    const maxRowsPerPage = 15;
    // const createdCell = getCreatedCell();
    let dt = $('#GrantApplicationsTable');    
    let dataTable, currentRow, previousRow, currentCell, previousCell, originalContent, previousUserOptionsSelected, currentUserOptionsSelected;
    /* let mapTitles = new Map(); = not used */
    /* commented out clear filter functionality - needs to be looked at again or deleted */

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
                         columns: [1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36],
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
                columns: [
                    { //0
                        title: '<span class="btn btn-secondary btn-light fl fl-filter" title="Toggle Filter" id="btn-toggle-filter"></span>',
                        orderable: false,
                        className: 'notexport',
                        render: function (data) {
                            return '<div class="select-checkbox" title="Select Application" ></div>';
                        },
                    },
                    { //1
                        title: 'Applicant Name',
                        data: 'applicant.applicantName',
                        name: 'applicant.applicantName',
                        className: 'data-table-header',
                    },
                    { //2
                        title: 'Application #',
                        data: 'referenceNo',
                        name: 'referenceNo',
                        className: 'data-table-header',
                    },
                    { //3
                        title: 'Category',
                        data: 'category',
                        name: 'category',
                        className: 'data-table-header',
                    },
                    { //4
                        title: l('SubmissionDate'),
                        data: 'submissionDate',
                        name: 'submissionDate',
                        className: 'data-table-header',
                        render: function (data) {
                            return luxon.DateTime.fromISO(data, {
                                locale: abp.localization.currentCulture.name,
                            }).toLocaleString();
                        },
                    },
                    { //5
                        title: 'Project Name',
                        data: 'projectName',
                        name: 'projectName',
                        className: 'data-table-header',
                    },
                    { //6
                        title: 'Sector',
                        name: 'applicant.sector',
                        data: 'applicant.sector',
                        className: 'data-table-header',
                        render: function (data) {
                            return data ?? '{Sector}';
                        },
                    },
                    { //7
                        title: 'Total Project Budget',
                        name: 'totalProjectBudget',
                        data: 'totalProjectBudget',
                        className: 'data-table-header',
                        render: function (data) {
                            return formatter.format(data);
                        },
                    },
                    { //8
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
                    },
                    { //9
                        title: l('Assignee'),
                        data: 'assignees',
                        name: 'assignees-hidden',
                        visible: false,
                        render: function (data, type, row) {
                            let displayText = ' ';

                            if (data != null) {
                                displayText = getNames(data);
                            }
                            return displayText;
                        },
                    },
                    { //10
                        title: l('GrantApplicationStatus'),
                        data: 'status',
                        name: 'status',
                        className: 'data-table-header',
                        render: function (data, type, row) {
                            let fill = row.assessmentReviewCount > 0 ? 'fas' : 'far';
                            return `<span class="d-flex align-items-center"><i class="${fill} fa-bookmark text-primary"></i><span class="ps-2 flex-fill">${row.status}</span></span>`;
                        },
                    },
                    { //11
                        title: l('RequestedAmount'),
                        data: 'requestedAmount',
                        name: 'requestedAmount',
                        className: 'data-table-header',
                        render: function (data) {
                            return formatter.format(data);
                        },
                    },
                    { //12
                        title: 'Approved Amount',
                        name: 'approved Amount',
                        data: 'approvedAmount',
                        className: 'data-table-header',
                        render: function (data) {
                            return formatter.format(data);
                        },
                    },
                    { //13
                        title: 'Economic Region',
                        name: 'economic Region',
                        data: 'economicRegion',
                        className: 'data-table-header',
                        render: function (data) {
                            return data ?? '{Region}';
                        },
                    },
                    { //14
                        title: 'Regional District',
                        name: 'regional District',
                        data: 'regionalDistrict',
                        className: 'data-table-header',
                        render: function (data) {
                            return data ?? '{Regional District}';
                        },
                    },
                    { //15
                        title: 'Community',
                        name: 'community',
                        data: 'community',
                        className: 'data-table-header',
                        render: function (data) {
                            return data ?? '{Community}';
                        },
                    },
                    { //16
                        title: 'Organization Number',
                        name: 'organizationNumber',
                        data: 'organizationNumber',
                        className: 'data-table-header',
                        visible: false,
                        render: function (data) {
                            return data ?? '{Organization Number}';
                        },
                    },
                    { //17
                        title: 'Org Book Status',
                        name: 'orgBookStatus',
                        data: 'orgBookStatus',
                        className: 'data-table-header',
                        render: function (data) {
                            return data ?? '{Org Book Status}';
                        },
                    },
                    { //18 -- mapped
                        title: 'Project Start Date',
                        name: 'projectStartDate',
                        data: 'projectStartDate',
                        className: 'data-table-header',
                        render: function (data) {
                            return data != null ? luxon.DateTime.fromISO(data, {
                                locale: abp.localization.currentCulture.name,
                            }).toUTC().toLocaleString() : '{Project Start Date}' ;
                        },
                    },
                    { //19 -- mapped
                        title: 'Project End Date',
                        name: 'projectEndDate',
                        data: 'projectEndDate',
                        className: 'data-table-header',
                        render: function (data) {
                            return data != null ? luxon.DateTime.fromISO(data, {
                                locale: abp.localization.currentCulture.name,
                            }).toUTC().toLocaleString() : '{Project End Date}';
                        },
                    },
                    { //20  -- mapped
                        title: 'Projected Funding Total',
                        name: 'projectFundingTotal',
                        data: 'projectFundingTotal',
                        className: 'data-table-header',
                        render: function (data) {
                            return formatter.format(data) ?? '{Projected Funding Total}';
                        },
                    },
                    { //21  -- mapped
                        title: '% of Total Project Budget',
                        name: 'percentageTotalProjectBudget',
                        data: 'percentageTotalProjectBudget',
                        className: 'data-table-header',
                        render: function (data) {
                            return data ?? '{% of Total Project Budget}';
                        },
                    },
                    { //22
                        title: 'Total Paid Amount $',
                        name: 'projectFundingTotal',
                        data: 'projectFundingTotal',
                        className: 'data-table-header',
                        render: function (data) {
                            return  formatter.format(data) ?? '{Total Paid Amount $}';
                        },
                    },
                    { //23
                        title: 'Electoral District',
                        name: 'electoralDistrict',
                        data: 'electoralDistrict',
                        className: 'data-table-header',
                        render: function (data) {
                            return data ?? '{Electoral District}';
                        },
                    },
                    { //24 -- mapped
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
                    },
                    { //25 -- mapped
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
                                }
                            }
                            else {
                                return '{Forestry Focus}';
                            }
                         
                        },
                    },
                    { //26 -- mapped
                        title: 'Acquisition',
                        name: 'acquisition',
                        data: 'acquisition',
                        className: 'data-table-header',
                        render: function (data) {

                            if (data) {
                                return titleCase(data);
                            }
                            else {
                                return  '{Acquisition}';
                            }
                          
                        },
                    },
                    { //27-- mapped
                        title: 'City',
                        name: 'city',
                        data: 'city',
                        className: 'data-table-header',
                        render: function (data) {
                            return data ?? '{city}';
                        },
                    },
                    { //28 -- mapped
                        title: 'Community Population',
                        name: 'communityPopulation',
                        data: 'communityPopulation',
                        className: 'data-table-header',
                        render: function (data) {
                            return data ?? '{Community Population}';
                        },
                    },
                    { //29 -- mapped
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
                    },
                    { //30 -- mapped
                        title: 'Recommendation',
                        name: 'recommendation',
                        data: 'recommendation',
                        className: 'data-table-header',
                        render: function (data) {
                            if (data) {
                                if (data == 'APPROVE') {
                                    return 'Recommended for Approval'
                                }
                                else if (data == 'DENY') {
                                    return 'Recommended for Denial'
                                }
                            }
                            else {
                                return '{Recommendation}';
                            }
                        },
                    },
                    { //31
                        title: 'Tags',
                        name: 'applicationTag',
                        data: 'applicationTag',
                        className: '',
                        render: function (data) {
                            return data.replace(/,/g, ', ') ?? '{Tags}';
                        },
                    },
                    { //32 -- mapped
                        title: 'Total Score',
                        name: 'totalScore',
                        data: 'totalScore',
                        className: 'data-table-header',
                        render: function (data) {
                            return data ?? '{Total Score}';
                        },
                        },
                    { //33 -- mapped
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
                     },
                    { //34 -- mapped
                        title: 'Recommended Amount',
                        name: 'recommendedAmount',
                        data: 'recommendedAmount',
                        className: 'data-table-header',
                        render: function (data) {
                            return formatter.format(data) ?? '{Recommended Amount}';
                        },
                    },
                    { //35 -- mapped
                        title: 'Due Date',
                        name: 'dueDate',
                        data: 'dueDate',
                        className: 'data-table-header',
                        render: function (data) {
                            return data != null ? luxon.DateTime.fromISO(data, {
                                locale: abp.localization.currentCulture.name,
                            }).toUTC().toLocaleString() : '{Due Date}';
                        },
                    },
                    { //36 -- mapped
                        title: 'Owner',
                        name: 'Owner',
                        data: 'owner',
                        className: 'data-table-header',
                        render: function (data) {
                            return data != null ? data.fullName : '{Owner}';
                        },
                    },
                ],

                columnDefs: [
                    {
                        targets: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15], // Index of columns to be visible by default
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
            dataTable.ajax.reload();
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
