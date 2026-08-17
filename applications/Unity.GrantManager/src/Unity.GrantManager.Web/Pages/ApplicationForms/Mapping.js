$(function () {
    const aiL = abp.localization.getResource('AI');
    let availableChefFieldsString = document.getElementById('availableChefsFields').value;
    let existingMappingString = document.getElementById('existingMapping').value;
    let intakeFieldsString = document.getElementById('intakeProperties').value;
    let chefsFormId = document.getElementById('chefsFormId').value;
    let formVersionId = document.getElementById('formVersionId').value;
    let intakeMapColumn = document.querySelector('#intake-map-available-fields-column');
    let worksheetMapColumn = document.querySelector('#worksheet-map-available-fields-column');
    let excludedIntakeMappings = new Set(['ConfirmationId', 'SubmissionId', 'SubmissionDate']);
    let dataTable;

    let allowableTypes = new Set(['textarea',
        'orgbook',
        'textfield',
        'currency',
        'datetime',
        'checkbox',
        'select',
        'selectboxes',
        'radio',
        'phoneNumber',
        'email',
        'number',
        'time',
        'day',
        'hidden',
        'simpletextfield',
        'simpletextfieldadvanced',
        'simpletime',
        'simpletimeadvanced',
        'simplenumber',
        'simplenumberadvanced',
        'simplephonenumber',
        'simplephonenumberadvanced',
        'simpleselect',
        'simpleselectadvanced',
        'simpleday',
        'simpledayadvanced',
        'simpleemail',
        'simpleemailadvanced',
        'simpledatetime',
        'simpledatetimeadvanced',
        'simpleurladvanced',
        'simplecheckbox',
        'simpleradios',
        'simpleradioadvanced',
        'simplecheckboxes',
        'simplecheckboxadvanced',
        'simplecurrencyadvanced',
        'simpletextarea',
        'simpletextareaadvanced',
        'bcaddress',
        'datagrid']);

    const UIElements = {
        btnBack: $('#btn-back'),
        btnSave: $('#btn-save'),
        btnEdit: $('#btn-edit'),
        btnGenerate: $('#btn-generate'),
        btnReviewMapping: $('#btn-review-mapping'),
        btnGenerateWorksheet: $('#btn-generate-worksheet'),
        btnGenerateScoresheet: $('#btn-generate-scoresheet'),
        btnReviewScoresheet: $('#btn-review-scoresheet'),
        btnReviewWorksheet: $('#btn-review-worksheet'),
        scoresheetReviewModal: $('#aiScoresheetReviewModal'),
        scoresheetReviewFields: $('#aiScoresheetReviewFields'),
        scoresheetReviewEmpty: $('#aiScoresheetReviewEmpty'),
        scoresheetReviewSelectAll: $('#aiScoresheetReviewSelectAll'),
        scoresheetTitle: $('#aiScoresheetTitle'),
        btnCreateScoresheetDraft: $('#btn-create-ai-scoresheet-draft'),
        btnDiscardScoresheet: $('#btn-discard-ai-scoresheet'),
        btnPublishAssignWorksheets: $('#btn-publish-assign-worksheets'),
        btnGenerateFinalMapping: $('#btn-generate-final-mapping'),
        btnReviewFinalMapping: $('#btn-review-final-mapping'),
        btnRestartAiFlow: $('#btn-restart-ai-flow'),
        worksheetReviewModal: $('#aiWorksheetReviewModal'),
        mappingReviewModal: $('#aiMappingReviewModal'),
        mappingReviewFields: $('#aiMappingReviewFields'),
        mappingReviewEmpty: $('#aiMappingReviewEmpty'),
        mappingReviewSelectAll: $('#aiMappingReviewSelectAll'),
        btnAddMapping: $('#btn-add-ai-mapping'),
        btnReviewLaterMapping: $('#btn-review-later-ai-mapping'),
        btnDiscardMapping: $('#btn-discard-ai-mapping'),
        worksheetReviewFields: $('#aiWorksheetReviewFields'),
        worksheetReviewEmpty: $('#aiWorksheetReviewEmpty'),
        worksheetTitle: $('#aiWorksheetTitle'),
        btnCreateWorksheetDraft: $('#btn-create-ai-worksheet-draft'),
        btnDiscardWorksheet: $('#btn-discard-ai-worksheet'),
        btnSync: $('#btn-sync'),
        btnReset: $('#btn-reset'),
        btnClose: $('.btn-close'),
        btnSaveMapping: $('#btn-save-mapping'),
        btnCancel: $('#btn-cancel-mapping'),
        inputSearchBar: $('#search-bar'),
        selectVersionList: $('#applicationFormVersion'),
        editMappingModal: $('#editMappingModal'),
        uiConfigurationTab: $('#nav-ui-configuration'),
        mappingTab: $('#nav-mapping-tab'),
        customFieldsTab: $('#nav-worksheet-fields-tab'),
        intakeFieldsTab: $('#nav-intake-fields-tab'),
        refreshAvailableWorksheetsHidden: $('#refresh_available_worksheets')
    };

    const reviewConfigs = {
        mapping: {
            modal: UIElements.mappingReviewModal,
            fields: UIElements.mappingReviewFields,
            empty: UIElements.mappingReviewEmpty,
            primary: UIElements.btnAddMapping,
            later: UIElements.btnReviewLaterMapping,
            discard: UIElements.btnDiscardMapping,
            emptyMessage: 'No new mappings were suggested.',
            emptyPrimaryText: 'Continue to Worksheets',
            primaryText: 'Add selected to map',
            continueReview: function () {
                UIElements.mappingReviewModal.modal('hide');
                loadMappingReview(false);
            }
        },
        worksheet: {
            modal: UIElements.worksheetReviewModal,
            fields: UIElements.worksheetReviewFields,
            empty: UIElements.worksheetReviewEmpty,
            primary: UIElements.btnCreateWorksheetDraft,
            later: $('#btn-review-later-ai-worksheet'),
            discard: UIElements.btnDiscardWorksheet,
            emptyMessage: 'No additional worksheet fields were suggested.',
            emptyPrimaryText: 'Finish',
            primaryText: 'Create Draft',
            continueReview: function () {
                setAiWorksheetPending(false);
                UIElements.worksheetReviewModal.modal('hide');
                loadMappingReview(false);
            }
        },
        scoresheet: {
            modal: UIElements.scoresheetReviewModal,
            fields: UIElements.scoresheetReviewFields,
            empty: UIElements.scoresheetReviewEmpty,
            primary: UIElements.btnCreateScoresheetDraft,
            later: $('#btn-review-later-ai-scoresheet'),
            discard: UIElements.btnDiscardScoresheet,
            emptyMessage: 'No additional scoresheet questions were suggested.',
            emptyPrimaryText: 'Finish',
            primaryText: 'Add to Scoresheet',
            continueReview: function () {
                const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
                abp.ajax({
                    url: `/api/app/application-form-version/discard-ai-scoresheet-suggestions?formVersionId=${encodeURIComponent(formVersion)}`,
                    type: 'POST'
                }).done(function () {
                    setAiScoresheetPending(false);
                    UIElements.scoresheetReviewModal.modal('hide');
                    loadMappingReview(false);
                }).fail(function () {
                    reviewConfigs.scoresheet.primary.prop('disabled', false);
                    abp.notify.error('', 'Unable to finish the scoresheet review.');
                });
            }
        }
    };

    init();

    function init() {
        bindUIEvents();
        restoreActiveTab();
        dataTable = initializeApplicationFormsTable();
        let availableChefsFields = availableChefFieldsString ? JSON.parse(availableChefFieldsString) : []
        initializeIntakeMap(availableChefsFields);
        bindExistingMaps();
        setupTooltips();
        initializeUIConfiguration();
        loadMappingReview(false);
        loadAiScoresheetReview(false);
    }

    function setupTooltips() {
        $('[data-toggle="tooltip"]').tooltip({
            placement: 'top'
        });
    }

    function startWorksheetPhase(callback) {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        return abp.ajax({
            url: `/api/app/application-form-version/mapping-review-phase?formVersionId=${encodeURIComponent(formVersion)}&phase=WorksheetReview`,
            type: 'POST'
        }).done(callback).fail(function () {
            abp.notify.error('', 'Unable to start worksheet generation.');
        });
    }

    function bindUIEvents() {
        UIElements.btnBack.on('click', handleBack);
        UIElements.btnSave.on('click', handleSave);
        UIElements.btnSaveMapping.on('click', handleSaveEditMapping);
        UIElements.btnSync.on('click', handleSync);
        UIElements.btnEdit.on('click', handleEdit);
        UIElements.btnGenerate.on('click', queueFormMapping);
        UIElements.btnReviewMapping.on('click', function () {
            loadMappingReview(true);
        });
        UIElements.btnReviewFinalMapping.on('click', function () {
            loadMappingReview(true);
        });
        UIElements.btnGenerateWorksheet.on('click', queueFormWorksheet);
        UIElements.btnGenerateFinalMapping.on('click', finalizeMappingReview);
        UIElements.btnRestartAiFlow.on('click', restartAiFlow);
        UIElements.btnGenerateScoresheet.on('click', function () {
            if (UIElements.btnGenerateScoresheet.attr('data-ai-can-generate') !== 'true') {
                abp.notify.error('', aiL('AI:GenerateFormScoresheetPermissionRequired'));
                return;
            }

            queueFormScoresheet(this);
        });
        UIElements.btnReviewScoresheet.on('click', function () {
            loadAiScoresheetReview(true);
        });
        UIElements.btnCreateScoresheetDraft.on('click', function () {
            if (UIElements.btnCreateScoresheetDraft.attr('data-empty-confirmation') === 'true') {
                continueEmptyReview(reviewConfigs.scoresheet);
                return;
            }
            createAiScoresheetDraft();
        });
        UIElements.btnDiscardScoresheet.on('click', discardAiScoresheetSuggestions);
        UIElements.scoresheetReviewFields.on('change', 'input[data-question-id]', updateAiScoresheetReview);
        UIElements.scoresheetReviewFields.on('change', 'input[data-section-id]', toggleAiScoresheetSection);
        UIElements.scoresheetReviewSelectAll.on('change', toggleAiScoresheetReviewAll);
        UIElements.scoresheetTitle.on('input', updateAiScoresheetDraftButton);
        UIElements.btnReviewWorksheet.on('click', loadAiWorksheetReview);
        UIElements.btnAddMapping.on('click', function () {
            if (UIElements.btnAddMapping.attr('data-empty-confirmation') === 'true') {
                continueEmptyReview(reviewConfigs.mapping);
                return;
            }
            addSelectedMappingSuggestion();
        });
        UIElements.btnReviewLaterMapping.on('click', function () {
            UIElements.mappingReviewModal.modal('hide');
        });
        UIElements.btnDiscardMapping.on('click', discardMappingSuggestions);
        UIElements.mappingReviewFields.on('change', 'input[data-suggestion-id]', updateMappingReviewSelection);
        UIElements.mappingReviewSelectAll.on('change', toggleMappingReviewAll);
        UIElements.btnCreateWorksheetDraft.on('click', function () {
            if (UIElements.btnCreateWorksheetDraft.attr('data-empty-confirmation') === 'true') {
                continueEmptyReview(reviewConfigs.worksheet);
                return;
            }
            createAiWorksheetDraft();
        });
        UIElements.btnDiscardWorksheet.on('click', discardAiWorksheetSuggestions);
        UIElements.worksheetReviewFields.on('change', 'input[data-field-id]', updateAiWorksheetReview);
        $('#aiWorksheetReviewSelectAll').on('change', toggleAiWorksheetReviewAll);
        UIElements.worksheetTitle.on('input', updateAiWorksheetDraftButton);
        UIElements.btnReset.on('click', handleReset);
        UIElements.btnCancel.on('click', handleCancelMapping);
        UIElements.btnClose.on('click', handleCancelMapping);
        UIElements.inputSearchBar.on('keyup', handleSeearchBar);
        UIElements.selectVersionList.on('change', handleSelectVersion);
        UIElements.mappingTab.on('click', handleMappingTabClick);

        // Persist active tab to localStorage on switch
        $('#nav-tab').on('shown.bs.tab', 'button[data-bs-toggle="tab"]', function () {
            const formId = document.getElementById('applicationFormId')?.value;
            if (formId) {
                localStorage.setItem('mapping-active-tab:' + formId, this.id);
            }
        });
    }

    function restoreActiveTab() {
        const formId = document.getElementById('applicationFormId')?.value;
        if (!formId) return;
        const savedTabId = localStorage.getItem('mapping-active-tab:' + formId);
        if (!savedTabId) return;
        const tabEl = document.getElementById(savedTabId);
        if (tabEl) {
            bootstrap.Tab.getOrCreateInstance(tabEl).show();
        }
    }

    function initializeUIConfiguration() {
        const providerName = 'F';
        const providerKey = $('#applicationFormId').val();
        const providerKeyDisplayName = 'Test.Display.Name';

        $.ajax({
            url: abp.appPath + 'SettingManagement/ZoneManagement',
            type: 'GET',
            data: {
                providerName: providerName,
                providerKey: providerKey,
                providerKeyDisplayName: providerKeyDisplayName
            },
            success: function (response) {
                UIElements.uiConfigurationTab.html(response);
            },
            error: function () {
                abp.notify.error('Failed to load UI Configuration.');
            }
        });
    }

    function handleEdit() {
        $('#jsonText').val(prettyJson(existingMappingString));
        UIElements.editMappingModal.addClass('display-modal');
    }

    function queueFormMapping(triggerButton = null) {
        if (UIElements.btnGenerate.attr('data-ai-pending') === 'true') {
            loadMappingReview(true);
            return;
        }

        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        const applicationId = String(document.getElementById('applicationFormId')?.value ?? '').trim();
        if (!validateGuid(formVersion)) {
            abp.notify.error('', 'The Form Version ID is not in a GUID format');
            return;
        }
        if (!validateGuid(applicationId)) {
            abp.notify.error('', 'The Application ID is not in a GUID format');
            return;
        }

        const buttonElement = triggerButton?.currentTarget || triggerButton?.target || triggerButton || UIElements.btnGenerate?.get?.(0);
        const $button = $(buttonElement);
        const existingHtml = $button.html();

        if ($button.attr('data-ai-pending') === 'true') {
            loadAiScoresheetReview(true);
            return;
        }

        if ($button.prop('disabled')) {
            return;
        }

        globalThis.AIGenerationButtonState?.setGenerating($button);

        abp.ajax({
            url: `/api/app/ai/generation/form-mapping?applicationId=${encodeURIComponent(applicationId)}&applicationFormVersionId=${encodeURIComponent(formVersion)}`,
            type: 'POST',
        })
            .done(function (generationStatus) {
                const request = generationStatus?.generationRequest;
                const status = globalThis.AIGenerationButtonState?.resolveStatus(request?.status) ?? '';

                if (status === 'Completed') {
                    globalThis.AIGenerationButtonState?.restoreForCooldownCheck($button, existingHtml);
                    globalThis.AIGenerationButtonState?.applyStatusState(generationStatus);
                    refreshMappingAfterGeneration(applicationId, formVersion);
                    return;
                }

                monitorFormMappingGeneration(applicationId, $button, existingHtml);
            })
            .fail(function (error) {
                if (globalThis.AIGenerationButtonState?.handleQueueFailure(error)) {
                    return;
                }

                abp.message.error('Failed to queue AI mapping generation. Please try again.');
                restoreGenerateMappingButton($button, existingHtml);
                globalThis.syncAIRateLimitButtons?.();
            });
    }

    function queueFormWorksheet(triggerButton = null) {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        const applicationId = String(document.getElementById('applicationFormId')?.value ?? '').trim();
        if (!validateGuid(formVersion) || !validateGuid(applicationId)) {
            abp.notify.error('', 'The Form Version ID or Application ID is not in a GUID format');
            return;
        }

        const buttonElement = triggerButton?.currentTarget || triggerButton?.target || triggerButton || UIElements.btnGenerateWorksheet?.get?.(0);

        if (isAiWorksheetPending()) {
            loadAiWorksheetReview();
            return;
        }

        startWorksheetPhase(function () {
            queueFormWorksheetCore(buttonElement);
        });
    }

    function queueFormWorksheetCore(triggerButton = null) {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        const applicationId = String(document.getElementById('applicationFormId')?.value ?? '').trim();
        const buttonElement = triggerButton?.currentTarget || triggerButton?.target || triggerButton || UIElements.btnGenerateWorksheet?.get?.(0);
        const $button = $(buttonElement);
        const existingHtml = $button.html();

        globalThis.AIGenerationButtonState?.setGenerating($button);

        abp.ajax({
            url: `/api/app/ai/generation/form-worksheet?applicationId=${encodeURIComponent(applicationId)}&applicationFormVersionId=${encodeURIComponent(formVersion)}`,
            type: 'POST',
        })
            .done(function (generationStatus) {
                const request = generationStatus?.generationRequest;
                const status = globalThis.AIGenerationButtonState?.resolveStatus(request?.status) ?? '';
                if (status === 'Completed') {
                    globalThis.AIGenerationButtonState?.restoreForCooldownCheck($button, existingHtml);
                    globalThis.AIGenerationButtonState?.applyStatusState(generationStatus);
                    refreshWorksheetAfterGeneration();
                    return;
                }

                monitorFormWorksheetGeneration(applicationId, $button, existingHtml);
            })
            .fail(function (error) {
                if (globalThis.AIGenerationButtonState?.handleQueueFailure(error)) {
                    return;
                }

                abp.message.error('Failed to queue AI worksheet generation. Please try again.');
                restoreGenerateWorksheetButton($button, existingHtml);
                globalThis.syncAIRateLimitButtons?.();
            });
    }

    function monitorFormWorksheetGeneration(applicationId, $button, existingHtml) {
        globalThis.AIGenerationButtonState?.monitor({
            $button,
            originalHtml: existingHtml,
            getStatus: () => abp.ajax({
                url: `/api/app/ai/generation/status?applicationId=${encodeURIComponent(applicationId)}&operationType=form-worksheet`,
                type: 'GET'
            }),
            onComplete: function () {
                refreshWorksheetAfterGeneration();
            },
            onFailed: function (request) {
                abp.message.error(request?.failureReason || 'AI worksheet generation failed.');
            },
            onPollFailed: function () {
                abp.message.error('Unable to load AI worksheet generation status. Please try again.');
            }
        });
    }

    function queueFormScoresheet(triggerButton = null) {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        const applicationId = String(document.getElementById('applicationFormId')?.value ?? '').trim();
        if (!validateGuid(formVersion) || !validateGuid(applicationId)) {
            abp.notify.error('', 'The Form Version ID or Application ID is not in a GUID format');
            return;
        }

        const buttonElement = triggerButton?.currentTarget || triggerButton?.target || triggerButton || UIElements.btnGenerateScoresheet?.get?.(0);
        const $button = $(buttonElement);
        const existingHtml = $button.html();

        if ($button.prop('disabled')) {
            return;
        }

        globalThis.AIGenerationButtonState?.setGenerating($button);

        abp.ajax({
            url: `/api/app/ai/generation/form-scoresheet?applicationId=${encodeURIComponent(applicationId)}&applicationFormVersionId=${encodeURIComponent(formVersion)}`,
            type: 'POST',
        })
            .done(function (generationStatus) {
                const request = generationStatus?.generationRequest;
                const status = globalThis.AIGenerationButtonState?.resolveStatus(request?.status) ?? '';
                if (status === 'Completed') {
                    globalThis.AIGenerationButtonState?.restoreForCooldownCheck($button, existingHtml);
                    globalThis.AIGenerationButtonState?.applyStatusState(generationStatus);
                    refreshScoresheetAfterGeneration();
                    return;
                }

                monitorFormScoresheetGeneration(applicationId, $button, existingHtml);
            })
            .fail(function (error) {
                if (globalThis.AIGenerationButtonState?.handleQueueFailure(error)) {
                    return;
                }

                abp.message.error('Failed to queue AI scoresheet generation. Please try again.');
                restoreGenerateScoresheetButton($button, existingHtml);
                globalThis.syncAIRateLimitButtons?.();
            });
    }

    function monitorFormScoresheetGeneration(applicationId, $button, existingHtml) {
        globalThis.AIGenerationButtonState?.monitor({
            $button,
            originalHtml: existingHtml,
            getStatus: () => abp.ajax({
                url: `/api/app/ai/generation/status?applicationId=${encodeURIComponent(applicationId)}&operationType=form-scoresheet`,
                type: 'GET'
            }),
            onComplete: function () {
                refreshScoresheetAfterGeneration();
            },
            onFailed: function (request) {
                abp.message.error(request?.failureReason || 'AI scoresheet generation failed.');
            },
            onPollFailed: function () {
                abp.message.error('Unable to load AI scoresheet generation status. Please try again.');
            }
        });
    }

    function refreshWorksheetAfterGeneration() {
        setAiWorksheetPending(true);
        loadAiWorksheetReview();
    }

    function isAiWorksheetPending() {
        return UIElements.btnGenerateWorksheet.attr('data-ai-pending') === 'true';
    }

    function setAiWorksheetPending(isPending) {
        UIElements.btnGenerateWorksheet
            .attr('data-ai-pending', isPending ? 'true' : 'false')
            .toggleClass('d-none', isPending);
        UIElements.btnReviewWorksheet.toggleClass('d-none', !isPending);

        if (!isPending) {
            globalThis.syncAIRateLimitButtons?.();
        }
    }

    function loadAiWorksheetReview() {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        if (!validateGuid(formVersion)) {
            abp.notify.error('', 'Unable to review the worksheet because the Form Version ID is invalid.');
            return;
        }

        abp.ajax({
            url: `/api/app/application-form-version/pending-ai-worksheet?formVersionId=${encodeURIComponent(formVersion)}`,
            type: 'GET'
        })
            .done(function (worksheet) {
                if (!worksheet) {
                    setAiWorksheetPending(false);
                    loadMappingReview(false).done(function (review) {
                        if (review?.noWorksheetSuggestionsGenerated) {
                            showEmptyReviewConfirmation(reviewConfigs.worksheet);
                        } else {
                            abp.notify.error('', 'The pending AI worksheet is no longer available.');
                        }
                    });
                    return;
                }

                setAiWorksheetPending(true);
                renderAiWorksheetReview(worksheet);
                UIElements.worksheetReviewModal.modal('show');
            })
            .fail(function () {
                abp.notify.error('', 'Unable to load the pending AI worksheet.');
            });
    }

    function renderAiWorksheetReview(worksheet) {
        resetEmptyReviewModal(reviewConfigs.worksheet);
        UIElements.worksheetReviewFields.empty();

        const fields = worksheet.fields || [];
        fields.forEach(function (field) {
            const fieldId = `ai-worksheet-field-${field.id}`;
            const $row = $('<div class="ai-suggestion-review__field"></div>');
            $('<span class="ai-suggestion-review__field-name"></span>')
                .attr('data-field-role', 'Source')
                .text(field.key || '—')
                .appendTo($row);
            $('<i class="fa-solid fa-arrow-right ai-suggestion-review__arrow" aria-hidden="true"></i>').appendTo($row);
            $('<span class="ai-suggestion-review__field-name"></span>')
                .attr('data-field-role', 'Worksheet')
                .text(field.label || field.key || '—')
                .appendTo($row);
            const $switch = $('<div class="ai-suggestion-review__switch"></div>');
            const $switchContainer = $('<div class="form-check unt-form-switch form-switch mb-0"></div>');
            $('<input class="form-check-input" type="checkbox">')
                .attr('id', fieldId)
                .attr('data-field-id', field.id)
                .attr('aria-label', `Include ${field.label || field.key || 'field'}`)
                .prop('checked', field.selected !== false)
                .appendTo($switchContainer);
            $switchContainer.appendTo($switch);
            $switch.appendTo($row);
            $row.appendTo(UIElements.worksheetReviewFields);
        });

        UIElements.worksheetReviewFields.attr('data-session-id', worksheet.sessionId);
        UIElements.worksheetReviewEmpty.toggleClass('d-none', fields.length > 0);
        updateAiWorksheetReview();
    }

    function updateAiWorksheetReview() {
        const $fields = UIElements.worksheetReviewFields.find('input[data-field-id]');
        const selectedCount = $fields.filter(':checked').length;
        $('#aiWorksheetReviewSelectAll')
            .prop('checked', $fields.length > 0 && selectedCount === $fields.length)
            .prop('indeterminate', false);
        updateAiWorksheetDraftButton();
    }

    function toggleAiWorksheetReviewAll() {
        UIElements.worksheetReviewFields.find('input[data-field-id]').prop('checked', $(this).prop('checked'));
        updateAiWorksheetReview();
    }

    function updateAiWorksheetDraftButton() {
        const hasTitle = String(UIElements.worksheetTitle.val() ?? '').trim().length > 0;
        const hasSelectedFields = UIElements.worksheetReviewFields.find('input[data-field-id]:checked').length > 0;
        UIElements.btnCreateWorksheetDraft.prop('disabled', !hasTitle || !hasSelectedFields);
    }

    function createAiWorksheetDraft() {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        const sessionId = UIElements.worksheetReviewFields.attr('data-session-id');
        const title = String(UIElements.worksheetTitle.val() ?? '').trim();
        const selectedFieldIds = UIElements.worksheetReviewFields
            .find('input[data-field-id]:checked')
            .map(function () { return $(this).attr('data-field-id'); })
            .get();

        if (!validateGuid(formVersion) || !validateGuid(sessionId) || !title || selectedFieldIds.length === 0) {
            abp.notify.error('', 'Enter a worksheet title and select at least one suggested field.');
            return;
        }

        UIElements.btnCreateWorksheetDraft.prop('disabled', true);
        UIElements.btnDiscardWorksheet.prop('disabled', true);

        abp.ajax({
            url: `/api/app/application-form-version/create-ai-worksheet-draft?formVersionId=${encodeURIComponent(formVersion)}`,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ sessionId, title, selectedFieldIds })
        })
            .done(function () {
                UIElements.worksheetTitle.val('');
                abp.notify.success('', 'Draft worksheet created.');
                refreshAiWorksheetReviewAfterDraftCreation(formVersion);
            })
            .fail(function () {
                abp.notify.error('', 'Unable to create the draft worksheet.');
            })
            .always(function () {
                UIElements.btnDiscardWorksheet.prop('disabled', false);
                updateAiWorksheetDraftButton();
            });
    }

    function refreshAiWorksheetReviewAfterDraftCreation(formVersion) {
        abp.ajax({
            url: `/api/app/application-form-version/pending-ai-worksheet?formVersionId=${encodeURIComponent(formVersion)}`,
            type: 'GET'
        })
            .done(function (worksheet) {
                if (!worksheet) {
                    UIElements.worksheetReviewModal.modal('hide');
                    setAiWorksheetPending(false);
                    offerFinalMappingGeneration();
                    return;
                }

                renderAiWorksheetReview(worksheet);
            })
            .fail(function () {
                abp.notify.error('', 'Draft created, but the remaining suggestions could not be loaded.');
            });
    }

    function discardAiWorksheetSuggestions() {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        if (!validateGuid(formVersion) || !isAiWorksheetPending()) {
            return;
        }

        abp.message.confirm(
            'This will permanently remove the remaining AI field suggestions.',
            'Discard remaining suggestions?')
            .then(function (confirmed) {
                if (!confirmed) {
                    return;
                }

                UIElements.btnCreateWorksheetDraft.prop('disabled', true);
                UIElements.btnDiscardWorksheet.prop('disabled', true);
                abp.ajax({
                    url: `/api/app/application-form-version/discard-ai-worksheet-suggestions?formVersionId=${encodeURIComponent(formVersion)}`,
                    type: 'POST'
                })
                    .done(function () {
                        setAiWorksheetPending(false);
                        UIElements.worksheetReviewModal.modal('hide');
                        loadMappingReview(false);
                        loadAiScoresheetReview(false);
                    })
                    .fail(function () {
                        abp.notify.error('', 'Unable to discard the remaining AI worksheet suggestions.');
                    })
                    .always(function () {
                        UIElements.btnDiscardWorksheet.prop('disabled', false);
                        updateAiWorksheetDraftButton();
                    });
            });
    }

    function offerFinalMappingGeneration() {
        UIElements.mappingReviewModal.modal('hide');
        abp.notify.success('', 'Publish and assign the worksheet drafts, then return here to generate mapping.');
        loadMappingReview(false);
        loadAiScoresheetReview(false);
    }

    function finalizeMappingReview() {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        const applicationId = String(document.getElementById('applicationFormId')?.value ?? '').trim();
        abp.ajax({
            url: `/api/app/application-form-version/finalize-mapping-review?formVersionId=${encodeURIComponent(formVersion)}`,
            type: 'POST'
        }).done(function () {
            monitorFormMappingGeneration(applicationId, UIElements.btnGenerate, UIElements.btnGenerate.html());
        }).fail(function (error) {
            abp.notify.error('', error?.responseJSON?.error?.message || 'Publish and assign all AI worksheet drafts before generating mapping.');
            loadMappingReview(false);
        });
    }

    function checkMappingReviewComplete() {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        abp.ajax({
            url: `/api/app/application-form-version/mapping-review?formVersionId=${encodeURIComponent(formVersion)}`,
            type: 'GET'
        }).done(function (review) {
            if (!review?.pendingSuggestions?.length) {
                UIElements.mappingReviewModal.modal('hide');
                if (isFinalMappingPhase(review.phase)) {
                    completeMappingReview();
                } else {
                    offerWorksheetGeneration();
                }
            }
        });
    }

    function completeMappingReview() {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        abp.ajax({
            url: `/api/app/application-form-version/mapping-review-phase?formVersionId=${encodeURIComponent(formVersion)}&phase=Completed`,
            type: 'POST'
        }).done(function () {
            UIElements.btnGenerate.attr('data-ai-pending', 'false');
            abp.notify.success('', 'AI mapping review completed.');
        });
    }

    function isFinalMappingPhase(phase) {
        return phase === 'FinalMappingReview' || phase === 2 || phase === '2';
    }

    function refreshScoresheetAfterGeneration() {
        setAiScoresheetPending(true);
        loadAiScoresheetReview(true);
    }

    function setAiScoresheetPending(isPending) {
        UIElements.btnGenerateScoresheet.toggleClass('d-none', isPending);
        UIElements.btnReviewScoresheet.toggleClass('d-none', !isPending);

        if (isPending) {
            UIElements.btnGenerateScoresheet
                .removeAttr('data-ai-cooldown-checking data-ai-rate-limit-disabled')
                .prop('disabled', false);
        }
    }

    function loadAiScoresheetReview(showModal = true, showEmpty = true) {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        if (!validateGuid(formVersion)) {
            return;
        }

        return abp.ajax({
            url: `/api/app/application-form-version/pending-ai-scoresheet?formVersionId=${encodeURIComponent(formVersion)}`,
            type: 'GET'
        }).done(function (review) {
            if (!review) {
                setAiScoresheetPending(false);
                if (showModal && showEmpty) {
                    showEmptyReviewConfirmation(reviewConfigs.scoresheet);
                } else {
                    UIElements.scoresheetReviewModal.modal('hide');
                }
                return;
            }

            setAiScoresheetPending(true);
            renderAiScoresheetReview(review);
            if (showModal) {
                const hasQuestions = (review.sections || []).some(section => (section.questions || []).length > 0);
                if (hasQuestions) {
                    UIElements.scoresheetReviewModal.modal('show');
                } else if (showEmpty) {
                    showEmptyReviewConfirmation(reviewConfigs.scoresheet);
                } else {
                    UIElements.scoresheetReviewModal.modal('hide');
                }
            }
        }).fail(function () {
            if (showModal) {
                abp.notify.error('', 'Unable to load AI scoresheet suggestions.');
            }
        });
    }

    function renderAiScoresheetReview(review) {
        resetEmptyReviewModal(reviewConfigs.scoresheet);
        UIElements.scoresheetReviewFields.empty();
        UIElements.scoresheetReviewSelectAll.prop('checked', false);
        UIElements.scoresheetTitle.val(review.title || '');
        UIElements.scoresheetReviewFields.attr('data-session-id', review.sessionId || '');

        const sections = review.sections || [];
        UIElements.scoresheetReviewEmpty.toggle(sections.length === 0);
        sections.forEach(function (section) {
            const $section = $('<div/>', { class: 'ai-suggestion-review__section' });
            const $header = $('<div/>', { class: 'ai-suggestion-review__section-header' });
            $('<span/>', { text: section.name || 'Section' }).appendTo($header);
            const $sectionToggle = $('<div/>', { class: 'form-check unt-form-switch form-switch mb-0', title: 'Include all questions in this section' });
            $('<input/>', {
                type: 'checkbox',
                class: 'form-check-input',
                'data-section-id': section.id,
                'aria-label': `Include all questions in ${section.name || 'section'}`
            }).appendTo($sectionToggle);
            $sectionToggle.appendTo($header);
            $section.append($header);

            (section.questions || []).forEach(function (question) {
                const $row = $('<div/>', { class: 'ai-suggestion-review__field' });
                const $checkbox = $('<input/>', {
                    type: 'checkbox',
                    class: 'form-check-input',
                    'data-question-id': question.id,
                    'data-question-section-id': section.id,
                    checked: question.selected !== false
                });
                $('<div/>', { class: 'ai-suggestion-review__source', text: question.label || question.name || '' }).appendTo($row);
                if (!UIElements.scoresheetReviewModal.find('.ai-suggestion-review__panel').attr('data-hide-target-column')) {
                    $('<div/>', { class: 'ai-suggestion-review__arrow', text: '→' }).appendTo($row);
                }
                if (!UIElements.scoresheetReviewModal.find('.ai-suggestion-review__panel').attr('data-hide-target-column')) {
                    $('<div/>', { class: 'ai-suggestion-review__target', text: section.name || '' }).appendTo($row);
                }
                const $questionToggle = $('<div/>', { class: 'ai-suggestion-review__switch' });
                const $questionSwitch = $('<div/>', { class: 'form-check unt-form-switch form-switch mb-0' });
                $checkbox.appendTo($questionSwitch);
                $questionSwitch.appendTo($questionToggle);
                $questionToggle.appendTo($row);
                $section.append($row);
            });
            UIElements.scoresheetReviewFields.append($section);
        });

        updateAiScoresheetReview();
    }

    function updateAiScoresheetReview() {
        UIElements.scoresheetReviewFields.find('input[data-section-id]').each(function () {
            const sectionId = $(this).attr('data-section-id');
            const $questions = UIElements.scoresheetReviewFields.find('input[data-question-section-id]').filter(function () {
                return $(this).attr('data-question-section-id') === sectionId;
            });
            $(this).prop('checked', $questions.length > 0 && $questions.filter(':checked').length === $questions.length);
        });
        const $questions = UIElements.scoresheetReviewFields.find('input[data-question-id]');
        UIElements.scoresheetReviewSelectAll.prop('checked', $questions.length > 0 && $questions.filter(':checked').length === $questions.length);
        updateAiScoresheetDraftButton();
    }

    function toggleAiScoresheetSection() {
        const sectionId = $(this).attr('data-section-id');
        const checked = $(this).prop('checked');
        UIElements.scoresheetReviewFields.find('input[data-question-section-id]').filter(function () {
            return $(this).attr('data-question-section-id') === sectionId;
        }).prop('checked', checked);
        updateAiScoresheetReview();
    }

    function toggleAiScoresheetReviewAll() {
        UIElements.scoresheetReviewFields.find('input[data-question-id]').prop('checked', $(this).prop('checked'));
        updateAiScoresheetReview();
    }

    function updateAiScoresheetDraftButton() {
        const title = String(UIElements.scoresheetTitle.val() || '').trim();
        const selected = UIElements.scoresheetReviewFields.find('input[data-question-id]:checked').length;
        UIElements.btnCreateScoresheetDraft.prop('disabled', !title || selected === 0);
    }

    function createAiScoresheetDraft() {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        const sessionId = String(UIElements.scoresheetReviewFields.attr('data-session-id') || '').trim();
        const title = String(UIElements.scoresheetTitle.val() || '').trim();
        const selectedQuestionIds = UIElements.scoresheetReviewFields.find('input[data-question-id]:checked').map(function () {
            return $(this).attr('data-question-id');
        }).get();
        if (!validateGuid(formVersion) || !validateGuid(sessionId) || !title || selectedQuestionIds.length === 0) {
            updateAiScoresheetDraftButton();
            return;
        }

        UIElements.btnCreateScoresheetDraft.prop('disabled', true);
        abp.ajax({
            url: `/api/app/application-form-version/create-ai-scoresheet-draft?formVersionId=${encodeURIComponent(formVersion)}`,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ sessionId: sessionId, title: title, selectedQuestionIds: selectedQuestionIds })
        }).done(function () {
            abp.notify.success('', 'Scoresheet draft created.');
            loadAiScoresheetReview(true, false);
        }).fail(function (error) {
            abp.notify.error('', error?.responseJSON?.error?.message || 'Unable to create the scoresheet draft.');
            updateAiScoresheetDraftButton();
        });
    }

    function discardAiScoresheetSuggestions() {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        if (!validateGuid(formVersion)) {
            return;
        }
        abp.message.confirm('This will permanently remove the remaining AI scoresheet suggestions.', 'Discard remaining suggestions?')
            .then(function (confirmed) {
                if (!confirmed) return;
                UIElements.btnDiscardScoresheet.prop('disabled', true);
                abp.ajax({
                    url: `/api/app/application-form-version/discard-ai-scoresheet-suggestions?formVersionId=${encodeURIComponent(formVersion)}`,
                    type: 'POST'
                }).done(function () {
                    setAiScoresheetPending(false);
                    UIElements.scoresheetReviewModal.modal('hide');
                    abp.notify.success('', 'Remaining AI scoresheet suggestions discarded.');
                }).fail(function () {
                    abp.notify.error('', 'Unable to discard the remaining AI scoresheet suggestions.');
                }).always(function () {
                    UIElements.btnDiscardScoresheet.prop('disabled', false);
                });
            });
    }

    function monitorFormMappingGeneration(applicationId, $button, existingHtml) {
        globalThis.AIGenerationButtonState?.monitor({
            $button,
            originalHtml: existingHtml,
            getStatus: () => abp.ajax({
                url: `/api/app/ai/generation/status?applicationId=${encodeURIComponent(applicationId)}&operationType=form-mapping`,
                type: 'GET'
            }),
            onComplete: function () {
                refreshMappingAfterGeneration(applicationId);
            },
            onFailed: function (request) {
                abp.message.error(request?.failureReason || 'AI mapping generation failed.');
            },
            onPollFailed: function () {
                abp.message.error('Unable to load AI mapping generation status. Please try again.');
            }
        });
    }

    function loadMappingReview(showModal = true) {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        if (!validateGuid(formVersion)) {
            return;
        }

        return abp.ajax({
            url: `/api/app/application-form-version/mapping-review?formVersionId=${encodeURIComponent(formVersion)}`,
            type: 'GET'
        })
            .done(function (review) {
                setAiWorkflowReady();
                updateWorkflowActions(review);
                if (!review?.pendingSuggestions?.length) {
                    if (showModal && (review?.noSuggestionsGenerated ||
                        (review?.unchangedSuggestionCount > 0 && review?.state === 'Completed'))) {
                        showEmptyReviewConfirmation(reviewConfigs.mapping);
                        return;
                    }
                    if (review?.phase === 'PublishAndAssignWorksheets') {
                        UIElements.btnGenerate.prop('disabled', !review.canGenerateFinalMapping);
                    }
                    return;
                }

                renderMappingReview(review);
                if (showModal) {
                    UIElements.mappingReviewModal.modal('show');
                }
            }).fail(function () {
                setAiWorkflowReady();
            });
    }

    function setAiWorkflowReady() {
        $('.configuration-action-bar[data-ai-workflow-loading="true"]')
            .attr('data-ai-workflow-loading', 'false');
    }

    function updateWorkflowActions(review) {
        const action = getWorkflowAction(review);
        const state = getWorkflowState(review);
        const isInitial = action === 'GenerateInitialMapping';
        const isInitialReview = action === 'ReviewInitialMapping';
        const isGenerateWorksheets = action === 'GenerateWorksheets';
        const isReviewWorksheets = action === 'ReviewWorksheets';
        const isPublishAssign = action === 'PublishAndAssignWorksheets';
        const isFinalMapping = action === 'GenerateFinalMapping';
        const isFinalReview = action === 'ReviewFinalMapping';
        const isCompleted = state === 'Completed';

        UIElements.btnGenerate.toggleClass('d-none', !isInitial);
        UIElements.btnReviewMapping.toggleClass('d-none', !isInitialReview);
        UIElements.btnGenerateWorksheet.toggleClass('d-none', !isGenerateWorksheets);
        UIElements.btnReviewWorksheet.toggleClass('d-none', !isReviewWorksheets);
        UIElements.btnPublishAssignWorksheets.toggleClass('d-none', !isPublishAssign);
        UIElements.btnGenerateFinalMapping.toggleClass('d-none', !isFinalMapping);
        UIElements.btnReviewFinalMapping.toggleClass('d-none', !isFinalReview);
        UIElements.btnRestartAiFlow.toggleClass('d-none', !isCompleted);
        UIElements.btnGenerate.prop('disabled', !review?.actionEnabled && isInitial);
        UIElements.btnGenerateFinalMapping.prop('disabled', !review?.actionEnabled);

    }

    function getWorkflowState(review) {
        if (review?.state) {
            return review.state;
        }
        return getEnumName(review?.workflowState, {
            10: 'GenerateInitialMapping',
            20: 'ReviewInitialMapping',
            30: 'GenerateWorksheets',
            40: 'ReviewWorksheets',
            50: 'PublishAndAssignWorksheets',
            60: 'GenerateFinalMapping',
            70: 'ReviewFinalMapping',
            80: 'Completed'
        });
    }

    function getWorkflowAction(review) {
        if (review?.action) {
            return review.action;
        }
        return getEnumName(review?.workflowAction, {
            10: 'GenerateInitialMapping',
            20: 'ReviewInitialMapping',
            30: 'GenerateWorksheets',
            40: 'ReviewWorksheets',
            50: 'PublishAndAssignWorksheets',
            60: 'GenerateFinalMapping',
            70: 'ReviewFinalMapping',
            80: 'GenerateMapping',
            90: 'GenerateWorksheetsNextCycle'
        });
    }

    function getEnumName(value, numericNames) {
        if (typeof value === 'string') {
            return value;
        }
        return numericNames[String(value)] || '';
    }

    function renderMappingReview(review) {
        resetEmptyReviewModal(reviewConfigs.mapping);
        UIElements.mappingReviewFields.empty();
        (review.pendingSuggestions || []).forEach(function (suggestion) {
            const fieldId = `ai-mapping-field-${suggestion.id}`;
            const $row = $('<div class="ai-suggestion-review__field"></div>');
            $('<span class="ai-suggestion-review__field-name"></span>')
                .attr('data-field-role', 'CHEFS field')
                .text(suggestion.sourceField || '—')
                .appendTo($row);
            $('<i class="fa-solid fa-arrow-right ai-suggestion-review__arrow" aria-hidden="true"></i>').appendTo($row);
            const $target = $('<span class="ai-suggestion-review__field-name"></span>')
                .attr('data-field-role', 'Unity core field')
                .text(suggestion.changeType === 'Changed'
                    ? `${suggestion.previousTargetField || 'Unmapped'} → ${suggestion.targetField || '—'}`
                    : suggestion.targetField || '—')
                .appendTo($row);
            if (suggestion.conflictSourceField) {
                $('<small class="text-warning ai-suggestion-review__conflict d-block"></small>')
                    .text(` Replaces mapping from ${suggestion.conflictSourceField}`)
                    .appendTo($target);
            }
            const $switch = $('<div class="ai-suggestion-review__switch"></div>');
            const $switchContainer = $('<div class="form-check unt-form-switch form-switch mb-0"></div>');
            $('<input class="form-check-input" type="checkbox">')
                .attr('id', fieldId)
                .attr('data-suggestion-id', suggestion.id)
                .attr('aria-label', `Include ${suggestion.sourceField || 'CHEFS field'} mapping suggestion`)
                .prop('checked', false)
                .appendTo($switchContainer);
            $switchContainer.appendTo($switch);
            $switch.appendTo($row);
            $row.appendTo(UIElements.mappingReviewFields);
        });
        UIElements.mappingReviewEmpty.toggleClass('d-none', (review.pendingSuggestions || []).length > 0);
        UIElements.mappingReviewFields.attr('data-phase', review.phase || '');
        updateMappingReviewSelection();
    }

    function showEmptyReviewConfirmation(config) {
        config.fields.empty().attr('data-empty-confirmation', 'true');
        config.modal.attr('data-empty-confirmation', 'true');
        config.modal.find('.ai-suggestion-review').attr('data-empty-confirmation', 'true');
        config.empty.find('.ai-suggestion-review__empty-text').text(config.emptyMessage).removeClass('d-none');
        config.empty.removeClass('d-none');
        config.modal.find('.ai-suggestion-review__table-header').addClass('d-none');
        config.modal.find('.ai-suggestion-review__title-group').addClass('d-none');
        config.primary.attr('data-empty-confirmation', 'true')
            .text(config.emptyPrimaryText)
            .prop('disabled', false);
        config.later.add(config.discard).addClass('d-none');
        config.modal.modal('show');
    }

    function resetEmptyReviewModal(config) {
        config.fields.removeAttr('data-empty-confirmation');
        config.modal.removeAttr('data-empty-confirmation');
        config.modal.find('.ai-suggestion-review').removeAttr('data-empty-confirmation');
        config.empty.find('.ai-suggestion-review__empty-text').text('');
        config.empty.addClass('d-none');
        config.modal.find('.ai-suggestion-review__table-header').removeClass('d-none');
        config.modal.find('.ai-suggestion-review__title-group').removeClass('d-none');
        config.primary.removeAttr('data-empty-confirmation')
            .text(config.primaryText)
            .prop('disabled', true);
        config.later.add(config.discard).removeClass('d-none');
    }

    function continueEmptyReview(config) {
        config.primary.prop('disabled', true);
        config.continueReview();
    }

    function updateMappingReviewSelection() {
        const $suggestions = UIElements.mappingReviewFields.find('input[data-suggestion-id]');
        const selectedCount = $suggestions.filter(':checked').length;
        UIElements.mappingReviewSelectAll
            .prop('checked', $suggestions.length > 0 && selectedCount === $suggestions.length)
            .prop('indeterminate', false);
        UIElements.btnAddMapping.prop('disabled', selectedCount === 0);
    }

    function toggleMappingReviewAll() {
        UIElements.mappingReviewFields
            .find('input[data-suggestion-id]')
            .prop('checked', UIElements.mappingReviewSelectAll.prop('checked'));
        updateMappingReviewSelection();
    }

    async function addSelectedMappingSuggestion() {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        const suggestionIds = UIElements.mappingReviewFields
            .find('input[data-suggestion-id]:checked')
            .map(function () { return $(this).attr('data-suggestion-id'); })
            .get();

        if (!validateGuid(formVersion) || suggestionIds.length === 0) {
            return;
        }

        UIElements.btnAddMapping.prop('disabled', true);
        let result;
        try {
            result = await abp.ajax({
                url: `/api/app/application-form-version/accept-mapping-suggestions?formVersionId=${encodeURIComponent(formVersion)}`,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ suggestionIds })
            });
        } catch (error) {
            abp.notify.error(
                '',
                error?.responseJSON?.error?.message || 'Unable to add the selected mapping suggestions.');
            updateMappingReviewSelection();
            return;
        }

        existingMappingString = result.submissionHeaderMapping;
        $('#existingMapping').val(existingMappingString);
        handleReset();

        try {
            await loadMappingReview(true);
            checkMappingReviewComplete();
        } catch (error) {
            console.error('Unable to refresh mapping suggestions after they were added.', error);
        } finally {
            updateMappingReviewSelection();
        }
    }

    function discardMappingSuggestions() {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        abp.message.confirm('This will permanently remove the remaining AI mapping suggestions.', 'Discard remaining suggestions?')
            .then(function (confirmed) {
                if (!confirmed) {
                    return;
                }

                return abp.ajax({
                    url: `/api/app/application-form-version/discard-mapping-suggestions?formVersionId=${encodeURIComponent(formVersion)}`,
                    type: 'POST'
                })
                    .done(function () {
                        UIElements.mappingReviewModal.modal('hide');
                        if (isFinalMappingPhase(UIElements.mappingReviewFields.attr('data-phase'))) {
                            completeMappingReview();
                        } else {
                            offerWorksheetGeneration();
                        }
                    })
                    .fail(function () {
                        abp.notify.error('', 'Unable to discard the mapping suggestions.');
                    });
            });
    }

    function restartAiFlow() {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        if (!validateGuid(formVersion)) {
            return;
        }

        abp.message.confirm(
            'This permanently deletes AI workflow progress, AI-created worksheets and assignments, and all saved mappings for this form version.',
            'Restart AI Flow?')
            .then(function (confirmed) {
                if (!confirmed) {
                    return;
                }

                UIElements.btnRestartAiFlow.prop('disabled', true);
                return abp.ajax({
                    url: `/api/app/application-form-version/reset-ai-flow?formVersionId=${encodeURIComponent(formVersion)}`,
                    type: 'POST'
                }).done(function () {
                    globalThis.location.reload();
                }).fail(function (error) {
                    abp.notify.error('', error?.responseJSON?.error?.message || 'Unable to restart the AI flow.');
                }).always(function () {
                    UIElements.btnRestartAiFlow.prop('disabled', false);
                });
            });
    }

    function offerWorksheetGeneration() {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        if (!validateGuid(formVersion)) {
            return;
        }

        return abp.ajax({
            url: `/api/app/application-form-version/mapping-review-phase?formVersionId=${encodeURIComponent(formVersion)}&phase=WorksheetReview`,
            type: 'POST'
        }).done(function () {
            loadMappingReview(false);
        }).fail(function (error) {
            abp.notify.error('', error?.responseJSON?.error?.message || 'Unable to continue to worksheet generation.');
        });
    }

    function refreshMappingAfterGeneration(applicationId, formVersion = null) {
        const resolvedFormVersion = String(formVersion ?? document.getElementById('formVersionId')?.value ?? '').trim();
        if (!validateGuid(resolvedFormVersion)) {
            abp.notify.error('', 'Unable to refresh the generated mapping because the Form Version ID is invalid.');
            return;
        }

        loadMappingReview(true);
    }

    function restoreGenerateMappingButton($button, existingHtml) {
        if (!$button?.length) {
            return;
        }

        globalThis.AIGenerationButtonState?.restore($button);
        $button.html(existingHtml).prop('disabled', false);
        $button.find('span').last().text('Generate Mapping');
    }

    function restoreGenerateWorksheetButton($button, existingHtml) {
        if (!$button?.length) {
            return;
        }

        globalThis.AIGenerationButtonState?.restore($button);
        $button.html(existingHtml).prop('disabled', false);
    }

    function restoreGenerateScoresheetButton($button, existingHtml) {
        if (!$button?.length) {
            return;
        }

        globalThis.AIGenerationButtonState?.restore($button);
        $button.html(existingHtml).prop('disabled', false);
        $button.find('span').last().text('Generate Scoresheet');
    }

    function handleSaveEditMapping() {
        try {
            let jsonText = $('#jsonText').val();
            JSON.parse(jsonText);
            let mappingJsonStr = jsonText.replaceAll(/\s+/g, ' ').replaceAll(/(\r\n|\n|\r)/gm, "");
            UIElements.btnSaveMapping.prop('disabled', true);
            handleSaveMapping(JSON.parse(mappingJsonStr));
            handleCancelMapping();

            abp.notify.success(
                '',
                'Edit mapping save successful. Reloading page to new version'
            );

            setTimeout(function () {
                globalThis.location.href = location.href;
            }, 500);

        }
        catch (err) {
            UIElements.btnSaveMapping.prop('disabled', false);
            abp.notify.error(
                '',
                'The JSON is not valid:' + err
            );
        }
    }

    function handleCancelMapping() {
        UIElements.editMappingModal.removeClass('display-modal');
    }

    function handleSeearchBar(e) {
        let filterValue = e.currentTarget.value;
        let oTable = $('#ApplicationFormsTable').dataTable();
        oTable.fnFilter(filterValue);
    }

    function handleSelectVersion(e) {
        let chefsFormVersionGuid = e.currentTarget.value;
        navigateToVersion(chefsFormVersionGuid);
    }

    function navigateToVersion(chefsFormVersionGuid) {
        abp.notify.success(
            '',
            'Reloading page to new version'
        );

        setTimeout(function () {
            const url = new URL(globalThis.location.href);

            // If this really is a GUID, validate it defensively
            if (!/^[0-9a-fA-F-]{36}$/.test(chefsFormVersionGuid)) {
                abp.notify.error("The CHEFS Form Version ID is not in a GUID format");
                return; // or handle error
            }

            url.searchParams.set("ChefsFormVersionGuid", chefsFormVersionGuid);
            globalThis.location.href = url.toString();
        }, 500);
    }

    function bindExistingMaps() {
        if (existingMappingString + "" != "undefined" && existingMappingString != null && existingMappingString != "") {
            try {
                let existingMapping = JSON.parse(existingMappingString);
                let keys = Object.keys(existingMapping);
                for (let key of keys) {
                    let intakeProperty = key;
                    let chefsMappingProperty = existingMapping[intakeProperty];
                    let intakeMappingCard = document.getElementById("unity_" + intakeProperty);
                    let chefsMappingDiv = document.getElementById(chefsMappingProperty);
                    if (chefsMappingDiv != null && intakeMappingCard != null) {
                        chefsMappingDiv.appendChild(intakeMappingCard);
                    } else {
                        abp.notify.error(
                            '',
                            'Could not map existing: ' + chefsMappingProperty
                        );
                    }
                }
            } catch (err) {
                console.error('Unable to apply saved mapping.', err);
                abp.notify.error('', aiL('AI:SavedMappingApplyFailed'));
            }
        }
    }



    function handleSync() {
        let chefsFormVersionId = document.getElementById('chefsFormVersionId').value;
        if (!validateGuid(chefsFormVersionId)) {
            abp.notify.error(
                '',
                'The Form Version ID is not in a GUID format'
            );
            return;
        }

        if (chefsFormVersionId == "") {
            abp.notify.error(
                '',
                'ChefsFormVersionGuid is neeeded - Mapping Not Synchronized Successful'
            );

        } else {
            $.ajax(
                {
                    url: `/api/app/form/${chefsFormId}/version/${chefsFormVersionId}`,
                    type: "POST",
                    success: function (data) {
                        let formVersion = data.formVersion;
                        let updatedApplicationFormName = data.updatedFormName;
                        let updatedNameMessage = updatedApplicationFormName ? 'Form name updated to ' + updatedApplicationFormName : 'Form name is unchanged';
                        if (updatedApplicationFormName) {
                            document.getElementById('applicationFormName').textContent = updatedApplicationFormName;
                        }

                        let availableChefsFields = JSON.parse(formVersion.availableChefsFields)
                        document.getElementById('availableChefsFields').value = JSON.stringify(availableChefsFields);
                        initializeIntakeMap(availableChefsFields);

                        abp.notify.success(
                            '',
                            'Synchronized Successful' + updatedNameMessage
                        );
                        navigateToVersion(formVersion.chefsFormVersionGuid);
                    },
                    error: function () {
                        abp.notify.error(
                            '',
                            'Mapping Not Synchronized Successful'
                        );
                    }
                }
            );
        }
    }



    function handleSave() {
        let mappingDivs = $('.map-div');
        let mappingJson = {};

        for (let mappingDiv of mappingDivs) {
            let chefMappingDiv = mappingDiv;
            if (chefMappingDiv.childElementCount > 0) {

                let chefsKey = mappingDiv.id;
                let intakeMappingChildren = chefMappingDiv.children;

                for (let intakeMappingChild of intakeMappingChildren) {
                    mappingJson[intakeMappingChild.id.replace('unity_', '')] = chefsKey;
                }
            }
        }
        handleSaveMapping(mappingJson);
    }

    function handleSaveMapping(mappingJson) {
        let formData = JSON.parse(document.getElementById('applicationFormVersionDtoString').value);
        formData["submissionHeaderMapping"] = JSON.stringify(mappingJson);
        formData["availableChefsFields"] = document.getElementById('availableChefsFields').value;
        formData["ChefsApplicationFormGuid"] = document.getElementById('applicationFormId').value;

        UIElements.btnSave.prop('disabled', true);
        $.ajax(
            {
                url: "/api/app/application-form-version/" + formVersionId,
                data: JSON.stringify(formData),
                contentType: "application/json",
                type: "PUT",
                success: function (data) {
                    $('#existingMapping').val(data.submissionHeaderMapping);
                    existingMappingString = data.submissionHeaderMapping;
                    abp.notify.success(
                        data.responseText,
                        'Mapping Saved Successfully'
                    );
                },
                error: function (data) {
                    abp.notify.error(
                        data.responseText,
                        'Mapping Not Saved Successful'
                    );
                },
                complete: function () {
                    UIElements.btnSave.prop('disabled', false);
                }
            }
        );
    }

    function handleReset() {
        $(intakeMapColumn).empty();
        $(worksheetMapColumn).empty();
        let availableChefsFields = availableChefFieldsString ? JSON.parse(availableChefFieldsString) : []
        initializeIntakeMap(availableChefsFields);
        bindExistingMaps();
    }


    function createIntakeFieldCard(intakeField) {
        let intakeFieldJson = intakeField;
        let dragableDiv = document.createElement('div');
        dragableDiv.id = 'unity_' + intakeFieldJson.Name;
        dragableDiv.className = 'card mapping-field';
        dragableDiv.setAttribute("draggable", "true");

        // Set icon HTML (internal code, safe)
        dragableDiv.innerHTML = `${setTypeIndicator(intakeField)}`;

        // Append label as text node to prevent HTML injection
        dragableDiv.appendChild(document.createTextNode(intakeFieldJson.Label));

        // Append asterisk and route to the appropriate column based on custom status
        if (intakeFieldJson.IsCustom) {
            dragableDiv.appendChild(document.createTextNode(" *"));
            dragableDiv.className += ' custom-field';
            worksheetMapColumn.appendChild(dragableDiv);
        } else {
            intakeMapColumn.appendChild(dragableDiv);
        }
    }

    function buildAvailableChefsFieldsRows(availableChefsFields) {
        let rowsToAdd = [];
        for (let key of Object.keys(availableChefsFields)) {
            let jsonObj = JSON.parse(availableChefsFields[key]);
            if (allowableTypes.has(jsonObj.type.trim())) {
                rowsToAdd.push([stripHtml(jsonObj.label), key, jsonObj.type, key]);
            }
        }
        return rowsToAdd;
    }

    function initializeIntakeMap(availableChefsFields) {
        try {

            let intakeFields = JSON.parse(intakeFieldsString);

            for (let intakeField of intakeFields) {
                if (!excludedIntakeMappings.has(intakeField.Name)) {
                    createIntakeFieldCard(intakeField);
                }
            }

            dataTable.clear();

            let rowsToAdd = buildAvailableChefsFieldsRows(availableChefsFields);

            if (rowsToAdd.length > 0) {
                dataTable.rows.add(rowsToAdd);
            }
            dataTable.draw();
        }
        catch (err) {
            console.info('Mapping error: ' + err);
        }
    }


    document.addEventListener('dragstart', function (ev) {
        if (ev.target.classList.contains('non-drag')) {
            ev.preventDefault();
            return;
        } else if (ev.target.classList.contains('custom-field')) {
            UIElements.customFieldsTab.trigger('click');
        } else if (!ev.target.classList.contains('custom-field')) {
            UIElements.intakeFieldsTab.trigger('click');
        }
        beingDragged(ev);
    });

    document.addEventListener('dragend', function (ev) {
        if (ev.target.classList.contains('non-drag')) {
            ev.preventDefault();
            return;
        }
        dragEnd(ev);
    });

    document.addEventListener('dragover', function (event) {
        let beingDragged = document.querySelector('.dragging');
        if (event.target.matches('.card')) {
            if (beingDragged.classList.contains('card')) {
                allowDrop(event);
            }
        }
        if (event.target.matches('.col')) {
            if (beingDragged.classList.contains('card')) {
                colDraggedOver(event);
            }
            if (beingDragged.classList.contains('col')) {
                allowDrop(event);
            }
        }
    });



    function allowDrop(ev) {
        ev.preventDefault();

        let dragOver = ev.target;
        let dragOverParent = dragOver.parentElement;
        let beingDragged = document.querySelector('.dragging');
        let draggedParent = beingDragged.parentElement;

        let draggedIndex = whichChild(beingDragged);
        let dragOverIndex = whichChild(dragOver);

        if (draggedParent === dragOverParent) {
            if (draggedIndex < dragOverIndex) {
                beingDragged.before(dragOver);
            } else if (draggedIndex > dragOverIndex) {
                beingDragged.after(dragOver);
            }
        } else {
            dragOver.before(beingDragged);
        }
    }

    function colDraggedOver(event) {
        let dragOver = event.target;
        let beingDragged = document.querySelector('.dragging');
        let draggedParent = beingDragged.parentElement;
        if (
            draggedParent.id !== dragOver.id &&
            draggedParent.classList.contains('col') &&
            dragOver.classList.contains('col')
        ) {
            if (dragOver.childElementCount == 0) {
                dragOver.appendChild(beingDragged);
            }
        }
    }






    function handleMappingTabClick() {
        loadMappingReview(false);
        // Refresh the hidden field with the latest form version ID
        let refreshAvailableWorkSheets = UIElements.refreshAvailableWorksheetsHidden.val();
        if (refreshAvailableWorkSheets && refreshAvailableWorkSheets !== "undefined") {
            navigateToVersion(refreshAvailableWorkSheets);
        }
    }

    PubSub.subscribe(
        'refresh_available_worksheets',
        (_, data) => {
            UIElements.refreshAvailableWorksheetsHidden.val(data.chefsFormVersionId);
        }
    );


});


function handleBack() {
    location.href = '/ApplicationForms';
}

function beingDragged(ev) {
    let draggedEl = ev.target;
    if (draggedEl.classList + "" !== "undefined") {
        draggedEl.classList.add('dragging');
    }
}

function dragEnd(ev) {
    let draggedEl = ev.target;
    if (draggedEl.classList + "" !== "undefined") {
        draggedEl.classList.remove('dragging');
    }
}
