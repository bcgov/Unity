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

    const UIElements = {
        searchBar: $('#search-bar'),
        btnFilter: $('#btn-sort'),
        btnSave: $('#btn-save'),
        userDiv: $('#users-div'),
        users: $('#users'),
        closeSort: $('#close-sort')
    };

    init();
    
    function init() {
        $('#users').select2();
        bindUIEvents();
        dataTable = initializeDataTable();
        dataTable.buttons().container().prependTo('#dynamicButtonContainerId');
        $('.csv-download').removeClass('dt-button buttons-csv buttons-html5');
        $('.csv-download').prepend('<i class="fl fl-export"></i>');
    }

    function bindUIEvents() {
        UIElements.btnFilter.on('click', toggleFilterRow);
        UIElements.closeSort.on('click', toggleFilterRow);
        UIElements.btnSave.on('click', handleSave);
        UIElements.userDiv.on('change', markUserDivAsChanged);
        UIElements.userDiv.on('blur', checkUserDivChanged);
        UIElements.users.on('blur', checkUserDivChanged);
        UIElements.searchBar.on('keyup', function(e) {
            handleSearch(e);
        });
    }

    dataTable.on('select', function(e, dt, type, indexes) {
        selectApplication(type, indexes, 'select_application');
    });

    dataTable.on('deselect', function(e, dt, type, indexes) {
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

    function markUserDivAsChanged() {
        userDivChanged = true;
        $('#btn-save').attr('disabled', false);
    }

    function handleSave() {
        changeCellContent(currentCell);
        markUserDivAsUnchanged();
        modifyAssignmentsOnServer();
    }

    function handleSearch(e) {
        let filterValue = e.currentTarget.value;
        let oTable = $('#GrantApplicationsTable').dataTable();
        oTable.fnFilter(filterValue);
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
                    assigneeDisplayName: userOption.text,
                    oidcSub: userOption.value,
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
            if($(userOption).prop('selected')) {
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
                
                if (e.target.children.length == 0) {

                    let currentContent = e.target.textContent;
                    e.target.textContent = '';
                    currentRow = e.target.parentElement._DT_RowIndex;
                    let assigness = dataTable.row(e.target.parentElement).context[0]
                        .aoData[currentRow]._aData.assignees;
                    let assigneeIds = [];
    
                    $(assigness).each(function (key, assignee) {
                        assigneeIds.push(assignee.oidcSub);
                    });

                    previousUserOptionsSelected = getUserOptionSelectedCount();

                    if(originalContent != "" 
                        && previousCell+"" != "undefined"
                        && previousCell.textContent == ""
                        && previousUserOptionsSelected > 0
                        && currentRow != previousRow
                    )  {
                        previousCell.textContent = originalContent;
                    } 
                    
                    for (let userOption of userOptions) {
                        $(userOption).prop(
                            'selected',
                            assigneeIds.includes(userOption.value)
                        );
                    }

                    if(originalContent != " " 
                        && previousCell+"" != "undefined"
                        && currentUserOptionsSelected+"" != "undefined"
                        && previousUserOptionsSelected == currentUserOptionsSelected
                        && currentRow != previousRow
                    )  {
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
            dom: 'Bfrtip',
            buttons: [
                {
                    extend: 'csv',
                    text: 'Export',
                    className: 'btn btn-light csv-download',
                    exportOptions: {
                        columns: [1, 2, 3, 4, 5, 7, 8, 9, 10, 11],
                        orthogonal: 'fullName',
                    }
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

                    $('.dataTables_filter').css('display', 'none');
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
            },
            initComplete: function () {
                let api = this.api();
                addFilterRow(api);
                api.columns.adjust();
            },
            columnDefs: [
                { //0
                    title: '',
                    className: 'select-checkbox',
                    orderable: false,
                    render: function (data) {
                        return '';
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
                    data: 'projectName',
                    name: 'projectName',
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
                    title: 'Sector',
                    name: 'sector',
                    className: 'data-table-header',
                    visible: false,
                    render: function (data) {
                        return '';
                    },
                },
                { //6
                    title: 'Total Project Budget',
                    name: 'totalProjectBudget',
                    className: 'data-table-header',
                    visible: false,
                    render: function (data) {
                        return '';
                    },
                },
                { //7
                    title: l('Assignee'),
                    data: 'assignees',
                    name: 'assignees',
                    className: 'dt-editable',
                    createdCell: createdCell,
                    render: function (data, type, row) {
                        let disaplayText = ' ';

                        if (data != null && data.length == 1) {
                            disaplayText = type === 'fullName' ? getNames(data) : data[0].assigneeDisplayName;
                        } else if (data.length > 1) {
                            disaplayText = type === 'fullName' ? getNames(data) : l('Multiple assignees')
                        }

                        return disaplayText;
                    },
                },
                { //8
                    title: l('GrantApplicationStatus'),
                    data: 'status',
                    name: 'status',
                    className: 'data-table-header',
                    render: function (data) {
                        let disaplayText = ' ';
                        if (data != null && data.length >= 0) {
                            disaplayText = data;
                        }
                        return disaplayText;
                    },
                },
                { //9
                    title: l('RequestedAmount'),
                    data: 'requestedAmount',
                    name: 'requestedAmount',
                    className: 'data-table-header',
                    render: function (data) {
                        return formatter.format(data);
                    },
                },
                { //10
                    title: 'Final Decision Date',
                    name: 'finalDecisionDate',
                    className: 'data-table-header',
                    visible: false,
                    render: function (data) {
                        return '';
                    },
                },
                { //11
                    title: 'Approved Amount',
                    name: 'approved Amount',
                    data: 'eligibleAmount',
                    className: 'data-table-header',
                    visible: false,
                    render: function (data) {
                        return formatter.format(data);
                    },
                },
            ],
        })
       );
    }

    function addFilterRow(api) {
        let trNode = document.createElement('tr');
        trNode.classList.add('filter');
        trNode.classList.add('hidden');
        trNode.id = 'dtFilterRow';

        let mapTitles = new Map();
        api.columns().every(function () {
            let column = this;
            let title = $(column.header()).text();
            let index = column.selector.cols;
            mapTitles.set(title, index);
        });

        let children = [
            ...document.getElementById('GrantApplicationsTable').children[0]
                .children[0].children,
        ];

        children.forEach(function (child) {
            let label = child.attributes['aria-label'].value;
            child.classList.remove('select-checkbox');
            child.classList.remove('sorting');
            child.classList.remove('sorting_asc');
            child.classList.add('grey-background');

            const firstElement = label.split(':').shift();

            if (firstElement != '') {
                let inputFilter = document.createElement('input');
                inputFilter.type = 'text';
                inputFilter.classList.add('filter-input');
                inputFilter.placeholder = firstElement;
                inputFilter.addEventListener('keyup', function () {
                    dataTable
                        .columns(mapTitles.get(this.placeholder))
                        .search(this.value)
                        .draw();
                });
                child.appendChild(inputFilter);
            } else {
                child.classList.add('close-icon');
                child.addEventListener('click', toggleFilterRow);
            }
            trNode.appendChild(child);
        });

        document
            .getElementsByClassName('table')[0]
            .children[0].appendChild(trNode);
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
            name = name + ' ' + d.assigneeDisplayName;

            if (index != (data.length - 1)) {
                name = name + ',';
            }
        });

        return name;
    }
});
