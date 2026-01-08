const TagTypes = {};
let userCanUpdate = abp.auth.isGranted('Unity.GrantManager.SettingManagement.Tags.Update');
let userCanDelete = abp.auth.isGranted('Unity.GrantManager.SettingManagement.Tags.Delete');
let addNewTagModal = new abp.ModalManager({
    viewUrl: 'Tags/CreateTagsModal'
});

function defineTagSummaryColumnDefs() {
    const columnDefs = [
        {
            title: "Tags",
            name: 'tag',
            data: 'tag.name'
        },
        {
            title: "TagData",
            name: 'tag',
            data: 'tag',           
            visible: false,        
            searchable: false,
            orderable: false
        },
        {
            title: "TagId",
            name: 'tag.id',
            data: 'tag.id',
            visible: false,
            searchable: false,
            orderable: false
        }
    ];

    Object.values(TagTypes).forEach(tagType => {
        columnDefs.push({
            title: `${tagType.name} Count`,
            name: `${tagType.name.toLowerCase()}Count`,
            data: `tagTypeCounts.${tagType.name}`,
            defaultContent: 0,
            render: function (data) {
                return data || 0;
            }
        });
    });

    columnDefs.push({
        title: "Count",
        name: 'totalCount',
        data: 'totalCount'
    });

    columnDefs.push({
        title: "Actions",
        name: 'actions',
        data: 'tag.name',
        orderable: false,
        render: function (data, type, row) {
            let $buttonWrapper = $('<div>').addClass('d-flex flex-nowrap gap-1');

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

            $buttonWrapper.append($editButton);
            $buttonWrapper.append($deleteButton);

            return $buttonWrapper.prop('outerHTML');
        }
    });

    return columnDefs;
}

function configureSearch(dataTableInstance) {
    const searchId = "#search-tags";

    $(searchId).on('input change', function () {
        let filter = dataTableInstance.search($(this).val()).draw();
        console.info(`Filter on #${searchId}: ${filter}`);
    });
}

function mapTagWithType(tag, tagType) {
    return {
        ...tag,
        tagType: tagType.name,
        tagTypeKey: Object.keys(TagTypes).find(key => TagTypes[key] === tagType),
        tag: {
            ...tag.tag
        }
    };
}



