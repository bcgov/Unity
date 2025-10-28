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
    
    // Format version number to VX.0 format
    function formatVersion(version) {
        return version ? ` V${version}.0` : '';
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
                    width: '30%',
                    render: function (data, type, full) {
                        if (type === 'display') {
                            const versionText = formatVersion(full.formVersion);
                            return `${data}${versionText}`;
                        }
                        return data;
                    }
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

    // ============= REFACTORED APPLICATION LINKS MODAL =============
    // All services and modules are defined outside the main function to reduce complexity

    // State Management Module
    const LinkStateManager = {
        state: {
            suggestionsArray: [],
            allApplications: '',
            linkedApplicationsList: [],
            grantApplicationsList: [],
            currentLinks: [],
            deletedLinks: [],
            validationErrors: {},
            hasValidationErrors: false,
            hasChanges: false,
            originalExistingLinks: [],
            originalLinksSnapshot: '',
            VALIDATION_ERROR_MESSAGE: "Error: Validation error."
        },

        initialize: function() {
            // Reset state that should not persist across modal sessions
            this.state.deletedLinks = [];
            this.state.validationErrors = {};
            this.state.hasValidationErrors = false;
            this.state.hasChanges = false;
            
            this.state.allApplications = $('#AllApplications').val();
            this.state.linkedApplicationsList = JSON.parse($('#LinkedApplicationsList').val() || '[]');
            this.state.grantApplicationsList = JSON.parse($('#GrantApplicationsList').val() || '[]');
            
            const timestamp = Date.now();
            this.state.currentLinks = this.mapExistingLinks(this.state.linkedApplicationsList, timestamp);
            this.state.originalExistingLinks = this.state.currentLinks.map(link => ({...link}));
            this.state.originalLinksSnapshot = this.createSnapshot(this.state.currentLinks);
            
            if (this.state.allApplications) {
                this.state.suggestionsArray = this.state.allApplications.split(',');
            }
        },

        mapExistingLinks: function(linkedList, timestamp) {
            const linkTypeMap = { 0: 'Related', 1: 'Parent', 2: 'Child' };
            return linkedList.map((linkedApp, idx) => {
                const uniqueId = 'existing_' + idx + '_' + timestamp;
                const mappedLinkType = this.getLinkType(linkedApp, linkTypeMap);
                
                return {
                    id: uniqueId,
                    referenceNumber: linkedApp.ReferenceNumber || linkedApp.referenceNumber || 'Unknown',
                    projectName: linkedApp.ProjectName || linkedApp.projectName || 'Unknown',
                    applicantName: linkedApp.ApplicantName || linkedApp.applicantName || 'Unknown',
                    category: linkedApp.Category || linkedApp.category || 'Unknown',
                    applicationStatus: linkedApp.ApplicationStatus || linkedApp.applicationStatus || 'Unknown',
                    linkType: mappedLinkType,
                    formVersion: linkedApp.FormVersion || linkedApp.formVersion || null,
                    isExisting: true,
                    isNew: false
                };
            });
        },

        getLinkType: function(linkedApp, linkTypeMap) {
            return linkTypeMap[linkedApp.LinkType] || 
                   linkTypeMap[linkedApp.linkType] || 
                   linkedApp.LinkType || 
                   linkedApp.linkType || 
                   'Related';
        },

        createSnapshot: function(links) {
            return JSON.stringify(links.map(link => ({
                referenceNumber: link.referenceNumber,
                linkType: link.linkType,
                isExisting: link.isExisting
            })));
        },

        getState: function() {
            return this.state;
        }
    };

    // Change Detection Service
    const ChangeDetectionService = {
        detectChanges: function(state) {
            if (state.deletedLinks.length > 0) {
                state.hasChanges = true;
                return;
            }
            
            const hasNewLinks = state.currentLinks.some(link => link.isNew && !link.isExisting);
            if (hasNewLinks) {
                state.hasChanges = true;
                return;
            }
            
            const currentSnapshot = LinkStateManager.createSnapshot(state.currentLinks);
            state.hasChanges = currentSnapshot !== state.originalLinksSnapshot;
        }
    };

    // Validation Service
    const ValidationService = {
        validateAllLinks: async function(state) {
            const linksToValidate = this.getLinksToValidate(state);
            
            if (linksToValidate.length === 0) {
                this.clearValidationState(state);
                return;
            }
            
            try {
                const response = await this.callValidationEndpoint(linksToValidate);
                this.processValidationResponse(response, state);
            } catch (error) {
                console.error('Failed to validate application links:', error);
                abp.notify.warn('Unable to validate links. You may proceed, but please verify the links are correct.');
                this.clearValidationState(state);
            }
        },

        getLinksToValidate: function(state) {
            return state.currentLinks.filter(link => 
                link.linkType !== 'Related' && 
                !link.isLoading &&
                !state.deletedLinks.some(deleted => deleted.referenceNumber === link.referenceNumber)
            );
        },

        callValidationEndpoint: async function(linksToValidate) {
            const currentApplicationId = $('#CurrentApplicationId').val();
            const queryParams = new URLSearchParams();
            queryParams.append('currentApplicationId', currentApplicationId);
            
            linksToValidate.forEach((link, index) => {
                queryParams.append(`links[${index}].referenceNumber`, link.referenceNumber);
                queryParams.append(`links[${index}].linkType`, link.linkType);
            });
            
            return await $.ajax({
                url: `/ApplicationLinks/ApplicationLinksModal?handler=ValidateLinks&${queryParams.toString()}`,
                type: 'GET'
            });
        },

        processValidationResponse: function(response, state) {
            state.validationErrors = response.validationErrors || {};
            const errorMessages = response.errorMessages || {};
            
            const activeValidationErrors = this.filterActiveErrors(state);
            state.hasValidationErrors = Object.values(activeValidationErrors).some(v => v);
            
            this.updateLinkValidationStates(state, errorMessages);
            
            UIService.updateLinksDisplay(state);
            UIService.updateSaveButtonState(state);
            UIService.updateValidationSummary(state);
        },

        filterActiveErrors: function(state) {
            return Object.keys(state.validationErrors)
                .filter(refNum => !state.deletedLinks.some(deleted => deleted.referenceNumber === refNum))
                .reduce((acc, refNum) => {
                    acc[refNum] = state.validationErrors[refNum];
                    return acc;
                }, {});
        },

        updateLinkValidationStates: function(state, errorMessages) {
            state.currentLinks.forEach(link => {
                const isDeleted = state.deletedLinks.some(deleted => deleted.referenceNumber === link.referenceNumber);
                const isRelated = link.linkType === 'Related';
                
                if (isDeleted || isRelated) {
                    link.hasValidationError = false;
                    link.validationErrorMessage = '';
                } else {
                    const hasError = state.validationErrors[link.referenceNumber];
                    link.hasValidationError = hasError;
                    link.validationErrorMessage = hasError ? 
                        (errorMessages[link.referenceNumber] || state.VALIDATION_ERROR_MESSAGE) : '';
                }
            });
        },

        clearValidationState: function(state) {
            state.validationErrors = {};
            state.hasValidationErrors = false;
            
            state.currentLinks.forEach(link => {
                link.hasValidationError = false;
                link.validationErrorMessage = '';
            });
            
            UIService.updateLinksDisplay(state);
            UIService.updateSaveButtonState(state);
            UIService.updateValidationSummary(state);
        }
    };

    // UI Service
    const UIService = {
        updateSaveButtonState: function(state) {
            const saveButton = $('#applicationLinksModelSaveBtn');
            let disableButton = false;
            let disableReason = '';
            
            if (state.hasValidationErrors) {
                disableButton = true;
                disableReason = 'Please resolve validation errors before saving';
            } else if (!state.hasChanges) {
                disableButton = true;
                disableReason = 'No changes to save';
            }
            
            saveButton.prop('disabled', disableButton);
            if (disableButton) {
                saveButton.attr('title', disableReason);
            } else {
                saveButton.removeAttr('title');
            }
        },

        updateValidationSummary: function(state) {
            const summaryContainer = $('#validationSummaryContainer');
            
            if (state.hasValidationErrors) {
                const errorCount = Object.values(state.validationErrors).filter(v => v).length;
                if (!summaryContainer.length) {
                    const summaryHtml = `
                        <div id="validationSummaryContainer" class="validation-summary alert alert-danger" role="alert">
                            <i class="fa fa-exclamation-triangle validation-summary-icon"></i>
                            <strong>Validation Error:</strong> <span id="validationErrorCount">${errorCount}</span> link(s) cannot be saved. 
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
        },

        updateLinksDisplay: function(state) {
            const linksContainer = $('#linksContainer');
            const noLinksMessage = $('#noLinksMessage');

            if (state.currentLinks.length === 0) {
                noLinksMessage.show();
                linksContainer.find('.link-item').remove();
            } else {
                noLinksMessage.hide();
                linksContainer.find('.link-item').remove();

                state.currentLinks.forEach((link, index) => {
                    const linkElement = this.createLinkElement(link, index, state);
                    linksContainer.append(linkElement);
                });
            }
            
            ChangeDetectionService.detectChanges(state);
            this.updateSaveButtonState(state);
        },

        createLinkElement: function(link, index, state) {
            const linkElement = this.buildLinkHtml(link, index);
            this.attachLinkEventHandlers(linkElement, link, index, state);
            return linkElement;
        },

        buildLinkHtml: function(link, index) {
            const linkTypeClass = (link.linkType || 'related').toLowerCase();
            let additionalClasses = '';
            
            if (link.isLoading) additionalClasses += ' loading';
            if (link.hasError) additionalClasses += ' error';
            if (link.isNew) additionalClasses += ' new-item';
            if (link.hasValidationError) additionalClasses += ' validation-error';
            
            let statusBadges = '';
            if (link.isNew) statusBadges += '<span class="badge bg-success ms-2">NEW</span>';
            if (link.hasError) statusBadges += this.buildErrorIcon();
            
            const displayData = this.prepareDisplayData(link);
            const validationErrorHtml = this.buildValidationErrorHtml(link);
            
            return $(`
                <div class="link-item ${linkTypeClass}${additionalClasses}">
                    <div class="link-info">
                        <span class="link-reference">${displayData.referenceNumber}</span>
                        <span class="link-applicant">${displayData.applicantDisplay}</span>
                        <span class="link-category">${displayData.categoryDisplay}</span>
                        <span class="application-status">${displayData.statusDisplay}</span>
                        ${statusBadges}
                    </div>
                    <span class="link-type-badge ${linkTypeClass}">${displayData.linkType}</span>
                    <button type="button" class="link-delete-btn" data-index="${index}" title="Delete Link">
                        <i class="fa fa-times"></i>
                    </button>
                    ${validationErrorHtml}
                </div>
            `);
        },

        buildErrorIcon: function() {
            return '<span class="link-error-icon ms-2" title="Failed to load complete details. The link will still be created with available information."><i class="fa fa-exclamation-triangle"></i></span>';
        },

        prepareDisplayData: function(link) {
            const referenceNumber = escapeHtml(link.referenceNumber || 'Unknown Reference');
            const applicantName = escapeHtml(link.applicantName || 'Unknown Applicant');
            const category = escapeHtml(link.category || 'Unknown Category');
            const applicationStatus = escapeHtml(link.applicationStatus || 'Status Unavailable');
            const linkType = escapeHtml(link.linkType || 'Related');
            const versionText = formatVersion(link.formVersion || link.FormVersion);
            
            const applicantDisplay = link.isLoading ? 
                `<span class="loading-text">${applicantName}</span>` : applicantName;
            const categoryDisplay = link.isLoading ? 
                `<span class="loading-text">(${category}${versionText})</span>` : `(${category}${versionText})`;
            const statusDisplay = link.isLoading ? 
                `<span class="loading-text">${applicationStatus}</span>` : applicationStatus;
            
            return {
                referenceNumber,
                applicantDisplay,
                categoryDisplay,
                statusDisplay,
                linkType
            };
        },

        buildValidationErrorHtml: function(link) {
            if (!link.hasValidationError) return '';
            
            return `
                <div class="validation-error-message">
                    <i class="fa fa-exclamation-circle validation-error-icon"></i>
                    ${escapeHtml(link.validationErrorMessage)}
                </div>
            `;
        },

        attachLinkEventHandlers: function(linkElement, link, index, state) {
            linkElement.find('.link-delete-btn').on('click', () => {
                LinkActionsService.handleDeleteLink(link, index, state);
            });

            linkElement.find('.link-error-icon').on('click', () => {
                LinkActionsService.retryLoadingDetails(link, state);
            });
        }
    };

    // Link Actions Service
    const LinkActionsService = {
        handleDeleteLink: function(link, index, state) {
            if (link.isExisting && !link.isNew) {
                this.confirmDeleteExistingLink(link, index, state);
            } else {
                this.deleteNewLink(link, index, state);
            }
        },

        confirmDeleteExistingLink: function(link, index, state) {
            abp.message.confirm(
                `Are you sure you want to remove the link to "${link.referenceNumber}"? This change will only take effect when you click "Save Changes".`,
                'Remove Existing Link?',
                (isConfirmed) => {
                    if (isConfirmed) {
                        state.deletedLinks.push({
                            referenceNumber: link.referenceNumber,
                            originalData: link
                        });
                        this.removeLinkFromList(link, index, state);
                        setTimeout(() => ValidationService.validateAllLinks(state), 100);
                    }
                }
            );
        },

        deleteNewLink: function(link, index, state) {
            this.removeLinkFromList(link, index, state);
            setTimeout(() => ValidationService.validateAllLinks(state), 100);
        },

        removeLinkFromList: function(link, index, state) {
            if (state.validationErrors[link.referenceNumber]) {
                delete state.validationErrors[link.referenceNumber];
            }
            
            if (link.id) {
                const linkIndex = state.currentLinks.findIndex(l => l.id === link.id);
                if (linkIndex !== -1) {
                    state.currentLinks.splice(linkIndex, 1);
                }
            } else {
                state.currentLinks.splice(index, 1);
            }
            
            UIService.updateLinksDisplay(state);
        },

        retryLoadingDetails: function(link, state) {
            if (!link.referenceNumber || !link.hasError) return;
            
            link.isLoading = true;
            link.hasError = false;
            UIService.updateLinksDisplay(state);
            
            $.ajax({
                url: '/ApplicationLinks/ApplicationLinksModal?handler=ApplicationDetailsByReference',
                type: 'GET',
                data: { referenceNumber: link.referenceNumber },
                success: (response) => {
                    if (link.id && response.referenceNumber === link.referenceNumber) {
                        link.applicantName = response.applicantName || 'Unknown';
                        link.category = response.category || 'Unknown';
                        link.applicationStatus = response.applicationStatus || 'Unknown';
                        link.formVersion = response.formVersion || null;
                        link.isLoading = false;
                        UIService.updateLinksDisplay(state);
                    }
                },
                error: () => {
                    link.applicantName = 'Failed to load';
                    link.category = 'Failed to load';
                    link.applicationStatus = 'Unknown';
                    link.isLoading = false;
                    link.hasError = true;
                    UIService.updateLinksDisplay(state);
                }
            });
        }
    };

    // Auto-Suggest Service
    const AutoSuggestService = {
        currentSuggestions: [],
        activeSuggestionIndex: -1,

        setupAutoSuggest: function(state) {
            const searchInput = $('#submissionSearch');
            const searchContainer = $('.search-input-container');
            
            this.attachInputHandler(searchInput, searchContainer, state);
            this.attachKeyboardHandler(searchInput, searchContainer, state);
            this.attachClickOutsideHandler(searchContainer);
        },

        attachInputHandler: function(searchInput, searchContainer, state) {
            searchInput.on('input', () => {
                const inputValue = searchInput.val().trim().toLowerCase();
                
                if (inputValue.length > 0) {
                    const suggestions = state.suggestionsArray.filter(suggestion => 
                        suggestion.toLowerCase().includes(inputValue)
                    );

                    if (suggestions.length > 0) {
                        this.currentSuggestions = suggestions;
                        this.activeSuggestionIndex = -1;
                        this.displayAutoSuggest(searchContainer, suggestions, state);
                    } else {
                        this.clearSuggestions(searchContainer);
                    }
                } else {
                    this.clearSuggestions(searchContainer);
                }
            });
        },

        attachKeyboardHandler: function(searchInput, searchContainer, state) {
            searchInput.on('keydown', (e) => {
                if (this.currentSuggestions.length === 0) return;

                switch(e.key) {
                    case 'ArrowDown':
                        e.preventDefault();
                        this.navigateDown();
                        break;
                    case 'ArrowUp':
                        e.preventDefault();
                        this.navigateUp();
                        break;
                    case 'Enter':
                        e.preventDefault();
                        this.selectCurrentSuggestion(state);
                        break;
                    case 'Escape':
                        e.preventDefault();
                        this.clearSuggestions(searchContainer);
                        break;
                    case 'Tab':
                        this.clearSuggestions(searchContainer);
                        break;
                }
            });
        },

        attachClickOutsideHandler: function(searchContainer) {
            $(document).on('click', (e) => {
                if (!searchContainer.is(e.target) && searchContainer.has(e.target).length === 0) {
                    this.removeAutoSuggest(searchContainer);
                }
            });
        },

        navigateDown: function() {
            this.activeSuggestionIndex = (this.activeSuggestionIndex + 1) % this.currentSuggestions.length;
            this.updateSuggestionHighlight();
        },

        navigateUp: function() {
            this.activeSuggestionIndex = this.activeSuggestionIndex <= 0 ? 
                this.currentSuggestions.length - 1 : this.activeSuggestionIndex - 1;
            this.updateSuggestionHighlight();
        },

        selectCurrentSuggestion: function(state) {
            if (this.activeSuggestionIndex >= 0 && this.activeSuggestionIndex < this.currentSuggestions.length) {
                LinkSelectionService.selectSuggestion(this.currentSuggestions[this.activeSuggestionIndex], state);
            }
        },

        updateSuggestionHighlight: function() {
            $('.links-suggestion-element').removeClass('suggestion-active');
            if (this.activeSuggestionIndex >= 0) {
                $('.links-suggestion-element').eq(this.activeSuggestionIndex).addClass('suggestion-active');
            }
        },

        displayAutoSuggest: function(container, suggestions, state) {
            this.removeAutoSuggest(container);

            const suggestionContainer = $('<div class="links-suggestion-container"></div>');
            const suggestionTitle = $('<div class="links-suggestion-title">ALL APPLICATIONS</div>');
            suggestionContainer.append(suggestionTitle);

            suggestions.forEach((suggestion) => {
                const suggestionElement = $('<div class="links-suggestion-element"></div>').text(suggestion);
                suggestionElement.on('click', () => {
                    LinkSelectionService.selectSuggestion(suggestion, state);
                });
                suggestionContainer.append(suggestionElement);
            });

            container.append(suggestionContainer);
        },

        removeAutoSuggest: function(container) {
            container.find('.links-suggestion-container').remove();
        },

        clearSuggestions: function(searchContainer) {
            this.currentSuggestions = [];
            this.activeSuggestionIndex = -1;
            this.removeAutoSuggest(searchContainer);
        }
    };

    // Link Selection Service
    const LinkSelectionService = {
        selectSuggestion: function(suggestion, state) {
            const parts = suggestion.split(' - ');
            const referenceNumber = parts[0].trim();
            
            if (this.isDuplicate(referenceNumber, state)) {
                abp.notify.warn('This application is already linked.');
                return;
            }
            
            const fullApp = this.findApplication(referenceNumber, state);
            if (!fullApp) {
                abp.notify.error('Application not found.');
                return;
            }
            
            if (this.isRestoringDeletedLink(referenceNumber, state)) {
                this.restoreDeletedLink(referenceNumber, state);
            } else {
                this.addNewLink(referenceNumber, fullApp, state);
            }
        },

        isDuplicate: function(referenceNumber, state) {
            return state.currentLinks.some(link => link.referenceNumber === referenceNumber);
        },

        findApplication: function(referenceNumber, state) {
            return state.grantApplicationsList.find(app => app.ReferenceNo === referenceNumber);
        },

        isRestoringDeletedLink: function(referenceNumber, state) {
            const originalLink = state.originalExistingLinks.find(
                original => original.referenceNumber === referenceNumber
            );
            const deletedIndex = state.deletedLinks.findIndex(
                deleted => deleted.referenceNumber === referenceNumber
            );
            return originalLink && deletedIndex !== -1;
        },

        restoreDeletedLink: function(referenceNumber, state) {
            const deletedIndex = state.deletedLinks.findIndex(
                deleted => deleted.referenceNumber === referenceNumber
            );
            const deletedLink = state.deletedLinks[deletedIndex];
            
            state.deletedLinks.splice(deletedIndex, 1);
            
            const linkType = $('#linkTypeSelect').val() || deletedLink.originalData.linkType;
            state.currentLinks.unshift({
                ...deletedLink.originalData,
                linkType: linkType,
                isExisting: true,
                isNew: false,
                isRestored: true
            });
            
            this.clearSearchInput();
            UIService.updateLinksDisplay(state);
            abp.notify.info(`Link to "${referenceNumber}" has been restored.`);
            
            setTimeout(() => ValidationService.validateAllLinks(state), 100);
        },

        addNewLink: function(referenceNumber, fullApp, state) {
            const linkType = $('#linkTypeSelect').val() || 'Related';
            const uniqueId = Date.now() + '_' + Math.random(); // NOSONAR - Safe: ID only used for client-side DOM element tracking, not for security
            
            const newLink = {
                id: uniqueId,
                referenceNumber: referenceNumber,
                projectName: fullApp.ProjectName || 'Unknown',
                applicantName: 'Loading...',
                category: 'Loading...',
                applicationStatus: 'Loading...',
                linkType: linkType,
                formVersion: null,
                isLoading: true,
                isNew: !this.isOriginalLink(referenceNumber, state),
                isExisting: this.isOriginalLink(referenceNumber, state),
                hasValidationError: false,
                validationErrorMessage: ''
            };
            
            state.currentLinks.unshift(newLink);
            
            if (state.validationErrors[referenceNumber]) {
                delete state.validationErrors[referenceNumber];
            }
            
            this.clearSearchInput();
            UIService.updateLinksDisplay(state);
            
            this.fetchLinkDetails(newLink, state);
        },

        isOriginalLink: function(referenceNumber, state) {
            return state.originalExistingLinks.some(original => original.referenceNumber === referenceNumber);
        },

        clearSearchInput: function() {
            $('#submissionSearch').val('');
            AutoSuggestService.removeAutoSuggest($('.search-input-container'));
        },

        fetchLinkDetails: function(newLink, state) {
            $.ajax({
                url: '/ApplicationLinks/ApplicationLinksModal?handler=ApplicationDetailsByReference',
                type: 'GET',
                data: { referenceNumber: newLink.referenceNumber },
                success: (response) => {
                    const linkToUpdate = state.currentLinks.find(link => link.id === newLink.id);
                    
                    if (linkToUpdate && linkToUpdate.referenceNumber === response.referenceNumber) {
                        linkToUpdate.applicantName = response.applicantName || 'Unknown';
                        linkToUpdate.category = response.category || 'Unknown';
                        linkToUpdate.applicationStatus = response.applicationStatus || 'Unknown';
                        linkToUpdate.formVersion = response.formVersion || null;
                        linkToUpdate.isLoading = false;
                        UIService.updateLinksDisplay(state);
                    }
                    
                    setTimeout(() => ValidationService.validateAllLinks(state), 100);
                },
                error: () => {
                    const linkToUpdate = state.currentLinks.find(link => link.id === newLink.id);
                    if (linkToUpdate) {
                        linkToUpdate.applicantName = 'Failed to load';
                        linkToUpdate.category = 'Failed to load';
                        linkToUpdate.applicationStatus = 'Unknown';
                        linkToUpdate.isLoading = false;
                        linkToUpdate.hasError = true;
                        UIService.updateLinksDisplay(state);
                    }
                },
                timeout: 5000
            });
        }
    };

    // Form Submission Service
    const FormSubmissionService = {
        setupFormSubmission: function(state) {
            $('#applicationLinksForm').off('submit').on('submit', (e) => {
                e.preventDefault();
                this.handleFormSubmission(state);
                return false;
            });
        },

        handleFormSubmission: function(state) {
            if (state.hasValidationErrors) {
                abp.notify.error('Please remove the highlighted links before saving. ' + state.VALIDATION_ERROR_MESSAGE);
                return;
            }
            
            const formData = this.prepareFormData(state);
            this.submitForm(formData);
        },

        prepareFormData: function(state) {
            const linksWithTypes = state.currentLinks.map(link => ({
                ReferenceNumber: link.referenceNumber,
                ProjectName: link.projectName,
                LinkType: link.linkType
            }));
            
            const formData = new FormData();
            formData.append('LinksWithTypes', JSON.stringify(linksWithTypes));
            formData.append('CurrentApplicationId', $('#CurrentApplicationId').val());
            formData.append('GrantApplicationsList', $('#GrantApplicationsList').val());
            formData.append('LinkedApplicationsList', $('#LinkedApplicationsList').val());
            
            const token = $('input[name="__RequestVerificationToken"]').val();
            if (token) {
                formData.append('__RequestVerificationToken', token);
            }
            
            return formData;
        },

        submitForm: function(formData) {
            $.ajax({
                url: '/ApplicationLinks/ApplicationLinksModal?handler=OnPostAsync',
                type: 'POST',
                data: formData,
                processData: false,
                contentType: false,
                success: () => {
                    applicationLinksModal.close();
                    abp.notify.success(
                        'The application links have been successfully updated.',
                        'Application Links'
                    );
                    dataTable.ajax.reload();
                },
                error: (xhr, status, error) => {
                    console.error('Error updating application links:', status, error);
                    abp.notify.error('Error updating application links: ' + error);
                }
            });
        }
    };

    // Main initialization function - Now with minimal complexity
    function initializeEnhancedModal() {
        // Initialize state
        LinkStateManager.initialize();
        const state = LinkStateManager.getState();
        
        // Display existing links
        UIService.updateLinksDisplay(state);
        
        // Set initial state
        state.hasChanges = false;
        state.hasValidationErrors = false;
        UIService.updateSaveButtonState(state);
        
        // Setup features
        AutoSuggestService.setupAutoSuggest(state);
        FormSubmissionService.setupFormSubmission(state);
        
        // Validate existing Parent/Child links on load
        const hasParentChildLinks = state.currentLinks.some(link => link.linkType !== 'Related');
        if (hasParentChildLinks) {
            ValidationService.validateAllLinks(state);
        }
    }
});