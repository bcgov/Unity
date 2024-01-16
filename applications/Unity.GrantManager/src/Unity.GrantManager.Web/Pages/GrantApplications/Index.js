$(function () {
    const formatter = createNumberFormatter();
    const userDiv = document.getElementById('users-div');
    const l = abp.localization.getResource('GrantManager');
    const maxRowsPerPage = 15;
    const createdCell = getCreatedCell();
    let dt = $('#GrantApplicationsTable');
    let userOptions = document.getElementById('users');
    let dataTable, currentRow, previousRow, currentCell, previousCell, originalContent, previousUserOptionsSelected, currentUserOptionsSelected;
    let userDivChanged = false;
    let modifiedAssignments = new Map();
    let mapTitles = new Map();

    dataTable = initializeDataTable();
    dataTable.buttons().container().prependTo('#dynamicButtonContainerId');
    dataTable.on('search.dt', () => handleSearch());
   
   
    //$('#dynamicButtonContainerId').prepend($('.csv-download:eq(0)'));
    //$('#dynamicButtonContainerId').prepend($('.cln-visible:eq(0)'));
    
    const UIElements = {
        searchBar: $('#search-bar'),
        btnToggleFilter: $('#btn-toggle-filter'),
        filterIcon: $("i.fl.fl-filter"),
        btnSave: $('#btn-save'),
        userDiv: $('#users-div'),
        users: $('#users'),
        clearFilter: $('#btn-clear-filter')
    };
    init();
    function init() {
        $('#users').select2();
        $('.custom-table-btn').removeClass('dt-button buttons-csv buttons-html5');
        $('.csv-download').prepend('<i class="fl fl-export"></i>');
        $('.cln-visible').prepend('<i class="fl fl-settings"></i>');
        bindUIEvents();
        UIElements.clearFilter.html("<span class='x-mark'>X</span>" + UIElements.clearFilter.html());
    }

    function bindUIEvents() {
        UIElements.btnToggleFilter.on('click', toggleFilterRow);
        UIElements.filterIcon.on('click', $('#dtFilterRow').toggleClass('hidden'));
        UIElements.clearFilter.on('click', clearFilter);
        UIElements.btnSave.on('click', handleSave);
        UIElements.userDiv.on('change', markUserDivAsChanged);
        UIElements.userDiv.on('blur', checkUserDivChanged);
        UIElements.users.on('blur', checkUserDivChanged);
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

    function markUserDivAsChanged() {
        userDivChanged = true;
        $('#btn-save').attr('disabled', false);
    }

    function handleSave() {
        changeCellContent(currentCell);
        markUserDivAsUnchanged();
        modifyAssignmentsOnServer();
    }

    function handleSearch() {
        let filterValue = $('.dataTables_filter input').val();
        if (filterValue.length > 0) {
            $('#externalLink').prop('disabled', true);
            Array.from(document.getElementsByClassName('selected')).forEach(
                function (element, index, array) {
                    element.classList.toggle('selected');
                }
            );
        }
    }

    function checkUserDivChanged() {
        if (userDivChanged) {
            changeCellContent(currentCell);
            userDivChanged = false;
        }
    }

    function markUserDivAsUnchanged() {
        userDivChanged = false;
        $('#btn-save').attr('disabled', true);
    }

    function createNumberFormatter() {
        return new Intl.NumberFormat('en-CA', {
            style: 'currency',
            currency: 'CAD',
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
        });
    }

    function changeCellContent(cell) {
        let count = 0;
        let content = '';
        let aData = dataTable.row(cell).context[0].aoData[currentRow]._aData;
        aData.assignees = [];

        for (let userOption of userOptions) {
            if (userOption.selected) {
                count++;
                content = userOption.text;
                aData.assignees.push({
                    fullName: userOption.text,
                    assigneeId: userOption.value,
                });
            }
        }

        if (count === 1) {
            cell.textContent = content;
        } else if (count > 1) {
            cell.textContent = 'Multiple assignees';
        } else if (count === 0) {
            cell.textContent = '';
        }

        modifiedAssignments.set(aData.id, aData.assignees);
    }

    function getUserOptionSelectedCount() {
        let userOptionSelectedCount = 0;
        for (let userOption of userOptions) {
            if ($(userOption).prop('selected')) {
                userOptionSelectedCount++;
            }
        }
        return userOptionSelectedCount;
    }

    function getCreatedCell() {
        return function (cell) {
            cell.setAttribute('contenteditable', true);
            cell.addEventListener('focus', function (e) {
                checkUserDivChanged();
                if (e.target.children.length == 0 ||
                    (e.target.children.length == 1
                    && e.target.children[0].className.includes('dt-select-assignees'))) {
                    let currentContent = e.target.textContent;
                    e.target.textContent = '';
                    currentRow = e.target.parentElement._DT_RowIndex;
                    let assigness = dataTable.row(e.target.parentElement).context[0]
                        .aoData[currentRow]._aData.assignees;
                    let assigneeIds = [];

                    $(assigness).each(function (key, assignee) {
                        assigneeIds.push(assignee.assigneeId);
                    });

                    previousUserOptionsSelected = getUserOptionSelectedCount();

                    if (originalContent != ""
                        && previousCell + "" != "undefined"
                        && previousCell.textContent == ""
                        && previousUserOptionsSelected > 0
                        && currentRow != previousRow
                    ) {
                        previousCell.textContent = originalContent;
                    }

                    for (let userOption of userOptions) {
                        $(userOption).prop(
                            'selected',
                            assigneeIds.includes(userOption.value)
                        );
                    }

                    if (originalContent != " "
                        && previousCell + "" != "undefined"
                        && currentUserOptionsSelected + "" != "undefined"
                        && previousUserOptionsSelected == currentUserOptionsSelected
                        && currentRow != previousRow
                    ) {
                        previousCell.textContent = originalContent;
                    }

                    currentUserOptionsSelected = getUserOptionSelectedCount();

                    $(userDiv).appendTo(this);
                    $('#users').select2();
                    userDiv.classList.remove('hidden');
                    $('ul').click();

                    originalContent = currentContent;
                    previousCell = this;
                    previousRow = currentRow;
                    setTableHeighDynamic();
                }
                currentCell = this;                
            });

            cell.addEventListener('blur', function (e) {
                if (
                    e.relatedTarget != null &&
                    e.relatedTarget.classList.value != 'select2-selection select2-selection--multiple' &&
                    e.relatedTarget.classList.value != 'select2-search__field'
                ) {
                    changeCellContent(e.currentTarget);
                }
            });
        };
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
                         columns: [1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34],
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
                        data: 'applicant',
                        name: 'applicant',
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
                        name: 'sector',
                        data: 'sector',
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
                        createdCell: createdCell,                        
                        render: function (data, type, row) {
                            let displayText = ' ';

                            if (data != null && data.length == 1) {
                                displayText = type === 'fullName' ? getNames(data) : data[0].fullName;
                            } else if (data.length > 1) {
                                displayText = type === 'fullName' ? getNames(data) : l('Multiple assignees')
                            }

                            return `<span class="d-flex align-items-center dt-select-assignees">
                                <i class="fl fl-edit"></i>
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
                    //{ // -- 
                    //    title: 'Final Decision Date',
                    //    name: 'finalDecisionDate',
                    //    className: 'data-table-header',
                    //    visible: false,                    
                    //},
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
                        title: 'City',
                        name: 'city',
                        data: 'city',
                        className: 'data-table-header',
                        render: function (data) {
                            return data ?? '{City}';
                        },
                    },
                              
                    { //15
                        title: 'Organization Number',
                        name: 'organizationNumber',
                        data: 'organizationNumber',
                        className: 'data-table-header',
                        visible: false,
                        render: function (data) {
                            return data ?? '{Organization Number}';
                        },
                    },
                    { //16
                        title: 'Org Book Status',
                        name: 'orgBookStatus',
                        data: 'orgBookStatus',
                        className: 'data-table-header',
                        render: function (data) {
                            return data ?? '{Org Book Status}';
                        },
                    },
                    { //17 -- mapped
                        title: 'Project Start Date',
                        name: 'projectStartDate',
                        data: 'projectStartDate',
                        className: 'data-table-header',
                        render: function (data) {
                            return data != null ? luxon.DateTime.fromISO(data, {
                                locale: abp.localization.currentCulture.name,
                            }).toLocaleString() : '{Project Start Date}' ;
                        },
                    },
                    { //18 -- mapped
                        title: 'Project End Date',
                        name: 'projectEndDate',
                        data: 'projectEndDate',
                        className: 'data-table-header',
                        render: function (data) {
                            return data != null ? luxon.DateTime.fromISO(data, {
                                locale: abp.localization.currentCulture.name,
                            }).toLocaleString() : '{Project End Date}';
                        },
                    },
                    { //19  -- mapped
                        title: 'Projected Funding Total',
                        name: 'projectFundingTotal',
                        data: 'projectFundingTotal',
                        className: 'data-table-header',
                        render: function (data) {
                            return formatter.format(data) ?? '{Projected Funding Total}';
                        },
                    },
                    { //20  -- mapped
                        title: '% of Total Project Budget',
                        name: 'percentageTotalProjectBudget',
                        data: 'percentageTotalProjectBudget',
                        className: 'data-table-header',
                        render: function (data) {
                            return data ?? '{% of Total Project Budget}';
                        },
                    },
                    { //21
                        title: 'Total Paid Amount $',
                        name: 'projectFundingTotal',
                        data: 'projectFundingTotal',
                        className: 'data-table-header',
                        render: function (data) {
                            return  formatter.format(data) ?? '{Total Paid Amount $}';
                        },
                    },
                    { //22
                        title: 'Electoral District',
                        name: 'electoralDistrict',
                        data: 'electoralDistrict',
                        className: 'data-table-header',
                        render: function (data) {
                            return data ?? '{Electoral District}';
                        },
                    },
                    { //23 -- mapped
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
                    { //24 -- mapped
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
                    { //25 -- mapped
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
                    { //26 -- mapped
                        title: 'Community',
                        name: 'community',
                        data: 'community',
                        className: 'data-table-header',
                        render: function (data) {
                            return data ?? '{community}';
                        },
                    },
                    { //27 -- mapped
                        title: 'Community Population',
                        name: 'communityPopulation',
                        data: 'communityPopulation',
                        className: 'data-table-header',
                        render: function (data) {
                            return data ?? '{Community Population}';
                        },
                    },
                    { //28 -- mapped
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
                    { //29 -- mapped
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
                    { //30
                        title: 'Tags',
                        name: 'applicationTag',
                        data: 'applicationTag',
                        className: 'data-table-header',
                        render: function (data) {
                            return data ?? '{Tags}';
                        },
                    },
                    { //31 -- mapped
                        title: 'Total Score',
                        name: 'totalScore',
                        data: 'totalScore',
                        className: 'data-table-header',
                        render: function (data) {
                            return data ?? '{Total Score}';
                        },
                        },
                    { //32 -- mapped
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
                    { //33 -- mapped
                        title: 'Recommended Amount',
                        name: 'recommendedAmount',
                        data: 'recommendedAmount',
                        className: 'data-table-header',
                        render: function (data) {
                            return formatter.format(data) ?? '{Recommended Amount}';
                        },
                    },
                    { //34 -- mapped
                        title: 'Due Date',
                        name: 'dueDate',
                        data: 'dueDate',
                        className: 'data-table-header',
                        render: function (data) {
                            return data != null ? luxon.DateTime.fromISO(data, {
                                locale: abp.localization.currentCulture.name,
                            }).toLocaleString() : '{Due Date}';
                        },
                    },
                ],

                columnDefs: [
                    {
                        targets: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14], // Index of columns to be visible by default
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

  

    function modifyAssignmentsOnServer() {
        let id,
            obj = Object.fromEntries(modifiedAssignments);
        let jsonString = JSON.stringify(obj);

        try {
            unity.grantManager.grantApplications.grantApplication
                .updateAssignees(jsonString)
                .done(function () {
                    abp.notify.success('The application has been updated.');
                    PubSub.publish('refresh_application_list', id);
                });
        } catch (error) {
            console.log(error);
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
