$(function () {
    const l = abp.localization.getResource('GrantManager');

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
                    title: 'Program Name',
                    data: "programName"
                },
                {
                    title: 'Sector',
                    data: "type",
                    render: function (data) {
                        return l('Enum:GrantProgramType.' + data);
                    }
                },
                {
                    title: 'Published',
                    data: "publishDate",
                    render: function (data) {
                        return luxon
                            .DateTime
                            .fromISO(data, {
                                locale: abp.localization.currentCulture.name
                            }).toLocaleString();
                    }
                },
                {
                    title: 'Created',
                    data: "creationTime",
                    render: function (data) {
                        return luxon
                            .DateTime
                            .fromISO(data, {
                                locale: abp.localization.currentCulture.name
                            }).toLocaleString(luxon.DateTime.DATETIME_SHORT);
                    }
                }
            ]
        })
    );
});
