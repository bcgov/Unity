$(function () {
    const l = abp.localization.getResource('GrantManager');
    let createModal = new abp.ModalManager(abp.appPath + 'ApplicationForms/CreateModal');
    let updateModal = new abp.ModalManager(abp.appPath + 'ApplicationForms/UpdateModal');

    /**
     * Intakes: List All
     */
    let dataTable = $('#ApplicationFormsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: true,
            order: [[1, "asc"]],
            searching: false,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(unity.grantManager.applicationForms.applicationForm.getList),
            columnDefs: [
                {
                    title: l('Common:Name'),
                    data: "applicationFormName"
                },
                {
                    title: l('Common:Description'),
                    data: "applicationFormDescription",
                },
                {
                    title: l('ApplicationForms:ChefsFormId'),
                    data: "chefsApplicationFormGuid",
                },
                {
                    title: l("ApplicationForms:ChefsCriteriaFormId"),
                    data: "chefsCriteriaFormGuid"
                },
                {
                    title: l('Common:Command:Edit'),
                    rowAction: {
                        items:
                            [
                                {
                                    text: l('Common:Command:Edit'),
                                    action: (data) => updateModal.open({ id: data.record.id })
                                }
                            ]
                    }
                }
            ]
        })
    );

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
});
