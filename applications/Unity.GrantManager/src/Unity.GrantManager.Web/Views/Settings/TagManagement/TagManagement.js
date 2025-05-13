$(function () {
    abp.log.debug('TagManagement.js initialized!');

    let userCanUpdate = abp.auth.isGranted('Unity.GrantManager.SettingManagement.Tags.Update');
    let userCanDelete = abp.auth.isGranted('Unity.GrantManager.SettingManagement.Tags.Delete');

    const TagTypes = {};

    function registerTagType(key, name, service) {
        TagTypes[key] = {
            name: name,
            service: service,
            table: null
        };
    }

    registerTagType('APPLICATIONS', 'Application', unity.grantManager.grantApplications.applicationTags);

    if (abp.features.isEnabled('Unity.Payments')) {
        registerTagType('PAYMENTS', 'Payment', unity.payments.paymentTags.paymentTag);
    }

    Object.values(TagTypes).forEach(tagType => {
        tagType.table = createTagTable(tagType);
    });

    function createTagTable(tagType) {
        let tagManagementTable = $(`#${tagType.name}TagsTable`).DataTable(abp.libs.datatables.normalizeConfiguration({
            processing: true,
            serverSide: true,
            paging: false,
            searching: false,
            scrollCollapse: true,
            scrollX: true,
            ordering: true,
            ajax: abp.libs.datatables.createAjax(tagType.service.getTagSummary),
            columnDefs: [
                {
                    title: "Tags",
                    name: 'text',
                    data: 'text'
                },
                {
                    title: "Count",
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
                                'title': 'Edit',
                                'disabled': !userCanUpdate
                            }).append($('<i>').addClass('fl fl-edit'));

                        let $deleteButton = $('<button>')
                            .addClass('btn btn-sm delete-button px-0 float-end')
                            .attr({
                                'aria-label': 'Delete',
                                'title': 'Delete',
                                'disabled': !userCanDelete
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

        tagManagementTable.on('click', 'td button.edit-button', function (event) {
            event.stopPropagation();
            let rowData = tagManagementTable.row(event.target.closest('tr')).data();
            handleUpdateTag(tagType, rowData.text, rowData.count);
        });

        tagManagementTable.on('click', 'td button.delete-button', function (event) {
            event.stopPropagation();
            let rowData = tagManagementTable.row(event.target.closest('tr')).data();
            handleDeleteTag(tagType, rowData.text, rowData.count);
        });

        abp.log.debug(tagType.name + ' Table initialized!');

        return tagManagementTable;
    }

    function handleUpdateTag(tagType, tagText, tagCount) {

    }

    function handleDeleteTag(tagType, tagText, tagCount) {
        abp.message.confirm(`Are you sure you want to delete \n\r"${tagText}" from ${tagCount} ${tagType.name.toLowerCase()}s?`, "Delete Tag?")
            .then(function (confirmed) {
                if (confirmed) {
                    try {
                        debugger;
                        tagType.service.deleteTag(tagText)
                            .done(function (result) {
                                onTagDeleted(tagType, tagText, result);
                            })
                            .fail(onTagDeleteFailure);
                    } catch (error) {
                        onTagDeleteFailure(error);
                    }
                }
            });
    }

    function onTagDeleted(tagType, tagText, result) {
        abp.notify.success(`The tag "${tagText}" has been deleted from ${tagType.name.toLowerCase()}s.`);
        tagType.table.ajax.reload();
    }

    function onTagDeleteFailure(error) {
        abp.notify.error('Tag deletion failed.');
        if (error) {
            console.log(error);
        }
    }

    // Ensure DataTable headers are adjusted on tab switch
    $('a[data-bs-toggle="tab"], button[data-bs-toggle="tab"]').on('shown.bs.tab', function (e) {
        Object.values(TagTypes).forEach(tagType => {
            if (tagType.table) {
                tagType.table.columns.adjust().draw(false);
            }
        });
    });
});