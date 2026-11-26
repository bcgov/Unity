$(function () {
    if ($('#AIPromptsTable').length === 0) {
        return;
    }

    const l = abp.localization.getResource('AI');

    // Prompt-level modals (create / edit prompt rows)
    let createModal = new abp.ModalManager(abp.appPath + 'Prompts/CreateModal');
    let editModal   = new abp.ModalManager(abp.appPath + 'Prompts/EditModal');

    // ── State ────────────────────────────────────────────────────────────────
    let currentVersionId  = null;
    let isNewVersion      = false;
    let cachedVersions    = [];   // versions for the currently-selected prompt

    // Escapes for both element text and quoted attribute values (" and ' included),
    // since results also go into title="..." and data-*="...".
    const escapeHtml = (value) => String(value ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#39;');

    // Every column has a fixed width (rem) and the previews are capped to match
    // (ai-prompt-preview-text in Index.css). If one column were left to fill the
    // rest, DataTables sizes it without the vertical scrollbar and the table runs
    // past the Actions column.
    const promptPreview = (data) => {
        const text = escapeHtml((data ?? '').slice(0, 80));
        return `<span class="ai-prompt-preview-text" title="${text}">${text}</span>`;
    };

    // ── Table columns ────────────────────────────────────────────────────────
    const listColumns = [
        {
            title: l('PromptName'),
            name: 'name',
            data: 'name',
            index: 0,
            width: '10rem',
            className: 'text-nowrap',
            render: (data) => escapeHtml(data)
        },
        {
            title: l('VersionNumber'),
            name: 'versionNumber',
            data: 'versionNumber',
            index: 1,
            width: '7rem',
            // Headers are left-aligned by the theme, so numbers are too.
            className: 'text-start text-nowrap'
        },
        {
            title: l('SystemPrompt'),
            name: 'systemPrompt',
            data: 'systemPrompt',
            index: 2,
            defaultContent: '',
            width: '22rem',
            render: promptPreview
        },
        {
            title: l('UserPrompt'),
            name: 'userPrompt',
            data: 'userPrompt',
            index: 3,
            defaultContent: '',
            width: '22rem',
            render: promptPreview
        },
        {
            title: l('PromptIsActive'),
            name: 'isActive',
            data: 'isActive',
            index: 4,
            width: '5rem',
            className: 'text-nowrap',
            render: (data) => data
                ? '<span class="badge bg-success">Active</span>'
                : '<span class="badge bg-secondary">Inactive</span>'
        },
        {
            title: l('Actions'),
            data: 'id',
            orderable: false,
            width: '7rem',
            className: 'text-center text-nowrap',
            name: 'rowActions',
            index: 5,
            rowAction: {
                items: [
                    {
                        text: l('AISetup:Edit'),
                        action: (data) => editModal.open({ id: data.record.id })
                    },
                    {
                        // The version in use (highest active) cannot be deleted.
                        text: l('AISetup:Delete'),
                        visible: (record) => !record.isInUse,
                        confirmMessage: (data) => l('PromptDeleteConfirm', data.record.name, data.record.versionNumber),
                        action: (data) => unity.aI.prompts.aIPrompt.delete(data.record.id)
                            .then(() => {
                                abp.notify.success(l('PromptDeleted', data.record.name, data.record.versionNumber));
                                dataTable.ajax.reload();
                            })
                    }
                ]
            }
        }
    ];

    const responseCallback = (result) => ({
        recordsTotal:    result.totalCount,
        recordsFiltered: result.items.length,
        data:            result.items
    });

    const actionButtons = [
        {
            text: '<i class="fa-solid fa-plus"></i> New Prompt',
            titleAttr: 'New Prompt',
            id: 'CreatePromptButton',
            className: 'btn btn-light rounded-1',
            action: (e) => { e.preventDefault(); createModal.open(); }
        }
    ];

    const defaultVisibleColumns = ['name', 'versionNumber', 'systemPrompt', 'userPrompt', 'isActive', 'rowActions'];
    const dt = $('#AIPromptsTable');

    const dataTable = initializeDataTable({
        dt,
        defaultVisibleColumns,
        listColumns,
        maxRowsPerPage: 25,
        defaultSortColumn: 0,
        dataEndpoint: unity.aI.prompts.aIPrompt.getList,
        data: {},
        responseCallback,
        actionButtons,
        pagingEnabled: true,
        reorderEnabled: false,
        languageSetValues: {},
        dataTableName: 'AIPromptsTable',
        dynamicButtonContainerId: $('#ai-prompts-dynamic-buttons').length ? 'ai-prompts-dynamic-buttons' : 'dynamicButtonContainerId',
        useNullPlaceholder: true,
        externalSearchId: 'search-prompts',
        fixedHeaders: true
    });

    createModal.onResult(() => dataTable.ajax.reload());
    editModal.onResult(()   => dataTable.ajax.reload());

    // ── Row click → open version panel ──────────────────────────────────────
    $('#AIPromptsTable').on('click', 'tbody tr', function (e) {
        // Don't intercept action-column clicks
        if ($(e.target).closest('.dropdown, .dropdown-menu, button, a').length) return;

        const rowData = dataTable.row(this).data();
        if (!rowData) return;

        // Highlight selected row
        $('#AIPromptsTable tbody tr').removeClass('prompt-selected');
        $(this).addClass('prompt-selected');

        openVersionPanel(rowData);
    });

    // ── Open / populate right panel ──────────────────────────────────────────
    function openVersionPanel(promptData) {
        $('#versionEditorTitle').text(promptData.name);
        $('#versionPromptId').val(promptData.id);

        // Activate split layout
        $('#promptsSplitContainer').addClass('split-active');
        $('#promptsRightPane').show();

        loadVersions(promptData.id);
    }

    function loadVersions(promptId) {
        unity.aI.prompts.aIPrompt.getByPrompt(promptId).then(function (result) {
            cachedVersions = result.items || [];
            const $select  = $('#versionSelect');
            $select.empty();

            if (cachedVersions.length === 0) {
                $select.append('<option value="">— no versions —</option>');
                clearVersionForm(0);
                return;
            }

            // Sort ascending by versionNumber in place so [last] is always the max
            cachedVersions.sort((a, b) => a.versionNumber - b.versionNumber);

            cachedVersions.forEach(function (v) {
                $select.append(`<option value="${v.id}">${v.versionNumber}</option>`);
            });

            // Select latest (highest versionNumber) by default
            const latest = cachedVersions[cachedVersions.length - 1];
            $select.val(latest.id);
            populateVersionForm(latest);
        });
    }

    // Version dropdown change
    $('#versionSelect').on('change', function () {
        const id = $(this).val();
        if (!id) return;
        const v = cachedVersions.find(x => x.id === id);
        if (v) {
            populateVersionForm(v);
        } else {
            // fallback: fetch from server
            unity.aI.prompts.aIPrompt.get(id).then(populateVersionForm);
        }
    });

    // ── Populate form from a version DTO ─────────────────────────────────────
    function populateVersionForm(v) {
        isNewVersion     = false;
        currentVersionId = v.id;

        $('#versionId').val(v.id);
        $('#versionNumber').val(v.versionNumber);
        $('#versionSystemPrompt').val(v.systemPrompt ?? '').removeClass('is-invalid');
        $('#versionUserPrompt').val(v.userPrompt ?? '').removeClass('is-invalid');
        $('#versionIsActive').prop('checked', v.isActive ?? true);

        // Pretty-print MetadataJson if valid
        let meta = v.metadataJson ?? '';
        if (meta) {
            try { meta = JSON.stringify(JSON.parse(meta), null, 2); } catch (e) { console.warn('MetadataJson is not valid JSON; displaying raw value.', e); }
        }
        $('#versionMetadataJson').val(meta);

        clearJsonError();
        $('#saveVersionBtnLabel').text('Save Version');
    }

    // ── Clear form for a brand-new version ───────────────────────────────────
    function clearVersionForm(nextVersionNumber) {
        isNewVersion     = true;
        currentVersionId = null;

        $('#versionId').val('');
        $('#versionSystemPrompt').val('');
        $('#versionUserPrompt').val('');
        $('#versionIsActive').prop('checked', true);
        $('#versionMetadataJson').val('');

        clearJsonError();
        $('#saveVersionBtnLabel').text('Create Version');
    }

    // ── New version button ────────────────────────────────────────────────────
    $('#newVersionBtn').on('click', function () {
        const maxNum = cachedVersions.reduce((max, v) => Math.max(max, v.versionNumber), -1);
        const next   = maxNum + 1;

        // Add a placeholder option
        $('#versionSelect option[data-new]').remove();
        $('#versionSelect').prepend(
            `<option value="" data-new="1" data-num="${next}" selected>${next} (new)</option>`
        );

        clearVersionForm(next);
    });

    // ── Save / create version ─────────────────────────────────────────────────
    $('#saveVersionBtn').on('click', function () {
        const promptId = $('#versionPromptId').val();
        if (!promptId) return;

        // Required-field validation
        const systemPrompt = $('#versionSystemPrompt').val().trim();
        const userPrompt   = $('#versionUserPrompt').val().trim();
        let valid = true;
        if (systemPrompt) {
            $('#versionSystemPrompt').removeClass('is-invalid');
        } else {
            $('#versionSystemPrompt').addClass('is-invalid');
            valid = false;
        }
        if (userPrompt) {
            $('#versionUserPrompt').removeClass('is-invalid');
        } else {
            $('#versionUserPrompt').addClass('is-invalid');
            valid = false;
        }
        if (!valid) {
            $('#versionSystemPrompt.is-invalid, #versionUserPrompt.is-invalid')[0]?.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            return;
        }

        const metaRaw = $('#versionMetadataJson').val().trim();
        if (metaRaw && !validateJson(metaRaw)) return;

        const dto = {
            promptId:     promptId,
            versionNumber: Number.parseInt($('#versionNumber').val()) || 0,
            systemPrompt:  systemPrompt,
            userPrompt:    userPrompt,
            metadataJson:  metaRaw || null,
            isActive:      $('#versionIsActive').is(':checked')
        };

        if (isNewVersion) {
            const newOpt = $('#versionSelect option[data-new]');
            dto.versionNumber = newOpt.length ? Number.parseInt(newOpt.data('num')) : 0;

            unity.aI.prompts.aIPrompt.create(dto)
                .then(function () {
                    abp.notify.success('Version created');
                    loadVersions(promptId);
                })
                .catch(function (err) {
                    abp.notify.error(err?.message || 'Failed to create version');
                });
        } else {
            unity.aI.prompts.aIPrompt.update(currentVersionId, dto)
                .then(function () {
                    abp.notify.success('Version saved');
                    loadVersions(promptId);
                })
                .catch(function (err) {
                    abp.notify.error(err?.message || 'Failed to save version');
                });
        }
    });

    // ── Format JSON button ────────────────────────────────────────────────────
    $('#formatJsonBtn').on('click', function () {
        const raw = $('#versionMetadataJson').val().trim();
        if (raw && validateJson(raw)) {
            $('#versionMetadataJson').val(JSON.stringify(JSON.parse(raw), null, 2));
        }
    });

    // ── JSON validation helper ────────────────────────────────────────────────
    function validateJson(str) {
        try {
            JSON.parse(str);
            clearJsonError();
            return true;
        } catch (e) {
            $('#jsonValidationMsg').text('Invalid JSON: ' + e.message).show();
            $('#versionMetadataJson').addClass('is-invalid');
            return false;
        }
    }

    function clearJsonError() {
        $('#jsonValidationMsg').hide().text('');
        $('#versionMetadataJson').removeClass('is-invalid');
    }

    $('#versionMetadataJson').on('input', function () {
        const raw = $(this).val().trim();
        if (raw) validateJson(raw); else clearJsonError();
    });

    // ── Draggable divider ─────────────────────────────────────────────────────
    const $divider   = $('#promptsDivider');
    const $leftPane  = $('#promptsLeftPane');
    const $rightPane = $('#promptsRightPane');
    const $container = $('#promptsSplitContainer');

    let isDragging     = false;
    let dragStartX     = 0;
    let dragStartLeft  = 0;

    $divider.on('mousedown', function (e) {
        isDragging    = true;
        dragStartX    = e.clientX;
        dragStartLeft = $leftPane.width();
        $divider.addClass('dragging');
        $('body').addClass('split-dragging');
        e.preventDefault();
    });

    $(document).on('mousemove.splitDrag', function (e) {
        if (!isDragging) return;

        const totalWidth  = $container.width();
        const dividerW    = $divider.outerWidth();
        const delta       = e.clientX - dragStartX;
        let   newLeft     = dragStartLeft + delta;
        const minLeft     = totalWidth * 0.2;
        const maxLeft     = totalWidth * 0.8 - dividerW;

        newLeft = Math.max(minLeft, Math.min(maxLeft, newLeft));

        const leftPct  = (newLeft / totalWidth * 100).toFixed(2);
        const rightPct = ((totalWidth - newLeft - dividerW) / totalWidth * 100).toFixed(2);

        $leftPane.css('flex', `0 0 ${leftPct}%`);
        $rightPane.css({ 'flex': 'none', 'width': rightPct + '%' });
    });

    $(document).on('mouseup.splitDrag', function () {
        if (isDragging) {
            isDragging = false;
            $divider.removeClass('dragging');
            $('body').removeClass('split-dragging');
        }
    });
});