function loadUnifiedTagSummary() {
    return unity.grantManager.globalTag.tags.getTagSummary().then(result => {
        const allTags = result.items || [];
        const tagMap = new Map();

        allTags.forEach(tag => {
            const tagName = tag.tagName;

            if (!tagMap.has(tagName)) {
                tagMap.set(tagName, {
                    tag: {
                        id: tag.tagId,
                        name: tagName
                    },
                    totalCount: 0,
                    tagTypeCounts: {},
                    tagTypeKeys: {}
                });
            }

            const tagEntry = tagMap.get(tagName);

            if (tag.applicationTagCount > 0) {
                tagEntry.tagTypeCounts["Application"] = tag.applicationTagCount;
                tagEntry.totalCount += tag.applicationTagCount;
                tagEntry.tagTypeKeys["Application"] = "APPLICATIONS";
            }

            if (tag.paymentTagCount > 0) {
                tagEntry.tagTypeCounts["Payment"] = tag.paymentTagCount;
                tagEntry.totalCount += tag.paymentTagCount;
                tagEntry.tagTypeKeys["Payment"] = "PAYMENTS";
            }
        });

        return Array.from(tagMap.values()).map(entry => ({
            ...entry,
            tag: { ...entry.tag }
        }));
    });
}
function getUnifiedTagSummaryAjax(requestData, callback, settings) {
    loadUnifiedTagSummary().then(combinedTags => {
        callback({
            recordsTotal: combinedTags.length,
            recordsFiltered: combinedTags.length,
            data: combinedTags
        });
    }).catch(error => {
        console.error("Error fetching tag summary:", error);
        callback({
            recordsTotal: 0,
            recordsFiltered: 0,
            data: []
        });
    });
}
$(function () {
    abp.log.debug('TagManagement.js initialized!');

    abp.modals.RenameTag = function () {
        let formElements = {};
        let initialFormState = {};

        let initModal = function (modalManager, args) {
            formElements = {
                form: modalManager.getForm(),
                saveButton: $('#SaveButton'),
                originalTagInput: $('input[name="ViewModel.OriginalTag"]'),
                replacementTagInput: $('input[name="ViewModel.ReplacementTag"]')
            };

            initialFormState = formElements.form.serialize();

            formElements.form.on('change input', function () {
                $(this).valid();
                let currentFormState = formElements.form.serialize();
                formElements.saveButton.prop('disabled', !$(this).valid() || initialFormState === currentFormState);
            });

            console.log('initialized the modal...');
        };

        return {
            initModal: initModal
        };
    };

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

    let _renameTagModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'SettingManagement/TagManagement/RenameTagModal',
        modalClass: 'RenameTag',
        registeredTagTypes: TagTypes
    });

    let globalTagsTable = $('#GlobalTagsTable').DataTable(abp.libs.datatables.normalizeConfiguration({
        processing: true,
        serverSide: false,
        paging: false,
        searching: true,
        scrollCollapse: true,
        scrollX: true,
        ordering: true,
        ajax: (requestData, callback, settings) => getUnifiedTagSummaryAjax(requestData, callback, settings),
        columnDefs: defineTagSummaryColumnDefs()
    }));

    configureSearch(globalTagsTable);

    globalTagsTable.on('click', 'td button.edit-button', function (event) {
        event.stopPropagation();
        let rowData = globalTagsTable.row(event.target.closest('tr')).data(); 
        console.log("rowData", rowData)
        handleUpdateTag(rowData.tag.id,rowData.tag.name);
    });

    globalTagsTable.on('click', 'td button.delete-button', function (event) {
        event.stopPropagation();
        let rowData = globalTagsTable.row(event.target.closest('tr')).data();
        handleDeleteTag(rowData.tag.id,rowData.tag.name);
    });

    abp.log.debug('Global Tags Table initialized!');

    function handleUpdateTag(id, tagText) {
        console.log("handleUpdateTag ")
        _renameTagModal.open({
            SelectedTagId : id,
            SelectedTagText: tagText
        });
    }

    function handleDeleteTag(id,tagText) {
        console.log("handleDeleteTag")
        abp.message.confirm(`Are you sure you want to delete this tag? Deleting it will remove the tag from all assigned records.`, "Delete Tag?")
            .then(function (confirmed) {
                if (confirmed) {
                    try {
                        unity.grantManager.globalTag.tags.deleteTagGlobal(id)
                            .done(function (result) {
                                abp.notify.success(`The tag "${tagText}" has been deleted.`);
                                if (globalTagsTable) {
                                    globalTagsTable.ajax.reload();
                                }
                            })
                            .fail(onTagDeleteFailure);
                    } catch (error) {
                        onTagDeleteFailure(error);
                    }
                }
            });
    }

    function onTagDeleteFailure(error) {
        abp.notify.error('Tag deletion failed.');
        if (error) {
            console.log(error);
        }
    }
    function refreshGlobalTagsTable() {
        loadUnifiedTagSummary().then(combinedTags => {
            globalTagsTable.clear();
            globalTagsTable.rows.add(combinedTags);
            globalTagsTable.draw();
        });
    }
    _renameTagModal.onResult(function () {
        const updatedTagName = $('#ViewModel_ReplacementTag').val();
        abp.notify.success(`"The tag "${updatedTagName}" has been updated.`);
        refreshGlobalTagsTable();
    });

    if (abp.auth.isGranted('Unity.GrantManager.SettingManagement.Tags.Create')) {
        $('#addNewTag').click(function () {
            addNewTagModal.open();
        });
    }

    addNewTagModal.onResult(function () {
        const newTagName = $('#NewTags').val() 
        abp.notify.success(`"The tag "${newTagName}" has been created.`);
        refreshGlobalTagsTable();
    });
});
