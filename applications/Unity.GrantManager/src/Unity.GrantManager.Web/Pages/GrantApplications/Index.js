$(function () {
    const l = abp.localization.getResource('GrantManager');

    const dt = new DataTable('#grantApplicationsTable', {
        ajax: 'api/app/grant-application',
        processing: true,
        serverSide: true,        
        columns: [
            {
                title: "Name",
                data: "name"
            },
            {
                title: "Last Modified",
                data: "lastModificationTime",
                render: function (data) {
                    return luxon
                        .DateTime
                        .fromISO(data, {
                            locale: abp.localization.currentCulture.name
                        }).toLocaleString();
                }
            },
            {
                title: "Created",
                data: "creationTime",
                render: function (data) {
                    return luxon
                        .DateTime
                        .fromISO(data, {
                            locale: abp.localization.currentCulture.name
                        }).toLocaleString();
                }
            },
        ]
    });
});

