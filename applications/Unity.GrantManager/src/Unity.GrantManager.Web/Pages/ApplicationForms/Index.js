$(function () {
    const l = abp.localization.getResource('GrantManager');
    let createModal = new abp.ModalManager(abp.appPath + 'ApplicationForms/CreateModal');
    let updateModal = new abp.ModalManager(abp.appPath + 'ApplicationForms/UpdateModal');
    let tokenModal = new abp.ModalManager({
        viewUrl: '/ApplicationForms/TokenModal',
        modalClass: 'ManageTokens'
    })
    let _applicationFormsAppService = unity.grantManager.applicationForms.applicationForm;

    /**
     * Application Forms: List All
     */
    let dataTable = $('#ApplicationFormsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            paging: true,
            order: [[1, "asc"]],
            searching: true,
            language: { 
                search: "",
                searchPlaceholder: "Search",
            },
            scrollX: true,
            processing: true,
            ajax: abp.libs.datatables.createAjax(_applicationFormsAppService.getList),
            columnDefs: [
                {
                    title: l('Actions'),
                    rowAction: {
                        items:
                            [
                                {
                                    text: l('Common:Command:Edit'),
                                    action: function (data) {
                                        updateModal.open({ id: data.record.id })
                                    }
                                },
                                {
                                    text: l('ApplicationForms:Mapping'),
                                    action: function (data) {
                                        location.href = '/ApplicationForms/Mapping?ApplicationId=' + data.record.id
                                    }
                                }
                            ]
                    }
                },
                {
                    title: l('Common:Name'),
                    data: "applicationFormName"
                },
                {
                    title: l('Common:Description'),
                    data: "applicationFormDescription",
                },
                {
                    title: l('ApplicationForms:Category'),
                    data: "category",
                },
                {
                    title: l('ApplicationForms:ChefsFormId'),
                    data: "chefsApplicationFormGuid",
                }
            ]
        })
    );

    dataTable.on('search.dt', () => handleSearch());
    $('.dataTables_filter input').attr("placeholder", "Search");
    $('.dataTables_filter label')[0].childNodes[0].remove();

    createModal.onResult(function () {
        dataTable.ajax.reload();
    });

    updateModal.onResult(function () {
        dataTable.ajax.reload();
    });

    $('#CreateApplicationFormButton').click(function (e) {
        e.preventDefault();
        createModal.open();
    });

    $('#SetApiTokenBtn').click(function (e) {
        e.preventDefault();
        tokenModal.open();
    });

    $('#GetApiTokenBtn').click(function (e) {
        e.preventDefault();
        let link = document.createElement("a");
        link.setAttribute('download', '');
        link.href = '/api/app/configurationFile/applicationforms';
        document.body.appendChild(link);
        link.click();
        link.remove();
    });

    function handleSearch() {
        let filter = $('.dataTables_filter input').val();
        console.info(filter);
    }
});


