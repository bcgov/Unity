$(function () {
    const formatter = new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    });

    const searchBar = document.getElementById('search-bar');
    const btnFilter = document.getElementById('btn-filter');
    const btnSave = document.getElementById('btn-save');
    const userDiv = document.getElementById('users-div');
    const l = abp.localization.getResource('GrantManager');
    const maxRowsPerPage = 15;
    let dt = $('#GrantApplicationsTable');
    let userOptions = document.getElementById('users');
    let dataTable, currentRow, currentCell;
    let userDivChanged = false;
    let modifiedAssignments = new Map();

    $('#users').select2();

    function changeCellContent(cell) {
        let i, count = 0;
        let content = "";
        let aData = dataTable.row(cell).context[0].aoData[currentRow]._aData;
        aData.assignees = [];

        for(i = 0; i < userOptions.length; i++) {
            if (userOptions[i].selected) {
                count++;
                content = userOptions[i].text;
                aData.assignees.push({"assigneeDisplayName": userOptions[i].text, "oidcSub": userOptions[i].value});
            }
        }

        if(count === 1) {
            cell.textContent = content;
        }
        
        if (count > 1) {
            cell.textContent = "Multiple assignees";
        } 
        
        if (count === 0) {
            cell.textContent = "";
        }

        modifiedAssignments.set(aData.id, aData.assignees);
    }

    btnFilter.addEventListener('click', function() {
        document.getElementById('dtFilterRow').classList.toggle('hidden');
    });

    btnSave.addEventListener('click', function() {
        changeCellContent(currentCell);
        userDivChanged = false;
        $('#btn-save').attr("disabled", true); 
        modifyAssignments();
    });

    if(searchBar+""!="undefined") {
        $(searchBar).on('keyup', function(event) {
            let filterValue = event.currentTarget.value;
            let oTable = $('#GrantApplicationsTable').dataTable();
            oTable.fnFilter(filterValue);
            if(filterValue.length > 0) {
                selectedApplicationIds = [];
                $('#externalLink').prop('disabled', true);
                Array.from(document.getElementsByClassName("selected")).forEach(
                    function(element, index, array) {
                        element.classList.toggle("selected");
                    }
                );
            }
        });
    }

    const createdCell = function(cell) {
      cell.setAttribute('contenteditable', true);
      cell.addEventListener("focus", function(e) {
            
            if(e.target.children.length == 0) {
                if(userDivChanged) {
                    changeCellContent(currentCell);
                    userDivChanged = false;
                }

                e.target.textContent = "";
                currentRow = e.target.parentElement._DT_RowIndex;
                let assigness = dataTable.row(e.target.parentElement).context[0].aoData[currentRow]._aData.assignees;
                let assigneeIds = [];
    
                $(assigness).each(function( key, assignee ) {
                    assigneeIds.push(assignee.oidcSub);
                });
    
                let userOption, i;
    
                for(i = 0; i < userOptions.length; i++) {
                    userOption = userOptions[i];
                    $(userOption).prop("selected", assigneeIds.includes(userOption.value));
                }

                $(userDiv).appendTo(this);
                $('#users').select2();
                userDiv.classList.remove('hidden');
                $('ul').click();
            }      
            currentCell = this;  
      });
      
      cell.addEventListener("blur", function(e) {

        if(e.relatedTarget != null 
            && e.relatedTarget.classList.value != 'select2-selection select2-selection--multiple' 
            && e.relatedTarget.classList.value != 'select2-search__field'
            ) {
          changeCellContent(e.currentTarget);
        }
      });

    }

    dataTable = dt.DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            paging: true,
            order: [[1, 'asc']],
            searching: true,
            pageLength: maxRowsPerPage,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.grantApplications.grantApplication.getList
            ),
            select: {
              style: 'multiple',
              selector: 'td:not(:nth-child(6))'
            }, 
            drawCallback:function() {
                let $api = this.api();
                let pages = $api.page.info().pages;
                let rows = $api.data().length;
         
                // Tailor the settings based on the row count
                if(rows <= maxRowsPerPage){
                    $('.dataTables_info').css('display','none')
                    $('.dataTables_paginate').css('display','none');
        
                    $('.dataTables_filter').css('display', 'none')
                    $('.dataTables_length').css('display', 'none')
                } else if(pages === 1){
                    // With this current length setting, not more than 1 page, hide pagination
                    $('.dataTables_info').css('display','none')
                    $('.dataTables_paginate').css('display','none');
                } else {
                    // SHow everything
                    $('.dataTables_info').css('display','block')
                    $('.dataTables_paginate').css('display','block');
                }
            },
            initComplete: function () {
                let api = this.api();
                addFilterRow(api);
                api.columns.adjust();
            },
            columnDefs: [
                {
                    title: '',
                    className: 'select-checkbox',
                    render: function (data) {
                        return '';
                    },
                },
                {
                    title: l('ProjectName'),
                    data: 'projectName',
                    name: 'projectName',
                    className: 'data-table-header',
                },
                {
                    title: l('ReferenceNo'),
                    data: 'referenceNo',
                    name: 'referenceNo',
                    className: 'data-table-header',
                },
                {
                    title: l('EligibleAmount'),
                    data: 'eligibleAmount',
                    name: 'eligibleAmount',
                    className: 'data-table-header',
                    render: function (data) {
                        return formatter.format(data);
                    },
                },
                {
                    title: l('RequestedAmount'),
                    data: 'requestedAmount',
                    name: 'requestedAmount',
                    className: 'data-table-header',
                    render: function (data) {
                        return formatter.format(data);
                    },
                },
                {
                    title: l('Assignee'),
                    data: 'assignees',
                    name: 'assignees',
                    className: 'data-table-header',
                    createdCell: createdCell,
                    render: function (data, type, row) {
                        let disaplayText = ' ';
                        if(data != null && data.length == 1) {
                            disaplayText = data[0].assigneeDisplayName;
                        } else if(data.length > 1) {
                            disaplayText = l('Multiple assignees')
                        }
                        return disaplayText;
                    },
                },
                {
                    title: l('Probability'),
                    data: 'probability',
                    name: 'probability',
                    className: 'data-table-header',
                    render: function (data) {
                        let disaplayText = ' ';
                        if(data != null && data.length == 1) {
                            disaplayText = data[0].assigneeDisplayName;
                        }
                        return disaplayText;
                    },
                },
                {
                    title: l('GrantApplicationStatus'),
                    data: "status",
                    name: "status",
                    className: 'data-table-header',
                    render: function (data) {
                        let disaplayText = ' ';
                        if(data != null && data.length >= 0) {
                            disaplayText = data;
                        }
                        return disaplayText;
                    },
                },
                {
                    title: l('ProposalDate'),
                    data: 'proposalDate',
                    name: "proposalDate",
                    className: 'data-table-header',
                    render: function (data) {
                        return luxon.DateTime.fromISO(data, {
                            locale: abp.localization.currentCulture.name,
                        }).toLocaleString();
                    },
                },
                {
                    title: l('SubmissionDate'),
                    data: 'submissionDate',
                    name: "submissionDate",
                    className: 'data-table-header',
                    render: function (data) {
                        return luxon.DateTime.fromISO(data, {
                            locale: abp.localization.currentCulture.name,
                        }).toLocaleString();
                    },
                },
            ],
        })
    );

    $(userDiv).on('change', function(e){
        userDivChanged = true;
        $('#btn-save').attr("disabled", false); 
    });

    $(userDiv).on('blur', function() {
        if(userDivChanged) {
            changeCellContent(currentCell);
            userDivChanged = false;
        }
    })

    $('#users').on('blur', function() {
        if(userDivChanged) {
            changeCellContent(currentCell);
            userDivChanged = false;
        }
    })
    
    function addFilterRow(api) {
        let trNode = document.createElement('tr');
        trNode.classList.add('filter');
        trNode.classList.add('hidden');
        trNode.id = "dtFilterRow";

        let mapTitles = new Map();
        api.columns().every(function () {
            let column = this;
            let title = $(column.header()).text();
            let index = column.selector.cols;
            mapTitles.set(title, index);
        });

        let children = [...document.getElementById('GrantApplicationsTable').children[0].children[0].children];
        children.forEach(function(child) {
            let label = child.attributes['aria-label'].value;
            child.classList.remove('select-checkbox');
            child.classList.remove('sorting');
            child.classList.remove('sorting_asc');
            child.classList.add('grey-background');

            const firstElement = label.split(':').shift();

            if(firstElement != "") {
                let inputFilter = document.createElement('input');
                inputFilter.type = "text";
                inputFilter.classList.add('filter-input');
                inputFilter.placeholder = firstElement;
                inputFilter.addEventListener('keyup', function() {
                    dataTable.columns(mapTitles.get(this.placeholder)).search(this.value).draw();
                });
                child.appendChild(inputFilter);
            }
            trNode.appendChild(child);
        });

        document.getElementsByClassName('table')[0].children[0].appendChild(trNode);
    };

    dataTable.on('select', function (e, dt, type, indexes) {      
        if (type === 'row') {
            let selectedData = dataTable.row(indexes).data();
            PubSub.publish('select_application', selectedData);
        }
    });

    dataTable.on('deselect', function (e, dt, type, indexes) {
        if (type === 'row') {
            let deselectedData = dataTable.row(indexes).data();
            PubSub.publish('deselect_application', deselectedData);
        }
    });

    function modifyAssignments() {
        let id, obj = Object.fromEntries(modifiedAssignments);
        let jsonString = JSON.stringify(obj);

        try {
            unity.grantManager.grantApplications.grantApplication.modifyAssignees(jsonString)
                .done(function () {
                    abp.notify.success(
                        'The application has been updated.'
                    );
                    PubSub.publish('refresh_application_list', id);
                });
        }
        catch (error) {
            console.log(error);
        }
    }

    const refresh_application_list_subscription = PubSub.subscribe(
        'refresh_application_list',
        (msg, data) => {
            dataTable.ajax.reload();
            PubSub.publish('clear_selected_application');
        }
    );
});
