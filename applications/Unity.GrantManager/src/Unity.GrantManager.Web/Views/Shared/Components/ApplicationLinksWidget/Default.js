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
        console.log('DEBUG: applicationLinksModal.onResult triggered - modal should close and data should refresh');
        abp.notify.success(
            'The application links have been successfully updated.',
            'Application Links'
        );
        console.log('DEBUG: Calling dataTable.ajax.reload()...');
        dataTable.ajax.reload();
        console.log('DEBUG: Modal result handler completed');
    });

    function initializeEnhancedModal() {
        console.log('DEBUG: initializeEnhancedModal started');
        let suggestionsArray = [];
        let allApplications = $('#AllApplications').val();
        let linkedApplicationsList = JSON.parse($('#LinkedApplicationsList').val() || '[]');
        let grantApplicationsList = JSON.parse($('#GrantApplicationsList').val() || '[]');
        let currentLinks = [];
        
        console.log('DEBUG: Initial data loaded:');
        console.log('  - allApplications:', allApplications);
        console.log('  - linkedApplicationsList length:', linkedApplicationsList.length);
        console.log('  - grantApplicationsList length:', grantApplicationsList.length);
        console.log('  - currentLinks length:', currentLinks.length);


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
            console.log('DEBUG: Keydown event:', e.key, 'currentSuggestions.length:', currentSuggestions.length);
            if (currentSuggestions.length === 0) return;

            switch(e.key) {
                case 'ArrowDown':
                    e.preventDefault();
                    activeSuggestionIndex = (activeSuggestionIndex + 1) % currentSuggestions.length;
                    console.log('DEBUG: ArrowDown - activeSuggestionIndex now:', activeSuggestionIndex);
                    updateSuggestionHighlight();
                    break;
                case 'ArrowUp':
                    e.preventDefault();
                    activeSuggestionIndex = activeSuggestionIndex <= 0 ? currentSuggestions.length - 1 : activeSuggestionIndex - 1;
                    console.log('DEBUG: ArrowUp - activeSuggestionIndex now:', activeSuggestionIndex);
                    updateSuggestionHighlight();
                    break;
                case 'Enter':
                    e.preventDefault();
                    console.log('DEBUG: Enter key pressed - activeSuggestionIndex:', activeSuggestionIndex, 'currentSuggestions:', currentSuggestions);
                    if (activeSuggestionIndex >= 0 && activeSuggestionIndex < currentSuggestions.length) {
                        console.log('DEBUG: Calling selectSuggestion with:', currentSuggestions[activeSuggestionIndex]);
                        selectSuggestion(currentSuggestions[activeSuggestionIndex], grantApplicationsList, linkedApplicationsList, currentLinks);
                    } else {
                        console.log('DEBUG: Enter pressed but no valid selection - activeSuggestionIndex:', activeSuggestionIndex, 'length:', currentSuggestions.length);
                    }
                    break;
                case 'Escape':
                    e.preventDefault();
                    console.log('DEBUG: Escape key pressed - closing suggestions');
                    currentSuggestions = [];
                    activeSuggestionIndex = -1;
                    removeAutoSuggest(searchContainer);
                    break;
                case 'Tab':
                    console.log('DEBUG: Tab key pressed - closing suggestions');
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

        // Helper function to select a suggestion
        function selectSuggestion(suggestion, grantApplicationsList, linkedApplicationsList, currentLinks) {
            console.log('DEBUG: selectSuggestion called with suggestion:', suggestion);
            console.log('DEBUG: grantApplicationsList length:', grantApplicationsList.length);
            console.log('DEBUG: linkedApplicationsList length:', linkedApplicationsList.length);
            console.log('DEBUG: currentLinks before:', JSON.parse(JSON.stringify(currentLinks)));
            
            const parts = suggestion.split(' - ');
            const referenceNumber = parts[0].trim();
            console.log('DEBUG: Parsed referenceNumber:', referenceNumber);
            
            // Find the full application data
            const fullApp = grantApplicationsList.find(app => app.ReferenceNo === referenceNumber);
            console.log('DEBUG: Found fullApp:', fullApp);
            
            const isDuplicate = currentLinks.some(link => link.referenceNumber === referenceNumber);
            console.log('DEBUG: Is duplicate?', isDuplicate);
            
            if (fullApp && !isDuplicate) {
                const linkType = $('#linkTypeSelect').val() || 'Related';
                console.log('DEBUG: Selected linkType:', linkType);
                
                // Try to get additional data from existing linked applications list if available
                const existingLinkedApp = linkedApplicationsList.find(app => app.referenceNumber === referenceNumber);
                console.log('DEBUG: Found existingLinkedApp:', existingLinkedApp);
                
                const newLink = {
                    referenceNumber: referenceNumber,
                    projectName: fullApp.ProjectName || 'Unknown',
                    applicantName: existingLinkedApp?.applicantName || 'To be determined',
                    category: existingLinkedApp?.category || 'Unknown',
                    applicationStatus: existingLinkedApp?.applicationStatus || 'Unknown',
                    linkType: linkType
                };
                
                console.log('DEBUG: Adding new link:', newLink);
                // Add to beginning of array so it appears at top
                currentLinks.unshift(newLink);
                console.log('DEBUG: currentLinks after adding:', JSON.parse(JSON.stringify(currentLinks)));

                console.log('DEBUG: Calling updateLinksDisplay...');
                updateLinksDisplay(currentLinks);
                $('#submissionSearch').val('');
                currentSuggestions = [];
                activeSuggestionIndex = -1;
                removeAutoSuggest($('.search-input-container'));
                console.log('DEBUG: Selection process completed successfully');
            } else {
                console.log('DEBUG: Selection failed - fullApp exists:', !!fullApp, 'isDuplicate:', isDuplicate);
            }
        }
    }

    function displayAutoSuggest(container, suggestions, grantApplicationsList, linkedApplicationsList, currentLinks) {
        removeAutoSuggest(container);

        const suggestionContainer = $('<div class="links-suggestion-container"></div>');
        const suggestionTitle = $('<div class="links-suggestion-title">ALL APPLICATIONS</div>');
        suggestionContainer.append(suggestionTitle);

        suggestions.forEach(function(suggestion) {
            const suggestionElement = $('<div class="links-suggestion-element"></div>').text(suggestion);
            
            suggestionElement.on('click', function() {
                console.log('DEBUG: Mouse click on suggestion:', suggestion);
                console.log('DEBUG: Click handler - grantApplicationsList length:', grantApplicationsList.length);
                console.log('DEBUG: Click handler - linkedApplicationsList length:', linkedApplicationsList.length);
                console.log('DEBUG: Click handler - currentLinks before:', JSON.parse(JSON.stringify(currentLinks)));
                
                const parts = suggestion.split(' - ');
                const referenceNumber = parts[0].trim();
                console.log('DEBUG: Click handler - parsed referenceNumber:', referenceNumber);
                
                // Find the full application data
                const fullApp = grantApplicationsList.find(app => app.ReferenceNo === referenceNumber);
                console.log('DEBUG: Click handler - found fullApp:', fullApp);
                
                const isDuplicate = currentLinks.some(link => link.referenceNumber === referenceNumber);
                console.log('DEBUG: Click handler - is duplicate?', isDuplicate);
                
                if (fullApp && !isDuplicate) {
                    const linkType = $('#linkTypeSelect').val() || 'Related';
                    console.log('DEBUG: Click handler - selected linkType:', linkType);
                    
                    // Try to get additional data from existing linked applications list if available
                    const existingLinkedApp = linkedApplicationsList.find(app => app.referenceNumber === referenceNumber);
                    console.log('DEBUG: Click handler - found existingLinkedApp:', existingLinkedApp);
                    
                    const newLink = {
                        referenceNumber: referenceNumber,
                        projectName: fullApp.ProjectName || 'Unknown',
                        applicantName: existingLinkedApp?.applicantName || 'To be determined',
                        category: existingLinkedApp?.category || 'Unknown',
                        applicationStatus: existingLinkedApp?.applicationStatus || 'Unknown',
                        linkType: linkType
                    };
                    
                    console.log('DEBUG: Click handler - adding new link:', newLink);
                    // Add to beginning of array so it appears at top
                    currentLinks.unshift(newLink);
                    console.log('DEBUG: Click handler - currentLinks after adding:', JSON.parse(JSON.stringify(currentLinks)));

                    console.log('DEBUG: Click handler - calling updateLinksDisplay...');
                    updateLinksDisplay(currentLinks);
                    $('#submissionSearch').val('');
                    removeAutoSuggest(container);
                    console.log('DEBUG: Click handler - selection process completed successfully');
                } else {
                    console.log('DEBUG: Click handler - selection failed - fullApp exists:', !!fullApp, 'isDuplicate:', isDuplicate);
                }
            });

            suggestionContainer.append(suggestionElement);
        });

        container.append(suggestionContainer);
    }

    function removeAutoSuggest(container) {
        container.find('.links-suggestion-container').remove();
    }

    function updateLinksDisplay(currentLinks) {
        console.log('DEBUG: updateLinksDisplay called with currentLinks:', JSON.parse(JSON.stringify(currentLinks)));
        const linksContainer = $('#linksContainer');
        const noLinksMessage = $('#noLinksMessage');
        console.log('DEBUG: linksContainer element found:', linksContainer.length > 0);
        console.log('DEBUG: noLinksMessage element found:', noLinksMessage.length > 0);

        if (currentLinks.length === 0) {
            console.log('DEBUG: No links to display, showing no links message');
            noLinksMessage.show();
            linksContainer.find('.link-item').remove();
            return;
        }

        console.log('DEBUG: Displaying', currentLinks.length, 'links');
        noLinksMessage.hide();
        linksContainer.find('.link-item').remove();

        currentLinks.forEach(function(link, index) {
            console.log('DEBUG: Creating link element for index', index, ':', link);
            const linkElement = createLinkElement(link, index, currentLinks);
            linksContainer.append(linkElement);
            console.log('DEBUG: Link element appended for:', link.referenceNumber);
        });
        
        console.log('DEBUG: updateLinksDisplay completed, DOM should be updated');
    }

    function createLinkElement(link, index, currentLinks) {
        const linkTypeClass = (link.linkType || 'related').toLowerCase();
        
        // Ensure we have valid values, not undefined
        const referenceNumber = link.referenceNumber || 'Unknown Reference';
        const applicantName = link.applicantName || 'Unknown Applicant';
        const category = link.category || 'Unknown Category';
        const applicationStatus = link.applicationStatus || 'Status Unavailable';
        const linkType = link.linkType || 'Related';
        
        const linkElement = $(`
            <div class="link-item ${linkTypeClass}">
                <div class="link-info">
                    <span class="link-reference">${referenceNumber}</span>
                    <span class="link-applicant">${applicantName}</span>
                    <span class="link-category">(${category})</span>
                    <span class="link-status">${applicationStatus}</span>
                </div>
                <span class="link-type-badge ${linkTypeClass}">${linkType}</span>
                <button type="button" class="link-delete-btn" data-index="${index}" title="Delete Link">
                    <i class="fa fa-times"></i>
                </button>
            </div>
        `);

        // Handle delete button
        linkElement.find('.link-delete-btn').on('click', function() {
            const indexToRemove = $(this).data('index');
            currentLinks.splice(indexToRemove, 1);
            updateLinksDisplay(currentLinks);
        });

        return linkElement;
    }

    function setupFormSubmission(currentLinks) {
        $('#applicationLinksForm').off('submit').on('submit', function(e) {
            e.preventDefault(); // Prevent traditional form submission
            console.log('DEBUG: Form submission triggered (AJAX mode)');
            console.log('DEBUG: currentLinks at submission:', JSON.parse(JSON.stringify(currentLinks)));
            
            // Update the hidden field with current links data
            const linksWithTypes = currentLinks.map(link => ({
                ReferenceNumber: link.referenceNumber,
                ProjectName: link.projectName,
                LinkType: link.linkType
            }));

            console.log('DEBUG: linksWithTypes for submission:', linksWithTypes);
            
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
            
            console.log('DEBUG: Sending AJAX request...');
            
            // Submit via AJAX
            $.ajax({
                url: '/ApplicationLinks/ApplicationLinksModal?handler=OnPostAsync',
                type: 'POST',
                data: formData,
                processData: false,
                contentType: false,
                success: function(response) {
                    console.log('DEBUG: AJAX success response:', response);
                    // Trigger modal result manually since ABP may not detect AJAX
                    applicationLinksModal.close();
                    abp.notify.success(
                        'The application links have been successfully updated.',
                        'Application Links'
                    );
                    dataTable.ajax.reload();
                    console.log('DEBUG: Modal closed and data refreshed successfully');
                },
                error: function(xhr, status, error) {
                    console.error('DEBUG: AJAX error:', status, error);
                    abp.notify.error('Error updating application links: ' + error);
                }
            });
            
            return false; // Prevent any default submission
        });
    }
});
