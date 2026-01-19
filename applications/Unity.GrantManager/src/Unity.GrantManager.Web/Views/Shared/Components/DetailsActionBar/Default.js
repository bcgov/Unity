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
           
    $('#approveApplications').on('click', function () {
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

    $('#startAssessment').on('click', function () {
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

    // Helper function to process tag items and group them by application ID
    function processTagItems(tags, groupedTags) {
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
    }

    // Helper function to ensure all application IDs have entries in groupedTags
    function initializeApplicationEntries(applicationIds, groupedTags) {
        applicationIds.forEach(function (id) {
            if (!groupedTags.hasOwnProperty(id)) {
                groupedTags[id] = [];
            }
        });
    }

    // Helper function to check if tag exists in application tags
    function tagExistsInApp(tag, appTags) {
        return appTags.some(n => n.id === tag.id);
    }

    // Helper function to filter common tags between two applications
    function filterCommonTags(prevTags, nextTags) {
        return prevTags.filter(p => tagExistsInApp(p, nextTags));
    }

    // Helper function to calculate common tags across all applications
    function calculateCommonTags(groupedTags) {
        let groupedValues = Object.values(groupedTags);
        if (groupedValues.length === 0) return [];
        
        return groupedValues.reduce(filterCommonTags);
    }

    // Helper function to check if tag is common
    function isCommonTag(tag, commonTags) {
        return commonTags.some(ct => ct.id === tag.id);
    }

    // Helper function to sort tags by name
    function sortTagsByName(tags) {
        return [...tags].sort((a, b) => a.name.localeCompare(b.name));
    }

    // Helper function to create application tag summary
    function createAppTagSummary(appId, tagList, commonTags) {
        let uncommonTags = tagList.filter(tag => !isCommonTag(tag, commonTags));
        
        return {
            applicationId: appId,
            commonTags: sortTagsByName(commonTags),
            uncommonTags: sortTagsByName(uncommonTags)
        };
    }

    // Helper function to create tag summary data
    function createTagSummary(groupedTags, commonTags) {
        return Object.entries(groupedTags).map(([appId, tagList]) => 
            createAppTagSummary(appId, tagList, commonTags)
        );
    }

    // Helper function to collect uncommon tags
    function collectUncommonTags(groupedTags, commonTags) {
        let uncommonTags = [];
        Object.entries(groupedTags).forEach(([appId, tagList]) => {
            let uncommon = tagList.filter(tag => !isCommonTag(tag, commonTags));
            uncommonTags = uncommonTags.concat(uncommon);
        });
        return uncommonTags;
    }

    // Helper function to build tag input array
    function buildTagInputArray(commonTags, uncommonTags) {
        let tagInputArray = [];

        if (uncommonTags.length > 0) {
            tagInputArray.push({
                tagId: '00000000-0000-0000-0000-000000000000',
                name: 'Uncommon Tags',
                class: 'tags-uncommon',
                id: '00000000-0000-0000-0000-000000000000'
            });
        }

        commonTags.forEach(function (tag) {
            tagInputArray.push({
                tagId: tag.id,
                name: tag.name,
                class: 'tags-common',
                id: tag.id
            });
        });

        return tagInputArray;
    }

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
            let groupedTags = {};

            let allTags = await unity.grantManager.globalTag.tags.getList();
            let tags = await unity.grantManager.grantApplications.applicationTags.getListWithCacheKey(cacheKey);

            processTagItems(tags, groupedTags);
            initializeApplicationEntries(applicationIds, groupedTags);

            let commonTags = calculateCommonTags(groupedTags);
            let alltags = createTagSummary(groupedTags, commonTags);
            let uncommonTags = collectUncommonTags(groupedTags, commonTags);

            $('#TagsJson').val(JSON.stringify(alltags));

            let tagInputArray = buildTagInputArray(commonTags, uncommonTags);

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

    $('#completeAssessment').on('click', function () {
        completeAssessmentModal.open({
            applicationIds: JSON.stringify(new Array(selectedApplicationIds)),
            operation: 'ASSESSMENT_COMPLETED',
            message: 'Are you sure you want to complete assessment for this application?',
            title: 'Complete Assessment',
        });
    });

    $('#tagApplication').on('click', function () {
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
