$(function () {
    const l = abp.localization.getResource('GrantManager');
    let createModal = new abp.ModalManager(abp.appPath + 'GrantPrograms/CreateModal');
    let updateModal = new abp.ModalManager(abp.appPath + 'GrantPrograms/UpdateModal');

    /**
     * Grant Programs: List All
     */
    let dataTable = $('#GrantProgramsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            paging: true,
            order: [[1, "asc"]],
            searching: false,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(unity.grantManager.grantPrograms.grantProgram.getList),
            columnDefs: [
                {
                    title: l('ProgramName'),
                    data: "programName"
                },
                {
                    title: l('GrantProgramType'),
                    data: "type",
                    render: (data) => l('Enum:GrantProgramType.' + data)
                },
                {
                    title: l('PublishDate'),
                    data: "publishDate",
                    render: (data) => luxon
                        .DateTime
                        .fromISO(data, {
                            locale: abp.localization.currentCulture.name
                        }).toLocaleString()
                },
                {
                    title: l('CreateDate'),
                    data: "creationTime",
                    render: (data) => luxon
                        .DateTime
                        .fromISO(data, {
                            locale: abp.localization.currentCulture.name
                        }).toLocaleString(luxon.DateTime.DATETIME_SHORT)
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

    /**
     * Grant Programs: CREATE
     */
    createModal.onResult(function () {
        dataTable.ajax.reload();
    });

    updateModal.onResult(function () {
        dataTable.ajax.reload();
    });

    $('#CreateGrantProgramButton').click(function (e) {
        e.preventDefault();
        createModal.open();
    });

    /**
     * Grant Programs: UPDATE
     */
});
