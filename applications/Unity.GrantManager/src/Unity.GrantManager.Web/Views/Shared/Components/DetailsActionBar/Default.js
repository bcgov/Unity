$(function () {
    let selectedApplicationIds = decodeURIComponent($("#DetailsViewApplicationId").val());    
    
    let approveApplicationsModal = new abp.ModalManager({
        viewUrl: '../Approve/ApproveApplicationsModal'
    });

    let dontApproveApplicationsModal = new abp.ModalManager({
        viewUrl: '../Approve/ApproveApplicationsModal'
    });       

    let tagApplicationModal = new abp.ModalManager({
        viewUrl: '../ApplicationTags/ApplicationTagsSelectionModal',

    });
    
    approveApplicationsModal.onResult(function () {
        abp.notify.success(
            'This application has been successfully approved',
            'Approve Application'
        );        
    });
    dontApproveApplicationsModal.onResult(function () {
        abp.notify.success(
            'This application has now been disapproved',
            'Not Approve Application'
        );        
    });
           
    $('#approveApplications').click(function () {
        approveApplicationsModal.open({
            applicationIds: JSON.stringify(new Array(selectedApplicationIds)),
            operation: 'GRANT_APPROVED',
            message: 'Are you sure you want to approve this application?',
            title: 'Approve Applications',
        });
    });

    let startAssessmentModal = new abp.ModalManager({
        viewUrl: '../Approve/ApproveApplicationsModal'
    });

    startAssessmentModal.onResult(function () {
        abp.notify.success(
            'Assessment is now started for this application',
            'Start Assessment'
        );
    });

    $('#startAssessment').click(function () {
        startAssessmentModal.open({
            applicationIds: JSON.stringify(new Array(selectedApplicationIds)),
            operation: 'UNDER_ASSESSMENT',
            message: 'Are you sure you want to start assessment for this application?',
            title: 'Start Assessment',
        });
    });

    let completeAssessmentModal = new abp.ModalManager({
        viewUrl: '../Approve/ApproveApplicationsModal'
    });

    completeAssessmentModal.onResult(function () {
        abp.notify.success(
            'Assessment is now completed for this application',
            'Completed Assessment'
        );
    });

    tagApplicationModal.onOpen(async function () {
        let tagInput = new TagsInput({
            selector: 'SelectedTags',
            duplicate: false,
            max: 50
        });

        let cacheKey = $('#CacheKey').val();
        let applicationIds = [];

        if (!cacheKey) {
            console.error("Cache key is missing");
            abp.notify.error('Failed to load application tags. Please try again.');
            return;
        }

        // Retrieve application IDs from hidden field (populated by modal code-behind)
        let selectedIds = $('#SelectedApplicationIds').val();
        applicationIds = JSON.parse(selectedIds);

        if (!applicationIds || applicationIds.length === 0) return;

        try {
            let commonTags = [];
            let uncommonTags = [];
            let allTags = [];
            let groupedTags = {};


            allTags = await unity.grantManager.globalTag.tags.getList();

            // Use cache key to avoid URL length limits with many application IDs
            let tags = await unity.grantManager.grantApplications.applicationTags.getListWithCacheKey(cacheKey);


            tags.forEach(function (item) {
                if (!item.tag) return;
                let appId = item.applicationId;
                if (!groupedTags[appId]) {
                    groupedTags[appId] = [];
                }

                let exists = groupedTags[appId].some(t => t.id === item.tag.id);
                if (!exists) {
                    groupedTags[appId].push(item.tag);
                }
            });

            applicationIds.forEach(function (id) {
                if (!groupedTags.hasOwnProperty(id)) {
                    groupedTags[id] = [];
                }
            });


            let groupedValues = Object.values(groupedTags);
            if (groupedValues.length > 0) {
                commonTags = groupedValues.reduce(function (prev, next) {
                    return prev.filter(p => next.some(n => n.id === p.id));
                });
            }
            let alltags = Object.entries(groupedTags).map(([appId, tagList]) => {
                let uncommon = tagList.filter(tag => !commonTags.some(ct => ct.id === tag.id));

                return {
                    applicationId: appId,
                    commonTags: [...commonTags].sort((a, b) => a.name.localeCompare(b.name)),
                    uncommonTags: uncommon.sort((a, b) => a.name.localeCompare(b.name))
                };
            });


            $('#TagsJson').val(JSON.stringify(alltags));

            let tagInputArray = [];


            Object.entries(groupedTags).forEach(function ([appId, tagList]) {
                let uncommon = tagList.filter(tag => !commonTags.some(ct => ct.id === tag.id));
                uncommonTags = uncommonTags.concat(uncommon);

                
            });


            if (uncommonTags.length > 0) {
                tagInputArray.unshift({
                    tagId: '00000000-0000-0000-0000-000000000000',
                    name: 'Uncommon Tags',
                    class: 'tags-uncommon',
                    id: '00000000-0000-0000-0000-000000000000'
                });
            }


            if (commonTags.length > 0) {
                commonTags.forEach(function (tag) {
                    tagInputArray.push({
                        tagId: tag.id,
                        name: tag.name,
                        class: 'tags-common',
                        id: tag.id
                    });
                });
            }

            tagInput.setSuggestions(
                (allTags || []).filter((value, index, self) =>
                    index === self.findIndex(t => t.id === value.id)
                ).sort((a, b) => a.name.localeCompare(b.name))
            );

            tagInput.addData(tagInputArray);
        } catch (error) {
            console.error("Error loading tag select list", error);
        }
    });
    tagApplicationModal.onResult(function () {
        abp.notify.success(
            'The application tags have been successfully updated.',
            'Application Tags'
        );
        PubSub.publish("ApplicationTags_refresh");
        PubSub.publish("refresh_application_list");

    });

    $('#completeAssessment').click(function () {
        completeAssessmentModal.open({
            applicationIds: JSON.stringify(new Array(selectedApplicationIds)),
            operation: 'ASSESSMENT_COMPLETED',
            message: 'Are you sure you want to complete assessment for this application?',
            title: 'Complete Assessment',
        });
    });

    $('#tagApplication').click(function () {
        // Store application IDs in distributed cache to avoid URL length limits
        unity.grantManager.applications.applicationBulkActions
            .storeApplicationIds({ applicationIds: new Array(selectedApplicationIds) })
            .then(function(response) {
                tagApplicationModal.open({
                    cacheKey: response.cacheKey,
                    actionType: 'Add'
                });
            })
            .catch(function(error) {
                abp.notify.error('Failed to prepare tag selection. Please try again.');
                console.error('Error storing application IDs:', error);
            });
    });
});
