$(function () {
    const formatter = new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    });

    let searchBar = document.getElementById('search-bar');
    let btnFilter = document.getElementById('btn-filter');

    btnFilter.addEventListener('click', function () {
        document.getElementById('dtFilterRow').classList.toggle('hidden');
    });

    if (searchBar + "" != "undefined") {
        $(searchBar).on('keyup', function (event) {
            const filterValue = event.currentTarget.value;
            const oTable = $('#GrantApplicationsTable').dataTable();
            oTable.fnFilter(filterValue);
            if (filterValue.length > 0) {
                $('#externalLink').prop('disabled', true);
                Array.from(document.getElementsByClassName("selected")).forEach(
                    function (element, index, array) {
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
            dom: 'Bfrtip',
            buttons: [
                {
                    extend: 'csv',
                    text: 'Export',
                    className: 'btn btn-light csv-download',
                    exportOptions: {
                        columns: [10, 15, 11, 9, 1, 12, 13, 5, 7, 4, 14, 16],
                        orthogonal: 'fullName',
                    }
                }

            ],
            drawCallback: function () {
                const $api = this.api();
                const pages = $api.page.info().pages;
                const rows = $api.data().length;

                // Tailor the settings based on the row count
                if (rows <= maxRowsPerPage) {
                    $('.dataTables_info').css('display', 'none')
                    $('.dataTables_paginate').css('display', 'none');

                    $('.dataTables_filter').css('display', 'none')
                    $('.dataTables_length').css('display', 'none')
                } else if (pages === 1) {
                    // With this current length setting, not more than 1 page, hide pagination
                    $('.dataTables_info').css('display', 'none')
                    $('.dataTables_paginate').css('display', 'none');
                } else {
                    // SHow everything
                    $('.dataTables_info').css('display', 'block')
                    $('.dataTables_paginate').css('display', 'block');
                }
            },
            initComplete: function () {
                const api = this.api();
                addFilterRow(api);
            },
            columnDefs: [
                { //0
                    title: '',
                    className: 'select-checkbox',
                    render: function (data) {
                        return '';
                    },
                },
                { //1
                    title: l('ProjectName'),
                    data: 'projectName',
                    name: 'projectName',
                    className: 'data-table-header',
                },
                { //2
                    title: l('ReferenceNo'),
                    data: 'referenceNo',
                    name: 'referenceNo',
                    className: 'data-table-header',
                },
                { //3
                    title: l('EligibleAmount'),
                    data: 'eligibleAmount',
                    name: 'eligibleAmount',
                    className: 'data-table-header',
                    render: function (data) {
                        return formatter.format(data);
                    },
                },
                { //4
                    title: l('RequestedAmount'),
                    data: 'requestedAmount',
                    name: 'requestedAmount',
                    className: 'data-table-header',
                    render: function (data) {
                        return formatter.format(data);
                    },
                },
                { //5
                    title: l('Assignee'),
                    data: 'assignees',
                    name: 'assignees',
                    className: 'data-table-header',
                    render: function (data, type, row) {

                        if (data != null && data.length == 1) {
                            return type === 'fullName' ? getNames(data) : data[0].assigneeDisplayName;
                        } else if (data && data.length > 1) {

                            return type === 'fullName' ? getNames(data) : l('Multiple assignees')
                        }
                        else {
                            return '';
                        }

                    },
                },
                { //6
                    title: l('Probability'),
                    data: 'probability',
                    name: 'probability',
                    className: 'data-table-header',
                    render: function (data) {
                        let disaplayText = ' ';
                        if (data != null && data.length == 1) {
                            disaplayText = data[0].assigneeDisplayName;
                        }
                        return disaplayText;
                    },
                },
                { //7
                    title: l('GrantApplicationStatus'),
                    data: "status",
                    name: "status",
                    className: 'data-table-header',
                    render: function (data) {
                        let disaplayText = ' ';
                        if (data != null && data.length >= 0) {
                            disaplayText = data;
                        }
                        return disaplayText;
                    },
                },
                { //8
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
                { //9
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
                { //10
                    title: 'Applicant Name',
                    name: 'applicantName',
                    className: 'data-table-header',
                    visible: false,
                    render: function (data) {
                        return '';
                    },
                },
                { //11
                    title: 'Category',
                    name: 'category',
                    className: 'data-table-header',
                    visible: false,
                    render: function (data) {
                        return '';
                    },
                },
                { //12
                    title: 'Sector',
                    name: 'sector',
                    className: 'data-table-header',
                    visible: false,
                    render: function (data) {
                        return '';
                    },
                },
                { //13
                    title: 'Total Project Budget',
                    name: 'totalProjectBudget',
                    className: 'data-table-header',
                    visible: false,
                    render: function (data) {
                        return '';
                    },
                },
                { //14
                    title: 'Final Decision Date',
                    name: 'finalDecisionDate',
                    className: 'data-table-header',
                    visible: false,
                    render: function (data) {
                        return '';
                    },
                },
                { //15
                    title: 'Application #',
                    name: 'uniqueIdentifier',
                    data: 'referenceNo',
                    className: 'data-table-header',
                    visible: false,
                },
                { //16
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

    function addFilterRow(api) {
        const trNode = document.createElement('tr');
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

        const children = [...document.getElementById('GrantApplicationsTable').children[0].children[0].children];
        children.forEach(function (child) {
            let label = child.attributes['aria-label'].value;
            child.classList.remove('select-checkbox');
            child.classList.remove('sorting');
            child.classList.remove('sorting_asc');

            const firstElement = label.split(':').shift();

            if (firstElement != "") {
                let inputFilter = document.createElement('input');
                inputFilter.type = "text";
                inputFilter.placeholder = firstElement;
                inputFilter.addEventListener('keyup', function () {
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
            const selectedData = dataTable.row(indexes).data();
            console.log('Selected Data:', selectedData);
            PubSub.publish('select_application', selectedData);
        }
    });

    dataTable.on('deselect', function (e, dt, type, indexes) {
        if (type === 'row') {
            const deselectedData = dataTable.row(indexes).data();
            PubSub.publish('deselect_application', deselectedData);
        }
    });
    dataTable.on('click', 'tbody tr', function (e) {
        e.currentTarget.classList.toggle('selected');
    });
    dataTable
        .buttons()
        .container()
        .prependTo('#dynamicButtonContainerId');

    $('.csv-download').removeClass('dt-button buttons-csv buttons-html5');
    $('.csv-download').prepend('<i class="fl fl-export"></i>');


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
