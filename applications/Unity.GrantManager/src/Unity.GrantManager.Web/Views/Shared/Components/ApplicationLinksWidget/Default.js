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
                $('#' + tag).html(count);
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
        var linkId = $(this).data('link-id');
        
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


        if (allApplications) {
            suggestionsArray = allApplications.split(',');
        }

        // Initialize current links from existing data
        linkedApplicationsList.forEach(function(linkedApp) {
            // LinkType conversion: 0 = Related, 1 = Parent, 2 = Child
            const linkTypeMap = { 0: 'Related', 1: 'Parent', 2: 'Child' };
            
            // Prioritize PascalCase properties (as seen in logs) with fallback to camelCase
            currentLinks.push({
                referenceNumber: linkedApp.ReferenceNumber || linkedApp.referenceNumber || 'Unknown',
                projectName: linkedApp.ProjectName || linkedApp.projectName || 'Unknown',
                applicantName: linkedApp.ApplicantName || linkedApp.applicantName || 'Unknown',
                category: linkedApp.Category || linkedApp.category || 'Unknown',
                applicationStatus: linkedApp.ApplicationStatus || linkedApp.applicationStatus || 'Unknown',
                linkType: linkTypeMap[linkedApp.LinkType] || linkTypeMap[linkedApp.linkType] || linkedApp.LinkType || linkedApp.linkType || 'Related'
            });
        });

        // Display existing links
        updateLinksDisplay(currentLinks);

        // Setup auto-suggest for search input
        setupAutoSuggest(suggestionsArray, grantApplicationsList, linkedApplicationsList, currentLinks);

        // Setup form submission
        setupFormSubmission(currentLinks);
    }

    function setupAutoSuggest(suggestionsArray, grantApplicationsList, linkedApplicationsList, currentLinks) {
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
                    displayAutoSuggest(searchContainer, suggestions, grantApplicationsList, linkedApplicationsList, currentLinks);
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
                        selectSuggestion(currentSuggestions[activeSuggestionIndex], grantApplicationsList, linkedApplicationsList, currentLinks);
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

    // Helper function to select a suggestion - moved outside setupAutoSuggest for proper scope
    function selectSuggestion(suggestion, grantApplicationsList, linkedApplicationsList, currentLinks) {
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
        
        const linkType = $('#linkTypeSelect').val() || 'Related';
        
        // Create link with unique ID for safe updates
        const uniqueId = Date.now() + '_' + Math.random();
        const newLink = {
            id: uniqueId,
            referenceNumber: referenceNumber,
            projectName: fullApp.ProjectName || 'Unknown',
            applicantName: 'Loading...',
            category: 'Loading...',
            applicationStatus: 'Loading...',
            linkType: linkType,
            isLoading: true,
            isNew: true
        };
        
        // Add to beginning of array so it appears at top
        currentLinks.unshift(newLink);
        updateLinksDisplay(currentLinks);
        
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
                    updateLinksDisplay(currentLinks);
                }
                // If not found or mismatch, ignore (link was deleted)
            },
            error: function(xhr, status, error) {
                console.error('Error fetching application details:', error);
                const linkToUpdate = currentLinks.find(link => link.id === uniqueId);
                if (linkToUpdate) {
                    linkToUpdate.applicantName = 'Failed to load';
                    linkToUpdate.category = 'Failed to load';
                    linkToUpdate.applicationStatus = 'Unknown';
                    linkToUpdate.isLoading = false;
                    linkToUpdate.hasError = true;
                    updateLinksDisplay(currentLinks);
                }
            },
            timeout: 5000 // 5 second timeout
        });
    }

    function displayAutoSuggest(container, suggestions, grantApplicationsList, linkedApplicationsList, currentLinks) {
        removeAutoSuggest(container);

        const suggestionContainer = $('<div class="links-suggestion-container"></div>');
        const suggestionTitle = $('<div class="links-suggestion-title">ALL APPLICATIONS</div>');
        suggestionContainer.append(suggestionTitle);

        suggestions.forEach(function(suggestion) {
            const suggestionElement = $('<div class="links-suggestion-element"></div>').text(suggestion);
            
            suggestionElement.on('click', function() {
                selectSuggestion(suggestion, grantApplicationsList, linkedApplicationsList, currentLinks);
            });

            suggestionContainer.append(suggestionElement);
        });

        container.append(suggestionContainer);
    }

    function removeAutoSuggest(container) {
        container.find('.links-suggestion-container').remove();
    }

    function updateLinksDisplay(currentLinks) {
        const linksContainer = $('#linksContainer');
        const noLinksMessage = $('#noLinksMessage');

        if (currentLinks.length === 0) {
            noLinksMessage.show();
            linksContainer.find('.link-item').remove();
            return;
        }

        noLinksMessage.hide();
        linksContainer.find('.link-item').remove();

        currentLinks.forEach(function(link, index) {
            const linkElement = createLinkElement(link, index, currentLinks);
            linksContainer.append(linkElement);
        });
    }

    function createLinkElement(link, index, currentLinks) {
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
        
        // Add visual indicators for special states
        let statusBadges = '';
        if (link.isNew) {
            statusBadges += '<span class="badge bg-success ms-2">NEW</span>';
        }
        if (link.hasError) {
            statusBadges += '<span class="link-error-icon ms-2" title="Failed to load complete details. The link will still be created with available information."><i class="fa fa-exclamation-triangle"></i></span>';
        }
        
        // Apply loading styles to text
        const applicantDisplay = link.isLoading ? '<span class="loading-text">' + applicantName + '</span>' : applicantName;
        const categoryDisplay = link.isLoading ? '<span class="loading-text">(' + category + ')</span>' : '(' + category + ')';
        const statusDisplay = link.isLoading ? '<span class="loading-text">' + applicationStatus + '</span>' : applicationStatus;
        
        const linkElement = $(`
            <div class="link-item ${linkTypeClass}${additionalClasses}">
                <div class="link-info">
                    <span class="link-reference">${referenceNumber}</span>
                    <span class="link-applicant">${applicantDisplay}</span>
                    <span class="link-category">${categoryDisplay}</span>
                    <span class="link-status">${statusDisplay}</span>
                    ${statusBadges}
                </div>
                <span class="link-type-badge ${linkTypeClass}">${linkType}</span>
                <button type="button" class="link-delete-btn" data-index="${index}" title="Delete Link">
                    <i class="fa fa-times"></i>
                </button>
            </div>
        `);

        // Handle delete button - use link ID for safer deletion
        linkElement.find('.link-delete-btn').on('click', function() {
            if (link.id) {
                // Remove by ID for safer deletion
                const linkIndex = currentLinks.findIndex(l => l.id === link.id);
                if (linkIndex !== -1) {
                    currentLinks.splice(linkIndex, 1);
                    updateLinksDisplay(currentLinks);
                }
            } else {
                // Fallback to index-based deletion for existing links
                const indexToRemove = $(this).data('index');
                currentLinks.splice(indexToRemove, 1);
                updateLinksDisplay(currentLinks);
            }
        });

        // Handle error icon click to retry
        linkElement.find('.link-error-icon').on('click', function() {
            if (link.referenceNumber && link.hasError) {
                // Retry fetching details
                link.isLoading = true;
                link.hasError = false;
                updateLinksDisplay(currentLinks);
                
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
                            updateLinksDisplay(currentLinks);
                        }
                    },
                    error: function() {
                        link.applicantName = 'Failed to load';
                        link.category = 'Failed to load';
                        link.applicationStatus = 'Unknown';
                        link.isLoading = false;
                        link.hasError = true;
                        updateLinksDisplay(currentLinks);
                    }
                });
            }
        });

        return linkElement;
    }

    function setupFormSubmission(currentLinks) {
        $('#applicationLinksForm').off('submit').on('submit', function(e) {
            e.preventDefault(); // Prevent traditional form submission
            
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
});
