const TagTypes = {};
let userCanUpdate = abp.auth.isGranted('Unity.GrantManager.SettingManagement.Tags.Update');
let userCanDelete = abp.auth.isGranted('Unity.GrantManager.SettingManagement.Tags.Delete');

function defineTagSummaryColumnDefs() {
    // Define columns - start with fixed columns
    const columnDefs = [
        {
            title: "Tags",
            name: 'text',
            data: 'text'
        }
    ];

    // Add a column for each tag type
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

    // Add the actions column
    columnDefs.push({
        title: "Actions",
        name: 'actions',
        data: 'text',
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
        tagTypeKey: Object.keys(TagTypes).find(key => TagTypes[key] === tagType)
    };
}

function getUnifiedTagSummaryAjax(requestData, callback, settings) {
    // Fetch data from all tag types and combine
    let promises = Object.values(TagTypes).map(tagType =>
        tagType.service.getTagSummary()
            .then(result =>
                (result.items || []).map(tag => mapTagWithType(tag, tagType))
            )
    );

    Promise.all(promises).then(results => {
        // Combine all tags into a single array
        const allTags = results.flat();

        // Create a map to combine tags with the same text
        const tagMap = new Map();

        allTags.forEach(tag => {
            if (!tagMap.has(tag.text)) {
                // Initialize a new entry for this tag text
                tagMap.set(tag.text, {
                    text: tag.text,
                    totalCount: 0,
                    tagTypeCounts: {},
                    tagTypeKeys: {}
                });
            }

            const tagEntry = tagMap.get(tag.text);
            const tagTypeName = tag.tagType;

            // Add count for this tag type
            tagEntry.tagTypeCounts[tagTypeName] = tag.count;

            // Add to total count
            tagEntry.totalCount += tag.count;

            // Store the tag type key for action buttons
            tagEntry.tagTypeKeys[tagTypeName] = tag.tagTypeKey;
        });

        // Convert map to array for DataTable
        const combinedTags = Array.from(tagMap.values());

        callback({
            recordsTotal: combinedTags.length,
            recordsFiltered: combinedTags.length,
            data: combinedTags
        });
    }).catch(error => {
        console.error("Error fetching global tags:", error);
        callback({
            recordsTotal: 0,
            recordsFiltered: 0,
            data: []
        });
    });
}

$(function () {
    abp.log.debug('TagManagement.js initialized!');

    // Modal Validation Configuration
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

            // Set up form change event listener
            formElements.form.on('change input', function() {
                // Trigger validation
                $(this).valid();
                // Update save button state based on form validity
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
        handleUpdateTag(rowData.text);
    });

    globalTagsTable.on('click', 'td button.delete-button', function (event) {
        event.stopPropagation();
        let rowData = globalTagsTable.row(event.target.closest('tr')).data();
        handleDeleteTag(rowData.text);
    });

    abp.log.debug('Global Tags Table initialized!');

    function handleUpdateTag(tagText) {
        _renameTagModal.open({
            SelectedTagText: tagText
        });
    }

    function handleDeleteTag(tagText) {
        abp.message.confirm(`Are you sure you want to delete the "${tagText}" tag?`, "Delete Tag?")
            .then(function (confirmed) {
                if (confirmed) {
                    try {
                        // Needs to be fixed to work globally
                        unity.grantManager.grantApplications.applicationTags.deleteTagGlobal(tagText)
                            .done(function (result) {
                                abp.notify.success(`The tag "${tagText}" has been deleted.`);

                                if (globalTagsTable) {
                                    globalTagsTable.ajax.reload(); // Also refresh global table
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

    _renameTagModal.onResult(function () {
        globalTagsTable.ajax.reload();
    });
});