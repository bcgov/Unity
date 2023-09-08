$(function () {
    const formatter = new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    });

    let searchBar = document.getElementById('search-bar');
    let btnFilter = document.getElementById('btn-filter');
    
    btnFilter.addEventListener('click', function() {
        document.getElementById('dtFilterRow').classList.toggle('hidden');
    });

    if(searchBar+""!="undefined") {
        $(searchBar).on('keyup', function(event) {
            var filterValue = event.currentTarget.value;
            var oTable = $('#GrantApplicationsTable').dataTable();
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

    const l = abp.localization.getResource('GrantManager');
    let dt = $('#GrantApplicationsTable');
    let maxRowsPerPage = 40;

    const dataTable = dt.DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            paging: true,
            order: [[1, 'asc']],
            searching: true,
            pageLength: maxRowsPerPage,
            scrollX: true,
            select: 'multi',
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.grantApplications.grantApplication.getList
            ),
            drawCallback:function() {
                var $api = this.api();
                var pages = $api.page.info().pages;
                var rows = $api.data().length;
         
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
                var api = this.api();
                addFilterRow(api);
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
                    render: function (data) {
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
    
    function addFilterRow(api) {
        var trNode = document.createElement('tr');
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

        var children = [...document.getElementById('GrantApplicationsTable').children[0].children[0].children];
        children.forEach(function(child) {
            let label = child.attributes['aria-label'].value;
            child.classList.remove('select-checkbox');
            child.classList.remove('sorting');
            child.classList.remove('sorting_asc');

            const firstElement = label.split(':').shift();

            if(firstElement != "") {
                let inputFilter = document.createElement('input');
                inputFilter.type = "text";
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
            var selectedData = dataTable.row(indexes).data();
            console.log('Selected Data:', selectedData);
            PubSub.publish('select_application', selectedData);
        }
    });

    dataTable.on('deselect', function (e, dt, type, indexes) {
        if (type === 'row') {
            var deselectedData = dataTable.row(indexes).data();
            PubSub.publish('deselect_application', deselectedData);
        }
    });
    dataTable.on('click', 'tbody tr', function (e) {
        e.currentTarget.classList.toggle('selected');
    });

    const refresh_application_list_subscription = PubSub.subscribe(
        'refresh_application_list',
        (msg, data) => {
            dataTable.ajax.reload();
            PubSub.publish('clear_selected_application');
        }
    );
});
