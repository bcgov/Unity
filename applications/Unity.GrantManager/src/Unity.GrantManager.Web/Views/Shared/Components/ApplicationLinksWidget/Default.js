    // Simple HTML escape utility to prevent XSS
    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;')
            .replace(/\//g, '&#47;');
    }

$(function () {
    const l = abp.localization.getResource('GrantManager');
    const nullPlaceholder = '—';
    let selectedApplicationId = decodeURIComponent($("#DetailsViewApplicationId").val());    

    let inputAction = function (requestData, dataTableSettings) {
        return selectedApplicationId;
    };

    let responseCallback = function (result) {
        if (result) {
            // Filter out the current application from the results
            let filteredResult = result.filter(function(item) {
                return item.applicationId !== selectedApplicationId;
            });
            
            // Update the data-count attribute
            $('.links-container').attr('data-count', filteredResult.length);
            
            // Update the tab count directly
            setTimeout(() => {
                const tag = $('.links-container').data('linkscounttag');
                const count = $('.links-container').attr('data-count');
                // Set text safely: parse count as integer, fallback to 0 if not valid
                $('#' + tag).text(Number(count) || 0);
            }, 50);
            
            return {
                data: filteredResult
            };
        }

        $('.links-container').attr('data-count', 0);
        $('#application_links_count').html(0);
        return {
            data: []
        };
    };

    const dataTable = $('#ApplicationLinksTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            order: [[1, 'asc']],
            searching: false,
            paging: false,
            select: false,
            info: false,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.grantApplications.applicationLinks.getListByApplication, inputAction, responseCallback
            ),
            drawCallback: function() {
                this.api().columns.adjust();
            },
            columnDefs: [
                {
                    title: l('ApplicationLinks:Category'),
                    data: 'category',
                    width: '30%'
                },
                {
                    title: l('ApplicationLinks:ID'),
                    data: 'referenceNumber',
                    width: '20%',
                    render: function (data, type, full) {
                        if (type === 'display') {
                            return '<a href="/GrantApplications/Details?ApplicationId=' + full.applicationId + '" target="_self" class="link-primary text-decoration-underline">' + data + '</a>';
                        }
                        return data;
                    }
                },
                {
                    title: l('ApplicationLinks:Status'),
                    data: 'applicationStatus',
                    width: '25%'
                },
                {
                    title: l('ApplicationLinks:LinkType'),
                    data: 'linkType',
                    width: '20%',
                    render: function (data) {
                        return data ?? nullPlaceholder;
                    }
                },
                {
                    title: '',
                    data: 'id',
                    width: '5%',
                    className: 'text-center',
                    render: function (data, type, full, meta) {
                        return '<button class="btn btn-link p-0 delete-link-btn" data-link-id="' + data + '" title="Delete Link" style="color: #0066cc; text-decoration: none;"><i class="fa fa-times"></i></button>';
                    },
                    orderable: false
                }
            ],
        })
    );

    // Handle delete button clicks using event delegation
    $('#ApplicationLinksTable').on('click', '.delete-link-btn', function(e) {
        e.preventDefault();
        let linkId = $(this).data('link-id');
        
        abp.message.confirm(
            'Are you sure you want to delete this application link?',
            'Delete Application Link',
            function (isConfirmed) {
                if (isConfirmed) {
                    unity.grantManager.grantApplications.applicationLinks.deleteWithPair(linkId)
                        .then(function () {
                            abp.notify.success('Application link deleted successfully.');
                            dataTable.ajax.reload();
                        })
                        .catch(function (error) {
                            abp.notify.error('Error deleting application link.');
                        });
                }
            }
        );
    });

    let applicationLinksModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'ApplicationLinks/ApplicationLinksModal',
    });

    $('body').on('click','#addLinksRecordsBtn',function(e){
        e.preventDefault();
        applicationLinksModal.open({
            applicationId: selectedApplicationId,
        });
    });

    applicationLinksModal.onOpen(function () {
        // Initialize the new enhanced modal
        initializeEnhancedModal();
    });

    applicationLinksModal.onResult(function () {
        abp.notify.success(
            'The application links have been successfully updated.',
            'Application Links'
        );
        dataTable.ajax.reload();
    });

    function initializeEnhancedModal() {
        let suggestionsArray = [];
        let allApplications = $('#AllApplications').val();
        let linkedApplicationsList = JSON.parse($('#LinkedApplicationsList').val() || '[]');
        let grantApplicationsList = JSON.parse($('#GrantApplicationsList').val() || '[]');
        let currentLinks = [];
        let deletedLinks = [];  // Track deleted existing links
        let validationErrors = {}; // { referenceNumber: boolean }
        let hasValidationErrors = false;
        let hasChanges = false; // Track if any changes made
        const VALIDATION_ERROR_MESSAGE = "Error: Cannot link the submissions that are already connected as either a child or parent to an existing submission. The Parent & Child linking is in a single level";
        
        // Generate unique IDs for existing links once
        const timestamp = Date.now();
        
        // Initialize current links from existing data
        linkedApplicationsList.forEach(function(linkedApp, idx) {
            // LinkType conversion: 0 = Related, 1 = Parent, 2 = Child
            const linkTypeMap = { 0: 'Related', 1: 'Parent', 2: 'Child' };
            const uniqueId = 'existing_' + idx + '_' + timestamp;
            
            const mappedLinkType = linkTypeMap[linkedApp.LinkType] || linkTypeMap[linkedApp.linkType] || linkedApp.LinkType || linkedApp.linkType || 'Related';
            
            // Prioritize PascalCase properties with fallback to camelCase
            const linkData = {
                id: uniqueId,
                referenceNumber: linkedApp.ReferenceNumber || linkedApp.referenceNumber || 'Unknown',
                projectName: linkedApp.ProjectName || linkedApp.projectName || 'Unknown',
                applicantName: linkedApp.ApplicantName || linkedApp.applicantName || 'Unknown',
                category: linkedApp.Category || linkedApp.category || 'Unknown',
                applicationStatus: linkedApp.ApplicationStatus || linkedApp.applicationStatus || 'Unknown',
                linkType: mappedLinkType,
                isExisting: true,
                isNew: false
            };
            
            currentLinks.push(linkData);
        });
        
        // Store original existing links (immutable reference for comparison)
        const originalExistingLinks = currentLinks.map(link => ({...link}));
        
        // Store original state for change detection
        const originalLinksSnapshot = JSON.stringify(currentLinks.map(link => ({
            referenceNumber: link.referenceNumber,
            linkType: link.linkType,
            isExisting: link.isExisting
        })));

        // Populate suggestions array from all applications
        if (allApplications) {
            suggestionsArray = allApplications.split(',');
        }
        
        // Helper functions for change detection and validation
        function detectChanges() {
            // Check if any links were deleted
            if (deletedLinks.length > 0) {
                hasChanges = true;
                return;
            }
            
            // Check if any new links were added
            const hasNewLinks = currentLinks.some(link => link.isNew && !link.isExisting);
            if (hasNewLinks) {
                hasChanges = true;
                return;
            }
            
            // Check if any link types were changed
            const currentSnapshot = JSON.stringify(currentLinks.map(link => ({
                referenceNumber: link.referenceNumber,
                linkType: link.linkType,
                isExisting: link.isExisting
            })));
            
            hasChanges = currentSnapshot !== originalLinksSnapshot;
        }
        
        function updateSaveButtonState() {
            const saveButton = $('#applicationLinksModelSaveBtn');
            let disableButton = false;
            let disableReason = '';
            
            // Check validation errors first (higher priority)
            if (hasValidationErrors) {
                disableButton = true;
                disableReason = 'Please resolve validation errors before saving';
            }
            // Then check if there are changes
            else if (!hasChanges) {
                disableButton = true;
                disableReason = 'No changes to save';
            }
            
            if (disableButton) {
                saveButton.prop('disabled', true);
                saveButton.attr('title', disableReason);
            } else {
                saveButton.prop('disabled', false);
                saveButton.removeAttr('title');
            }
        }
        
        async function validateAllLinks() {
            const linksToValidate = getLinksToValidate();
            
            if (linksToValidate.length === 0) {
                clearValidationState();
                return;
            }
            
            try {
                const response = await callValidationEndpoint(linksToValidate);
                processValidationResponse(response);
            } catch (error) {
                clearValidationState();
            }
        }
        
        function getLinksToValidate() {
            return currentLinks.filter(link => 
                link.linkType !== 'Related' && 
                !link.isLoading &&
                !deletedLinks.some(deleted => deleted.referenceNumber === link.referenceNumber)
            );
        }
        
        async function callValidationEndpoint(linksToValidate) {
            const currentApplicationId = $('#CurrentApplicationId').val();
            const validationRequests = linksToValidate.map(link => ({
                referenceNumber: link.referenceNumber,
                linkType: link.linkType
            }));
            
            // Build query string for array serialization
            let queryParams = new URLSearchParams();
            queryParams.append('currentApplicationId', currentApplicationId);
            
            validationRequests.forEach((link, index) => {
                queryParams.append(`links[${index}].referenceNumber`, link.referenceNumber);
                queryParams.append(`links[${index}].linkType`, link.linkType);
            });
            
            return await $.ajax({
                url: `/ApplicationLinks/ApplicationLinksModal?handler=ValidateLinks&${queryParams.toString()}`,
                type: 'GET'
            });
        }
        
        function processValidationResponse(response) {
            validationErrors = response.validationErrors || {};
            
            // Filter out errors for deleted links
            const activeValidationErrors = Object.keys(validationErrors)
                .filter(refNum => !deletedLinks.some(deleted => deleted.referenceNumber === refNum))
                .reduce((acc, refNum) => {
                    acc[refNum] = validationErrors[refNum];
                    return acc;
                }, {});
            
            hasValidationErrors = Object.values(activeValidationErrors).some(v => v);
            
            // Update link validation state - only for links that could have been validated
            currentLinks.forEach(link => {
                const isDeleted = deletedLinks.some(deleted => deleted.referenceNumber === link.referenceNumber);
                const isRelated = link.linkType === 'Related';
                
                if (isDeleted || isRelated) {
                    // Deleted links and Related links should never have validation errors
                    link.hasValidationError = false;
                    link.validationErrorMessage = '';
                } else {
                    // Only Parent/Child links that weren't deleted can have validation errors
                    const hasError = validationErrors[link.referenceNumber];
                    
                    if (hasError) {
                        link.hasValidationError = true;
                        link.validationErrorMessage = VALIDATION_ERROR_MESSAGE;
                    } else {
                        link.hasValidationError = false;
                        link.validationErrorMessage = '';
                    }
                }
            });
            
            updateLinksDisplay();
            updateSaveButtonState();
            updateValidationSummary();
        }
        
        function clearValidationState() {
            validationErrors = {};
            hasValidationErrors = false;
            
            // Clear validation state from all current links
            currentLinks.forEach(link => {
                link.hasValidationError = false;
                link.validationErrorMessage = '';
            });
            
            updateLinksDisplay();
            updateSaveButtonState();
            updateValidationSummary();
        }
        
        function updateValidationSummary() {
            const summaryContainer = $('#validationSummaryContainer');
            
            if (hasValidationErrors) {
                const errorCount = Object.values(validationErrors).filter(v => v).length;
                if (!summaryContainer.length) {
                    // Create summary if doesn't exist
                    const summaryHtml = `
                        <div id="validationSummaryContainer" class="validation-summary alert alert-danger" role="alert">
                            <i class="fa fa-exclamation-triangle validation-summary-icon"></i>
                            <strong>Validation Error:</strong> <span id="validationErrorCount">${errorCount}</span> link(s) cannot be saved. 
                            Cannot link submissions that are already connected as either a child or parent. 
                            Please remove the highlighted links.
                        </div>
                    `;
                    $(summaryHtml).insertBefore('.links-display-area');
                } else {
                    $('#validationErrorCount').text(errorCount);
                    summaryContainer.show();
                }
            } else if (summaryContainer.length) {
                summaryContainer.hide();
            }
        }

        function updateLinksDisplay() {
            const linksContainer = $('#linksContainer');
            const noLinksMessage = $('#noLinksMessage');

            if (currentLinks.length === 0) {
                noLinksMessage.show();
                linksContainer.find('.link-item').remove();
            } else {
                noLinksMessage.hide();
                linksContainer.find('.link-item').remove();

                currentLinks.forEach(function(link, index) {
                    const linkElement = createLinkElement(link, index);
                    linksContainer.append(linkElement);
                });
            }
            
            detectChanges();
            updateSaveButtonState();
        }

        function removeLinkFromList(link, capturedIndex) {
            // Clear validation errors for the link being removed
            if (validationErrors[link.referenceNumber]) {
                delete validationErrors[link.referenceNumber];
            }
            
            if (link.id) {
                // Remove by ID for safer deletion
                const linkIndex = currentLinks.findIndex(l => l.id === link.id);
                if (linkIndex !== -1) {
                    currentLinks.splice(linkIndex, 1);
                    updateLinksDisplay();
                }
            } else {
                // Use the captured index instead of trying to get it from 'this'
                currentLinks.splice(capturedIndex, 1);
                updateLinksDisplay();
            }
        }

        function createLinkElement(link, index) {
            const linkTypeClass = (link.linkType || 'related').toLowerCase();
            
            // Ensure we have valid values, not undefined
            const referenceNumber = link.referenceNumber || 'Unknown Reference';
            const applicantName = link.applicantName || 'Unknown Applicant';
            const category = link.category || 'Unknown Category';
            const applicationStatus = link.applicationStatus || 'Status Unavailable';
            const linkType = link.linkType || 'Related';
            
            // Add additional classes based on state
            let additionalClasses = '';
            if (link.isLoading) additionalClasses += ' loading';
            if (link.hasError) additionalClasses += ' error';
            if (link.isNew) additionalClasses += ' new-item';
            if (link.hasValidationError) {
                additionalClasses += ' validation-error';
            }
            
            // Add visual indicators for special states
            let statusBadges = '';
            if (link.isNew) {
                statusBadges += '<span class="badge bg-success ms-2">NEW</span>';
            }
            if (link.hasError) {
                statusBadges += '<span class="link-error-icon ms-2" title="Failed to load complete details. The link will still be created with available information."><i class="fa fa-exclamation-triangle"></i></span>';
            }
            
            // Apply loading styles to text
            // Escape all interpolated user-controlled variables
            const escapedApplicantName = escapeHtml(applicantName);
            const escapedCategory = escapeHtml(category);
            const escapedApplicationStatus = escapeHtml(applicationStatus);
            const escapedReferenceNumber = escapeHtml(referenceNumber);
            const escapedLinkType = escapeHtml(linkType);

            const applicantDisplay = link.isLoading ? '<span class="loading-text">' + escapedApplicantName + '</span>' : escapedApplicantName;
            const categoryDisplay = link.isLoading ? '<span class="loading-text">(' + escapedCategory + ')</span>' : '(' + escapedCategory + ')';
            const statusDisplay = link.isLoading ? '<span class="loading-text">' + escapedApplicationStatus + '</span>' : escapedApplicationStatus;
            
            // Add validation error message if exists
            let validationErrorHtml = '';
            if (link.hasValidationError) {
                validationErrorHtml = `
                    <div class="validation-error-message">
                        <i class="fa fa-exclamation-circle validation-error-icon"></i>
                        ${escapeHtml(link.validationErrorMessage)}
                    </div>
                `;
            }
            
            const linkElement = $(`
                <div class="link-item ${linkTypeClass}${additionalClasses}">
                    <div class="link-info">
                        <span class="link-reference">${escapedReferenceNumber}</span>
                        <span class="link-applicant">${applicantDisplay}</span>
                        <span class="link-category">${categoryDisplay}</span>
                        <span class="application-status">${statusDisplay}</span>
                        ${statusBadges}
                    </div>
                    <span class="link-type-badge ${linkTypeClass}">${escapedLinkType}</span>
                    <button type="button" class="link-delete-btn" data-index="${index}" title="Delete Link">
                        <i class="fa fa-times"></i>
                    </button>
                    ${validationErrorHtml}
                </div>
            `);

            // Handle delete button - use link ID for safer deletion
            linkElement.find('.link-delete-btn').on('click', function() {
                // Capture the button element and index BEFORE showing dialog
                const capturedIndex = index;
                
                // Check if this is an existing link (not new)
                if (link.isExisting && !link.isNew) {
                    // Show confirmation dialog for existing links
                    abp.message.confirm(
                        `Are you sure you want to remove the link to "${link.referenceNumber}"? This change will only take effect when you click "Save Changes".`,
                        'Remove Existing Link?',
                        function (isConfirmed) {
                            if (isConfirmed) {
                                // Track the deletion
                                deletedLinks.push({
                                    referenceNumber: link.referenceNumber,
                                    originalData: link
                                });
                                
                                // Remove from display
                                removeLinkFromList(link, capturedIndex);
                                
                                // Re-validate after removal
                                setTimeout(() => {
                                    validateAllLinks();
                                }, 100);
                            }
                        }
                    );
                } else {
                    // New link - delete without confirmation
                    removeLinkFromList(link, capturedIndex);
                    
                    // Re-validate after removal
                    setTimeout(() => {
                        validateAllLinks();
                    }, 100);
                }
            });

            // Handle error icon click to retry
            linkElement.find('.link-error-icon').on('click', function() {
                if (link.referenceNumber && link.hasError) {
                    // Retry fetching details
                    link.isLoading = true;
                    link.hasError = false;
                    updateLinksDisplay();
                    
                    $.ajax({
                        url: '/ApplicationLinks/ApplicationLinksModal?handler=ApplicationDetailsByReference',
                        type: 'GET',
                        data: { referenceNumber: link.referenceNumber },
                        success: function(response) {
                            if (link.id && response.referenceNumber === link.referenceNumber) {
                                link.applicantName = response.applicantName || 'Unknown';
                                link.category = response.category || 'Unknown';
                                link.applicationStatus = response.applicationStatus || 'Unknown';
                                link.isLoading = false;
                                updateLinksDisplay();
                            }
                        },
                        error: function() {
                            link.applicantName = 'Failed to load';
                            link.category = 'Failed to load';
                            link.applicationStatus = 'Unknown';
                            link.isLoading = false;
                            link.hasError = true;
                            updateLinksDisplay();
                        }
                    });
                }
            });

            return linkElement;
        }

        function selectSuggestion(suggestion) {
            const parts = suggestion.split(' - ');
            const referenceNumber = parts[0].trim();
            
            // Check for duplicates
            const isDuplicate = currentLinks.some(link => link.referenceNumber === referenceNumber);
            
            if (isDuplicate) {
                abp.notify.warn('This application is already linked.');
                return;
            }
            
            // Find the full application data
            const fullApp = grantApplicationsList.find(app => app.ReferenceNo === referenceNumber);
            if (!fullApp) {
                abp.notify.error('Application not found.');
                return;
            }
            
            // Check if this was an originally existing link
            const originalLink = originalExistingLinks.find(
                original => original.referenceNumber === referenceNumber
            );
            
            // Check if it's in deleted list
            const deletedIndex = deletedLinks.findIndex(
                deleted => deleted.referenceNumber === referenceNumber
            );
            
            if (originalLink && deletedIndex !== -1) {
                // RESTORE CASE: This is a deleted existing link being re-added
                const deletedLink = deletedLinks[deletedIndex];
                
                // Remove from deleted list
                deletedLinks.splice(deletedIndex, 1);
                
                // Restore the original link data
                const linkType = $('#linkTypeSelect').val() || originalLink.linkType;
                currentLinks.unshift({
                    ...deletedLink.originalData,
                    linkType: linkType, // Allow link type to be changed on restore
                    isExisting: true,
                    isNew: false,
                    isRestored: true  // Optional flag for UI indication
                });
                
                updateLinksDisplay();
                $('#submissionSearch').val('');
                removeAutoSuggest($('.search-input-container'));
                
                // Show restore notification
                abp.notify.info(`Link to "${referenceNumber}" has been restored.`);
                
                // Validate                
                setTimeout(() => {
                    validateAllLinks();
                }, 100);                
                
            } else {
                // NEW LINK CASE: Continue with existing logic
                const linkType = $('#linkTypeSelect').val() || 'Related';
                
                // Create link with unique ID for safe updates
                const uniqueId = Date.now() + '_' + Math.random(); // NOSONAR - Safe: ID only used for client-side DOM element tracking, not for security
                const newLink = {
                    id: uniqueId,
                    referenceNumber: referenceNumber,
                    projectName: fullApp.ProjectName || 'Unknown',
                    applicantName: 'Loading...',
                    category: 'Loading...',
                    applicationStatus: 'Loading...',
                    linkType: linkType,
                    isLoading: true,
                    isNew: !originalLink,  // Only new if it wasn't originally existing
                    isExisting: !!originalLink,
                    hasValidationError: false,  // Explicitly clear validation state for new links
                    validationErrorMessage: ''
                };
                
                // Add to beginning of array so it appears at top
                currentLinks.unshift(newLink);
                
                // Clear any old validation errors for this reference number
                if (validationErrors[referenceNumber]) {
                    delete validationErrors[referenceNumber];
                }
                
                updateLinksDisplay();
                
                // Clear search input
                $('#submissionSearch').val('');
                removeAutoSuggest($('.search-input-container'));
                
                // Fetch complete details via AJAX
                $.ajax({
                    url: '/ApplicationLinks/ApplicationLinksModal?handler=ApplicationDetailsByReference',
                    type: 'GET',
                    data: { referenceNumber: referenceNumber },
                    success: function(response) {
                        // Find the link by unique ID
                        const linkToUpdate = currentLinks.find(link => link.id === uniqueId);
                        
                        // Validate link still exists AND reference number matches
                        if (linkToUpdate && linkToUpdate.referenceNumber === response.referenceNumber) {
                            linkToUpdate.applicantName = response.applicantName || 'Unknown';
                            linkToUpdate.category = response.category || 'Unknown';
                            linkToUpdate.applicationStatus = response.applicationStatus || 'Unknown';
                            linkToUpdate.isLoading = false;
                            updateLinksDisplay();
                        }
                        // If not found or mismatch, ignore (link was deleted)
                        
                        // Always validate to ensure proper state management
                        setTimeout(() => {
                            validateAllLinks();
                        }, 100);
                    },
                    error: function(xhr, status, error) {
                        const linkToUpdate = currentLinks.find(link => link.id === uniqueId);
                        if (linkToUpdate) {
                            linkToUpdate.applicantName = 'Failed to load';
                            linkToUpdate.category = 'Failed to load';
                            linkToUpdate.applicationStatus = 'Unknown';
                            linkToUpdate.isLoading = false;
                            linkToUpdate.hasError = true;
                            updateLinksDisplay();
                        }
                    },
                    timeout: 5000 // 5 second timeout
                });
            }
        }

        function displayAutoSuggest(container, suggestions) {
            removeAutoSuggest(container);

            const suggestionContainer = $('<div class="links-suggestion-container"></div>');
            const suggestionTitle = $('<div class="links-suggestion-title">ALL APPLICATIONS</div>');
            suggestionContainer.append(suggestionTitle);

            suggestions.forEach(function(suggestion) {
                const suggestionElement = $('<div class="links-suggestion-element"></div>').text(suggestion);
                
                suggestionElement.on('click', function() {
                    selectSuggestion(suggestion);
                });

                suggestionContainer.append(suggestionElement);
            });

            container.append(suggestionContainer);
        }

        function removeAutoSuggest(container) {
            container.find('.links-suggestion-container').remove();
        }

        function setupAutoSuggest() {
            const searchInput = $('#submissionSearch');
            const searchContainer = $('.search-input-container');
            let currentSuggestions = [];
            let activeSuggestionIndex = -1;

            searchInput.on('input', function() {
                const inputValue = $(this).val().trim().toLowerCase();
                
                if (inputValue.length > 0) {
                    const suggestions = suggestionsArray.filter(suggestion => 
                        suggestion.toLowerCase().includes(inputValue)
                    );

                    if (suggestions.length > 0) {
                        currentSuggestions = suggestions;
                        activeSuggestionIndex = -1;
                        displayAutoSuggest(searchContainer, suggestions);
                    } else {
                        currentSuggestions = [];
                        activeSuggestionIndex = -1;
                        removeAutoSuggest(searchContainer);
                    }
                } else {
                    currentSuggestions = [];
                    activeSuggestionIndex = -1;
                    removeAutoSuggest(searchContainer);
                }
            });

            // Add keyboard navigation
            searchInput.on('keydown', function(e) {
                if (currentSuggestions.length === 0) return;

                switch(e.key) {
                    case 'ArrowDown':
                        e.preventDefault();
                        activeSuggestionIndex = (activeSuggestionIndex + 1) % currentSuggestions.length;
                        updateSuggestionHighlight();
                        break;
                    case 'ArrowUp':
                        e.preventDefault();
                        activeSuggestionIndex = activeSuggestionIndex <= 0 ? currentSuggestions.length - 1 : activeSuggestionIndex - 1;
                        updateSuggestionHighlight();
                        break;
                    case 'Enter':
                        e.preventDefault();
                        if (activeSuggestionIndex >= 0 && activeSuggestionIndex < currentSuggestions.length) {
                            selectSuggestion(currentSuggestions[activeSuggestionIndex]);
                        }
                        break;
                    case 'Escape':
                        e.preventDefault();
                        currentSuggestions = [];
                        activeSuggestionIndex = -1;
                        removeAutoSuggest(searchContainer);
                        break;
                    case 'Tab':
                        currentSuggestions = [];
                        activeSuggestionIndex = -1;
                        removeAutoSuggest(searchContainer);
                        break;
                }
            });

            // Close suggestions when clicking outside
            $(document).on('click', function(e) {
                if (!searchContainer.is(e.target) && searchContainer.has(e.target).length === 0) {
                    removeAutoSuggest(searchContainer);
                }
            });

            // Helper function to update visual highlighting
            function updateSuggestionHighlight() {
                $('.links-suggestion-element').removeClass('suggestion-active');
                if (activeSuggestionIndex >= 0) {
                    $('.links-suggestion-element').eq(activeSuggestionIndex).addClass('suggestion-active');
                }
            }
        }

        function setupFormSubmission() {
            $('#applicationLinksForm').off('submit').on('submit', function(e) {
                e.preventDefault(); // Prevent traditional form submission                        
                
                // Check for validation errors
                if (hasValidationErrors) {
                    abp.notify.error('Please remove the highlighted links before saving. ' + VALIDATION_ERROR_MESSAGE);
                    return false;
                }
                
                // Update the hidden field with current links data
                const linksWithTypes = currentLinks.map(link => ({
                    ReferenceNumber: link.referenceNumber,
                    ProjectName: link.projectName,
                    LinkType: link.linkType
                }));
                
                // Prepare form data for AJAX submission
                const formData = new FormData();
                formData.append('LinksWithTypes', JSON.stringify(linksWithTypes));
                formData.append('CurrentApplicationId', $('#CurrentApplicationId').val());
                formData.append('GrantApplicationsList', $('#GrantApplicationsList').val());
                formData.append('LinkedApplicationsList', $('#LinkedApplicationsList').val());
                
                // Add anti-forgery token
                const token = $('input[name="__RequestVerificationToken"]').val();
                if (token) {
                    formData.append('__RequestVerificationToken', token);
                }
                
                // Submit via AJAX
                $.ajax({
                    url: '/ApplicationLinks/ApplicationLinksModal?handler=OnPostAsync',
                    type: 'POST',
                    data: formData,
                    processData: false,
                    contentType: false,
                    success: function(response) {
                        // Trigger modal result manually since ABP may not detect AJAX
                        applicationLinksModal.close();
                        abp.notify.success(
                            'The application links have been successfully updated.',
                            'Application Links'
                        );
                        dataTable.ajax.reload();
                    },
                    error: function(xhr, status, error) {
                        console.error('Error updating application links:', status, error);
                        abp.notify.error('Error updating application links: ' + error);
                    }
                });
                
                return false; // Prevent any default submission
            });
        }
        
        // Initialize everything
        
        // Display existing links
        updateLinksDisplay();
        
        // Set initial state - no changes yet
        hasChanges = false;
        hasValidationErrors = false;
        updateSaveButtonState();

        // Setup auto-suggest for search input
        setupAutoSuggest();

        // Setup form submission
        setupFormSubmission();
        
        // Add handler for link type dropdown change
        $('#linkTypeSelect').on('change', function() {
            const newType = $(this).val();
            
            // When changing type, update any newly added links that don't have a type yet
            currentLinks.forEach(link => {
                if (link.isNew && !link.isExisting) {
                    link.linkType = newType;
                }
            });
            
            // Re-validate if changing to/from Parent/Child
            const shouldValidate = newType !== 'Related' || currentLinks.some(l => l.linkType !== 'Related');
            
            if (shouldValidate) {
                validateAllLinks();
            }
        });
        
        // Validate existing Parent/Child links on load
        const hasParentChildLinks = currentLinks.some(link => link.linkType !== 'Related');
        
        if (hasParentChildLinks) {
            validateAllLinks();
        }
    }
});