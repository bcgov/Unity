$(function () {
    let availableChefFieldsString = document.getElementById('availableChefsFields').value;
    let existingMappingString = document.getElementById('existingMapping').value;
    let intakeFieldsString = document.getElementById('intakeProperties').value;
    let chefsFormId = document.getElementById('chefsFormId').value;
    let formVersionId = document.getElementById('formVersionId').value;
    let intakeMapColumn = document.querySelector('#intake-map-available-fields-column');
    let worksheetMapColumn = document.querySelector('#worksheet-map-available-fields-column');
    let excludedIntakeMappings = new Set(['ConfirmationId', 'SubmissionId', 'SubmissionDate']);
    let dataTable;
    if (globalThis.toastr) { toastr.options.positionClass = 'toast-top-center'; }

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
        btnGenerateWorksheet: $('#btn-generate-worksheet'),
        btnGenerateScoresheet: $('#btn-generate-scoresheet'),
        worksheetReviewModal: $('#aiWorksheetReviewModal'),
        worksheetReviewFields: $('#aiWorksheetReviewFields'),
        worksheetReviewEmpty: $('#aiWorksheetReviewEmpty'),
        btnConfirmWorksheet: $('#btn-confirm-ai-worksheet'),
        btnCancelWorksheet: $('#btn-cancel-ai-worksheet'),
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
    }

    function setupTooltips() {
        $('[data-toggle="tooltip"]').tooltip({
            placement: 'top'
        });
    }

    function bindUIEvents() {
        UIElements.btnBack.on('click', handleBack);
        UIElements.btnSave.on('click', handleSave);
        UIElements.btnSaveMapping.on('click', handleSaveEditMapping);
        UIElements.btnSync.on('click', handleSync);
        UIElements.btnEdit.on('click', handleEdit);
        UIElements.btnGenerate.on('click', queueFormMapping);
        UIElements.btnGenerateWorksheet.on('click', queueFormWorksheet);
        UIElements.btnGenerateScoresheet.on('click', queueFormScoresheet);
        UIElements.btnConfirmWorksheet.on('click', confirmAiWorksheet);
        UIElements.btnCancelWorksheet.on('click', cancelAiWorksheet);
        UIElements.worksheetReviewModal.on('hidden.bs.modal', cancelAiWorksheetIfNeeded);
        UIElements.worksheetReviewFields.on('change', 'input[data-field-id]', updateAiWorksheetReviewCount);
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
        const $button = $(buttonElement);

        if (isAiWorksheetPending()) {
            loadAiWorksheetReview();
            return;
        }

        const existingHtml = $button.html();

        if ($button.prop('disabled')) {
            return;
        }

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
        abp.notify.success('', 'Worksheet generated. Review the suggested fields before confirming.');
        loadAiWorksheetReview();
    }

    function isAiWorksheetPending() {
        return UIElements.btnGenerateWorksheet.attr('data-ai-pending') === 'true';
    }

    function setAiWorksheetPending(isPending) {
        UIElements.btnGenerateWorksheet.attr('data-ai-pending', isPending ? 'true' : 'false');
        UIElements.btnGenerateWorksheet.find('span').last().text(isPending ? 'Review Worksheet' : 'Generate Worksheet');
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
                    abp.notify.error('', 'The pending AI worksheet is no longer available.');
                    return;
                }

                renderAiWorksheetReview(worksheet);
                UIElements.worksheetReviewModal.modal('show');
            })
            .fail(function () {
                abp.notify.error('', 'Unable to load the pending AI worksheet.');
            });
    }

    function renderAiWorksheetReview(worksheet) {
        UIElements.worksheetReviewFields.empty();

        let fieldCount = 0;
        (worksheet.sections || []).forEach(function (section) {
            const fields = section.fields || [];
            const $section = $('<section class="ai-worksheet-review__section"></section>');
            const $sectionHeader = $('<div class="ai-worksheet-review__section-header"></div>');
            $('<h6 class="ai-worksheet-review__section-title"></h6>').text(section.name).appendTo($sectionHeader);
            $('<span class="ai-worksheet-review__section-count"></span>')
                .text(`0 of ${fields.length} selected`)
                .appendTo($sectionHeader);
            $sectionHeader.appendTo($section);

            fields.forEach(function (field) {
                fieldCount++;
                const fieldId = `ai-worksheet-field-${field.id}`;
                const $row = $('<div class="ai-worksheet-review__field"></div>');
                $('<span class="ai-worksheet-review__field-name"></span>')
                    .attr('data-field-role', 'CHEFS')
                    .text(field.key || '—')
                    .appendTo($row);
                $('<i class="fa-solid fa-arrow-right ai-worksheet-review__arrow" aria-hidden="true"></i>').appendTo($row);
                $('<span class="ai-worksheet-review__field-name"></span>')
                    .attr('data-field-role', 'Unity')
                    .text(field.label || field.key || '—')
                    .appendTo($row);
                const $switch = $('<div class="ai-worksheet-review__switch"></div>');
                const $switchContainer = $('<div class="form-check unt-form-switch form-switch mb-0"></div>');
                $('<input class="form-check-input" type="checkbox" checked>')
                    .attr('id', fieldId)
                    .attr('data-field-id', field.id)
                    .attr('aria-label', `Include ${field.label || field.key || 'field'}`)
                    .appendTo($switchContainer);
                $switchContainer.appendTo($switch);
                $switch.appendTo($row);
                $row.appendTo($section);
            });

            $section.appendTo(UIElements.worksheetReviewFields);
        });

        UIElements.worksheetReviewFields.attr('data-worksheet-id', worksheet.worksheetId);
        UIElements.worksheetReviewEmpty.toggleClass('d-none', fieldCount > 0);
        updateAiWorksheetReviewCount();
    }

    function updateAiWorksheetReviewCount() {
        UIElements.worksheetReviewFields.find('.ai-worksheet-review__section').each(function () {
            const $section = $(this);
            const $sectionFields = $section.find('input[data-field-id]');
            const sectionSelectedCount = $sectionFields.filter(':checked').length;
            $section.find('.ai-worksheet-review__section-count')
                .text(`${sectionSelectedCount} of ${$sectionFields.length} selected`);
        });
    }

    function confirmAiWorksheet() {
        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        const worksheetId = UIElements.worksheetReviewFields.attr('data-worksheet-id');
        const selectedFieldIds = UIElements.worksheetReviewFields
            .find('input[data-field-id]:checked')
            .map(function () { return $(this).attr('data-field-id'); })
            .get();

        if (!validateGuid(formVersion) || !validateGuid(worksheetId)) {
            abp.notify.error('', 'Unable to confirm the AI worksheet because its identifiers are invalid.');
            return;
        }

        UIElements.btnConfirmWorksheet.prop('disabled', true);
        UIElements.btnCancelWorksheet.prop('disabled', true);

        abp.ajax({
            url: `/api/app/application-form-version/confirm-ai-worksheet?formVersionId=${encodeURIComponent(formVersion)}`,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ worksheetId, selectedFieldIds })
        })
            .done(function () {
                UIElements.worksheetReviewModal.data('ai-confirmed', true).modal('hide');
                setAiWorksheetPending(false);
                abp.notify.success('', 'Worksheet confirmed and published successfully. Reloading page.');
                setTimeout(function () { globalThis.location.reload(); }, 500);
            })
            .fail(function () {
                abp.notify.error('', 'Unable to confirm the AI worksheet.');
            })
            .always(function () {
                UIElements.btnConfirmWorksheet.prop('disabled', false);
                UIElements.btnCancelWorksheet.prop('disabled', false);
            });
    }

    function cancelAiWorksheet() {
        if (UIElements.worksheetReviewModal.data('ai-confirmed') === true) {
            return;
        }

        const formVersion = String(document.getElementById('formVersionId')?.value ?? '').trim();
        if (!validateGuid(formVersion) || !isAiWorksheetPending()) {
            return;
        }

        UIElements.worksheetReviewModal.data('ai-confirmed', true);
        abp.ajax({
            url: `/api/app/application-form-version/cancel-ai-worksheet?formVersionId=${encodeURIComponent(formVersion)}`,
            type: 'POST'
        })
            .done(function () {
                setAiWorksheetPending(false);
                abp.notify.success('', 'Pending AI worksheet discarded.');
            })
            .fail(function () {
                UIElements.worksheetReviewModal.data('ai-confirmed', false);
                abp.notify.error('', 'Unable to discard the pending AI worksheet.');
            });
    }

    function cancelAiWorksheetIfNeeded() {
        cancelAiWorksheet();
        UIElements.worksheetReviewModal.removeData('ai-confirmed');
    }

    function refreshScoresheetAfterGeneration() {
        abp.notify.success('', 'Scoresheet generated and assigned successfully. Reloading page.');
        setTimeout(function () {
            globalThis.location.reload();
        }, 500);
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

    function refreshMappingAfterGeneration(applicationId, formVersion = null) {
        const resolvedFormVersion = String(formVersion ?? document.getElementById('formVersionId')?.value ?? '').trim();
        if (!validateGuid(resolvedFormVersion)) {
            abp.notify.error('', 'Unable to refresh the generated mapping because the Form Version ID is invalid.');
            return;
        }

        abp.ajax({
            url: `/api/app/application-form-version/${encodeURIComponent(resolvedFormVersion)}`,
            type: 'GET'
        })
            .done(function (applicationFormVersionDto) {
                const availableChefsFields = applicationFormVersionDto?.availableChefsFields
                    ? JSON.parse(applicationFormVersionDto.availableChefsFields)
                    : [];

                $('#applicationFormVersionDtoString').val(JSON.stringify(applicationFormVersionDto ?? {}));
                $('#availableChefsFields').val(applicationFormVersionDto?.availableChefsFields ?? '');
                $('#existingMapping').val(applicationFormVersionDto?.submissionHeaderMapping ?? '');

                existingMappingString = applicationFormVersionDto?.submissionHeaderMapping ?? '';
                availableChefFieldsString = applicationFormVersionDto?.availableChefsFields ?? '';

                $(intakeMapColumn).empty();
                $(worksheetMapColumn).empty();
                dataTable.clear().draw();
                initializeIntakeMap(availableChefsFields);
                bindExistingMaps();

                abp.notify.success('', 'Form mapping generated and saved successfully.');
            })
            .fail(function () {
                abp.notify.error('', 'Form mapping generated, but the page could not refresh the saved mapping.');
            });
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
        $button.find('span').last().text(isAiWorksheetPending() ? 'Review Worksheet' : 'Generate Worksheet');
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
            $.parseJSON(jsonText);
            let mappingJsonStr = jsonText.replace(/\s+/g, ' ').replace(/(\r\n|\n|\r)/gm, "");
            UIElements.btnSaveMapping.prop('disabled', true);
            handleSaveMapping($.parseJSON(mappingJsonStr));
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
                console.log(err);
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

    // AI Configuration tab
    const btnSaveAIConfig = document.getElementById('btn-save-ai-config');
    const btnCancelAIConfig = document.getElementById('btn-cancel-ai-config');

    if (btnSaveAIConfig) {
        const aiFormId = document.getElementById('applicationFormId').value;
        const automaticCheckbox = document.getElementById('AutomaticallyGenerateAIAnalysis');
        const manualCheckbox = document.getElementById('ManuallyInitiateAIAnalysis');

        let lastSavedAIValues = {
            automaticallyGenerateAIAnalysis: automaticCheckbox ? automaticCheckbox.checked : false,
            manuallyInitiateAIAnalysis: manualCheckbox ? manualCheckbox.checked : false
        };

        btnSaveAIConfig.addEventListener('click', function () {
            btnSaveAIConfig.disabled = true;
            abp.ajax({
                url: `/api/app/application-form/${aiFormId}/ai-config`,
                type: 'PATCH',
                data: JSON.stringify({
                    automaticallyGenerateAIAnalysis: automaticCheckbox ? automaticCheckbox.checked : false,
                    manuallyInitiateAIAnalysis: manualCheckbox ? manualCheckbox.checked : false
                }),
                contentType: 'application/json'
            })
                .done(function () {
                    lastSavedAIValues = {
                        automaticallyGenerateAIAnalysis: automaticCheckbox ? automaticCheckbox.checked : false,
                        manuallyInitiateAIAnalysis: manualCheckbox ? manualCheckbox.checked : false
                    };
                    abp.notify.success('AI configuration saved successfully.');
                })
                .fail(function () {
                    abp.notify.error('Failed to save AI configuration.');
                })
                .always(function () {
                    btnSaveAIConfig.disabled = false;
                });
        });

        if (btnCancelAIConfig) {
            btnCancelAIConfig.addEventListener('click', function () {
                if (automaticCheckbox) automaticCheckbox.checked = lastSavedAIValues.automaticallyGenerateAIAnalysis;
                if (manualCheckbox) manualCheckbox.checked = lastSavedAIValues.manuallyInitiateAIAnalysis;
            });
        }
    }
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
