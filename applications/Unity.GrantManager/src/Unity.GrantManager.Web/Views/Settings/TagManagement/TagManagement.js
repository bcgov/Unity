$(function () {
    abp.log.debug('TagManagement.js initialized!');

    let service = unity.grantManager.grantApplications.applicationTags;

    let dataTable = $("#ApplicationTagsTable").DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true,
        serverSide: true,
        paging: false,
        searching: false,
        scrollCollapse: true,
        scrollX: true,
        ordering: false,
        ajax: abp.libs.datatables.createAjax(service.getApplicationTagCounts),
        columnDefs: [
            {
                title: "Tags",
                name: 'text',
                data: 'text'
            },
            {
                title: "Application Count",
                name: 'count',
                data: 'count',
                render: function (data) {
                    let $cellWrapper = $('<div>').addClass('d-flex align-items-center');
                    let $textWrapper = $('<div>').addClass('w-100').append(data ?? '-');
                    let $buttonWrapper = $('<div>').addClass('d-flex flex-nowrap gap-1 flex-shrink-1');

                    let $editButton = $('<button>')
                        .addClass('btn btn-sm edit-button px-0 float-end')
                        .attr({
                            'aria-label': 'Edit',
                            'title': 'Edit'
                        }).append($('<i>').addClass('fl fl-edit'));

                    let $deleteButton = $('<button>')
                        .addClass('btn btn-sm edit-button px-0 float-end')
                        .attr({
                            'aria-label': 'Delete',
                            'title': 'Delete'
                        }).append($('<i>').addClass('fl fl-delete'));

                    $cellWrapper.append($textWrapper);
                    $buttonWrapper.append($editButton);
                    $buttonWrapper.append($deleteButton);
                    $cellWrapper.append($buttonWrapper);

                    return $cellWrapper.prop('outerHTML');
                }
            }
        ]
    }));
});