$(function () {
    const l = abp.localization.getResource('GrantManager');
    let createModal = new abp.ModalManager(abp.appPath + 'Intakes/CreateModal');
    let updateModal = new abp.ModalManager(abp.appPath + 'Intakes/UpdateModal');

    /**
     * Intakes: List All
     */
    let dataTable = $('#IntakesTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: true,
            order: [[1, "asc"]],
            searching: false,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(unity.grantManager.intake.intake.getList),
            columnDefs: [
                {
                    title: l('Intake'),
                    data: "intakeName"
                },
                {
                    title: l('Common:StartDate'),
                    data: "startDate",
                    render: (data) => luxon
                        .DateTime
                        .fromISO(data, {
                            locale: abp.localization.currentCulture.name
                        }).toLocaleString(luxon.DateTime.DATETIME_SHORT)
                },
                {
                    title: l('Common:EndDate'),
                    data: "endDate",
                    render: (data) => luxon
                        .DateTime
                        .fromISO(data, {
                            locale: abp.localization.currentCulture.name
                        }).toLocaleString(luxon.DateTime.DATETIME_SHORT)
                },
                {
                    title: l("Budget"),
                    data: "budget"
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

    $('#CreateIntakeButton').click(function (e) {
        e.preventDefault();
        createModal.open();
    });
});
