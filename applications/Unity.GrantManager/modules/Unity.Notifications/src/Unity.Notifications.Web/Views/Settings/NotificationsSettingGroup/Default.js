
$(function () {

    let dropdownItems = [];
    let emailAttachmentsTable = null;
    let templatesDataTable = null;
    let originalFormValues = {};

    const UiElements = {
        saveButton: $("#saveTemplateBtn"),
        discardButton: $("#discardTemaplateBtn"),
        deleteButton: $("#deleteTeamplateBtn"),
        closeButton: $("#closeTemplateBtn"),
        addTemplateButton: $("#CreateNewTemplate"),
        templatesTable: $('#TemplatesTable')
    }

    function init() {
        $('#email-attachments-section').hide();
        initializeTemplateDataTables();
        initializeDivider();
        initializeTabPersistence();
    }

    init();

    // ── Tab State Persistence ─────────────────────────────────────────────
    function initializeTabPersistence() {
        const tabContainer = $('#nav-tab');
        
        // Restore saved tab on page load
        const savedTabId = localStorage.getItem('notifications-active-tab');
        if (savedTabId) {
            const tabButton = tabContainer.find(`#${savedTabId}`);
            if (tabButton.length) {
                const tab = new bootstrap.Tab(tabButton[0]);
                tab.show();
            }
        }
        
        // Save tab selection when tab changes
        tabContainer.on('shown.bs.tab', 'button[data-bs-toggle="tab"]', function () {
            localStorage.setItem('notifications-active-tab', this.id);
        });
    }

    // ── Field validation helpers ──────────────────────────────────────────
    function checkTemplateNameUnique(name, currentId, callback) {
        $.ajax({
            url: `/api/app/template/template-by-name?name=${encodeURIComponent(name)}`,
            type: 'GET',
            success: function (response) {
                const isUnique = !response?.id || response.id === currentId;
                callback(isUnique);
            },
            error: function () {
                // If check fails, allow save to proceed
                callback(true);
            }
        });
    }

    function clearFieldError(fieldId) {
        $(`#${fieldId}`).removeClass('is-invalid');
        $(`#${fieldId}-error`).text('').hide();
    }

    function markFieldError(fieldId, errorMessage) {
        $(`#${fieldId}`).addClass('is-invalid');
        $(`#${fieldId}-error`).text(errorMessage).show();
    }

    function clearAllErrors() {
        $('#templateName, #sendFrom, #subject, #templateBody').removeClass('is-invalid');
        $('#templateName-error, #sendFrom-error, #subject-error, #templateBody-error').text('').hide();
    }

    // Listen for user input to clear error styling
    $('#templateName, #sendFrom, #subject').on('input', function () {
        clearFieldError(this.id);
    });

    $('#templateBody').on('input change', function () {
        clearFieldError(this.id);
    });

    // ── Open / close right panel ──────────────────────────────────────────
    function openRightPanel() {           
        // Activate split layout
        $('#splitContainer').addClass('split-active');
        $('#rightPane').show();
        if (templatesDataTable) {
            templatesDataTable.columns.adjust();
        }
    }

    function closeRightPanel() {
        // Deactivate split layout
        $('#splitContainer').removeClass('split-active');
        $('#rightPane').hide();
        $('#leftPane').css('flex', '');
        UiElements.deleteButton.hide();
        if (templatesDataTable) {
            templatesDataTable.columns.adjust();
        }
    }

    function populateFields(data) {
        // Store original values for discard functionality
        originalFormValues = {
            id: data.id,
            name: data.name,
            description: data.description || '',
            sendFrom: data.sendFrom,
            subject: data.subject,
            bodyText: data.bodyText || '',
            bodyHTML: data.bodyHTML
        };
        
        $('#templateId').val(data.id);
        $('#templateName').val(data.name);
        $('#sendFrom').val(data.sendFrom);
        $('#subject').val(data.subject);
        UiElements.deleteButton.show();
        $('#email-attachments-section').show();
        initEmailAttachmentsTable(data.id);
    }

    function populateFieldsForNewTemplate() {
        // Store original values as empty for new template
        originalFormValues = {
            id: '',
            name: '',
            description: '',
            sendFrom: '',
            subject: '',
            bodyText: '',
            bodyHTML: ''
        };
        
        $('#templateId').val('');
        $('#templateName').val('');
        $('#sendFrom').val('');
        $('#subject').val('');
        UiElements.deleteButton.hide();
        $('#email-attachments-section').hide();
        // Don't load attachments for new templates - they have no ID yet
    }

    UiElements.discardButton.on('click', function () {
        if (Object.keys(originalFormValues).length > 0) {
            $('#templateId').val(originalFormValues.id);
            $('#templateName').val(originalFormValues.name);
            $('#sendFrom').val(originalFormValues.sendFrom);
            $('#subject').val(originalFormValues.subject);
            const editor = tinymce.get('templateBody');
            if (editor) {
                editor.setContent(originalFormValues.bodyHTML || '');
            }
        }
    });

    UiElements.closeButton.on('click', function () {
        closeRightPanel();
    });

    UiElements.saveButton.on('click', function () {
        const templateId = $('#templateId').val();
        const templateName = $('#templateName').val();
        const sendFrom = $('#sendFrom').val();
        const subject = $('#subject').val();
        const editor = tinymce.get('templateBody');
        const bodyHTML = editor ? editor.getContent().trim() : '';

        // Clear all previous error styles
        clearAllErrors();

        // Collect all validation errors
        const validationErrors = [];

        if (!templateName?.trim()) {
            validationErrors.push('Template name is required.');
            markFieldError('templateName', 'Template name is required.');
        }
        if (!sendFrom?.trim()) {
            validationErrors.push('Send From is required.');
            markFieldError('sendFrom', 'Send From is required.');
        } else if (!isValidEmail(sendFrom.trim())) {
            validationErrors.push('Send From must be a valid email address.');
            markFieldError('sendFrom', 'Send From must be a valid email address.');
        }
        if (!subject?.trim()) {
            validationErrors.push('Subject is required.');
            markFieldError('subject', 'Subject is required.');
        }
        if (!bodyHTML) {
            validationErrors.push('Template body is required.');
            markFieldError('templateBody', 'Template body is required.');
        }

        // Show single error popup if there are validation errors
        if (validationErrors.length > 0) {
            return abp.notify.error(validationErrors.join('<br>'));
        }

        const templateData = {
            name: templateName,
            description: '',
            sendFrom: sendFrom,
            subject: subject,
            bodyText: '',
            bodyHTML: bodyHTML
        };

        const isNewTemplate = !templateId || templateId.trim() === '';

        // Check template name uniqueness before saving
        checkTemplateNameUnique(templateName.trim(), templateId, function (isUnique) {
            if (!isUnique) {
                markFieldError('templateName', 'Template name must be unique.');
                return;
            }
            performSave(isNewTemplate, templateId, templateData, templateName, sendFrom, subject, bodyHTML);
        });
    });

    function performSave(isNewTemplate, templateId, templateData, templateName, sendFrom, subject, bodyHTML) {
        if (isNewTemplate) {
            // Create new template
            unity.notifications.templates.template
                .create(templateData)
                .then(function (response) {
                    abp.notify.success('Template created successfully.');
                    // Populate templateId with returned ID
                    $('#templateId').val(response.id);
                    // Update original values with new template ID and data
                    originalFormValues = {
                        id: response.id,
                        name: templateName,
                        description: '',
                        sendFrom: sendFrom,
                        subject: subject,
                        bodyText: '',
                        bodyHTML: bodyHTML
                    };
                    // Now show attachments section and initialize table
                    $('#email-attachments-section').show();
                    initEmailAttachmentsTable(response.id);
                    UiElements.deleteButton.show();
                    // Reload templates list
                    PubSub.publish('reload_templates_table_no_close');
                })
                .catch(function (e) {
                    console.warn('Failed to create template:', e);
                    abp.notify.error('Failed to create template.');
                });
        } else {
            // Update existing template
            unity.notifications.templates.template
                .updateTemplate(templateId, templateData)
                .then(function () {
                    abp.notify.success('Template updated successfully.');
                    // Update original values after successful save
                    originalFormValues = {
                        id: templateId,
                        name: templateName,
                        description: '',
                        sendFrom: sendFrom,
                        subject: subject,
                        bodyText: '',
                        bodyHTML: bodyHTML
                    };
                    PubSub.publish('reload_templates_table_no_close');
                })
                .catch(function (e) {
                    console.warn('Failed to update template:', e);
                    abp.notify.error('Failed to update template.');
                });
        }
    }

    UiElements.addTemplateButton.on('click', function () {
        // Initialize template variables if needed
        initializeTemplateVariables();
        
        // Initialize editor with empty data
        initializeEditor({ bodyHTML: '' }, dropdownItems);
        
        // Populate fields with empty values for new template
        populateFieldsForNewTemplate();
        
        // Highlight nothing in the table
        $('#TemplatesTable tbody tr').removeClass('template-selected');
        
        // Open right panel
        openRightPanel();
    });

    function proceedWithDelete(templateId, onDelete) {
        // Destroy attachments table before deleting
        if (emailAttachmentsTable) {
            emailAttachmentsTable.destroy();
            emailAttachmentsTable = null;
        }
        unity.notifications.templates.template
            .deleteTemplate(templateId)
            .then(function () {
                abp.notify.success('Template deleted successfully.');
                onDelete();
            })
            .catch(function (e) {
                console.warn('Failed to delete template:', e);
                abp.notify.error('Failed to delete template.');
            });
    }

    UiElements.deleteButton.on('click', function () {
        let templateId = $('#templateId').val();
        abp.message.confirm(
            'Are you sure you want to delete this template?',
            'Delete Template',
            function (confirmed) {
                if (confirmed) {
                    $.ajax({
                        url: `/api/form-notifications/can-delete-template/${templateId}`,
                        type: 'GET',
                        success: function (response) {
                            if (response.canDelete) {
                                proceedWithDelete(templateId, function () {
                                    PubSub.publish('reload_templates_table_with_close');
                                });
                            } else {
                                abp.notify.error(response.errorMessage || 'This template cannot be deleted because it is currently in use.');
                            }
                        },
                        error: function () {
                            // If check fails, proceed with deletion
                            proceedWithDelete(templateId, function () {
                                PubSub.publish('reload_templates_table_with_close');
                            });
                        }
                    });
                }
            }
        );

    });

    function initializeTemplateDataTables() {                
        // ── Table columns ────────────────────────────────────────────────────────
        const listColumns = [
            {
                title: 'Id',
                name: 'id',
                data: 'id',
                visible: false,
                index: 0
            },        
            {
                title: 'Name',
                name: 'name',
                data: 'name',
                index: 1
            },
            {
                title: 'Subject',
                name: 'subject',
                data: 'subject',
                index: 2
            },
            {
                title: 'Actions',
                name: 'actions',
                data: 'id',
                orderable: false,
                searchable: false,
                index: 3,
                width: '210px',
                render: function (data, type, row) {
                    return `
                        <div class="d-inline-flex gap-2">
                            <button class="btn btn-sm btn-action-gray template-edit-btn" data-id="${data}" title="Edit">
                                <i class="fl fl-edit"></i>
                            </button>
                            <button class="btn btn-sm btn-action-gray template-delete-btn" data-id="${data}" title="Delete">
                                <i class="fl fl-cancel"></i>
                            </button>
                        </div>
                    `;
                }
            }       
        ];

        const responseCallback = (result) => (        
            {        
                recordsTotal:    result.length,
                recordsFiltered: result.length,
                data:            result
            }
        );

        const actionButtons = [
            {
            }
        ];

        const defaultVisibleColumns = ['name', 'subject', 'actions'];
        const dt = $('#TemplatesTable');

        templatesDataTable = initializeDataTable({
            dt,
            defaultVisibleColumns,
            listColumns,
            maxRowsPerPage: 25,
            defaultSortColumn: 0,
            dataEndpoint: unity.notifications.templates.template.getTemplatesByTenant,
            data: {},
            responseCallback,
            actionButtons,
            pagingEnabled: true,
            reorderEnabled: false,
            languageSetValues: {},
            dataTableName: 'TemplatesTable',
            dynamicButtonContainerId: 'dynamicButtonContainerId',
            useNullPlaceholder: true,
            externalSearchId: 'search-prompts',
            fixedHeaders: true
        });

        // ── Row click → open version panel ──────────────────────────────────────
        $('#TemplatesTable').on('click', 'tbody tr', function (e) {
            // Don't intercept action-column clicks
            if ($(e.target).closest('.dropdown, .dropdown-menu, button, a').length) return;

            const rowData = templatesDataTable.row(this).data();
            if (!rowData) return;

            // Initialize the tinymce editor with the selected template data
            initializeTemplateVariables();
            initializeEditor(rowData, dropdownItems);
            populateFields(rowData);

            // Highlight selected row
            $('#TemplatesTable tbody tr').removeClass('template-selected');
            $(this).addClass('template-selected');            
            openRightPanel();
        });

        // ── Edit button click ──────────────────────────────────────────────────
        $('#TemplatesTable').on('click', '.template-edit-btn', function (e) {
            e.stopPropagation();
            const templateId = $(this).data('id');
            const rowData = templatesDataTable.row($(this).closest('tr')).data();
            
            if (!rowData) return;
            
            initializeTemplateVariables();
            initializeEditor(rowData, dropdownItems);
            populateFields(rowData);
            
            // Highlight selected row
            $('#TemplatesTable tbody tr').removeClass('template-selected');
            $(this).closest('tr').addClass('template-selected');
            openRightPanel();
        });

        // ── Delete button click ────────────────────────────────────────────────
        $('#TemplatesTable').on('click', '.template-delete-btn', function (e) {
            e.stopPropagation();
            const templateId = $(this).data('id');
            
            abp.message.confirm(
                'Are you sure you want to delete this template?',
                'Delete Template',
                function (confirmed) {
                    if (confirmed) {
                        $.ajax({
                            url: `/api/form-notifications/can-delete-template/${templateId}`,
                            type: 'GET',
                            success: function (response) {
                                if (response.canDelete) {
                                    proceedWithDelete(templateId, function () {
                                        PubSub.publish('reload_templates_table_with_close');
                                    });
                                } else {
                                    abp.notify.error(response.errorMessage || 'This template cannot be deleted because it is currently in use.');
                                }
                            },
                            error: function () {
                                // If check fails, proceed with deletion
                                proceedWithDelete(templateId, function () {
                                    PubSub.publish('reload_templates_table_with_close');
                                });
                            }
                        });
                    }
                }
            );
        });

        // Removed nested functions - now defined at global scope above
    }


    function initializeTemplateVariables() {
        $.ajax({
            url: `/api/app/template/template-variables`,
            type: 'GET',
            success: function (response) {
                $.map(response, function (item) {
                    dropdownItems.push({
                        text: item.name,
                        value: item.token
                    });
                });
            },
            error: function () {
                // Handle error silently
            }
        });
    }

    function initEmailAttachmentsTable(templateId) {
        // Destroy existing table if it exists
        if (emailAttachmentsTable) {
            emailAttachmentsTable.destroy();
            emailAttachmentsTable = null;
        }

        // Skip initialization if no template ID provided
        if (!templateId) {
            return;
        }

        emailAttachmentsTable = $('#EmailAttachmentsTable').DataTable(
            abp.libs.datatables.normalizeConfiguration({
                serverSide: false,
                order: [[2, 'asc']],
                searching: false,
                paging: false,
                select: false,
                info: false,
                scrollX: true,
                scrollY: '200px',
                scrollCollapse: true,
                ajax: abp.libs.datatables.createAjax(
                    unity.notifications.emails.emailLogAttachment.getListByTemplateId,
                    function () { return templateId; },
                    function (result) { return { data: result }; }
                ),
                columnDefs: [
                    {
                        title: '<i class="fl fl-paperclip"></i>',
                        width: '40px',
                        className: 'text-center',
                        orderable: false,
                        render: function () {
                            return '<i class="fl fl-paperclip"></i>';
                        }
                    },
                    {
                        title: 'Document Name',
                        data: 'fileName',
                        className: 'data-table-header text-break',
                        width: '40%'
                    },
                    {
                        title: 'Date',
                        data: 'time',
                        className: 'data-table-header',
                        width: '130px',
                        render: function (data, type) {
                            if (type === 'display' || type === 'filter') {
                                return new Date(data).toDateString();
                            }
                            return data;
                        }
                    },
                    {
                        title: 'Attached by',
                        data: 'attachedBy',
                        className: 'data-table-header',
                        width: '25%'
                    },
                    {
                        title: 'File Size',
                        data: 'fileSize',
                        className: 'data-table-header',
                        width: '90px',
                        render: function (data) {
                            if (!data) return '—';
                            const mb = data * 0.000001;
                            return mb >= 1 ? mb.toFixed(2) + ' MB' : (data / 1024).toFixed(0) + ' KB';
                        }
                    },
                    {
                        title: '',
                        data: 'id',
                        width: '80px',
                        className: 'text-center',
                        orderable: false,
                        render: function (data) {
                            return generateEmailAttachmentButtonContent(data);
                        }
                    }
                ],
                drawCallback: function () {
                }
            })
        );
    }


    $('#email_attachment_upload_btn').on('click', function () {
        $('#email_attachment_upload').click();
    });

    $('#email_attachment_upload').on('change', function () {
        uploadEmailFiles('email_attachment_upload');
    });

    function uploadEmailFiles(inputId) {
        const templateId = $('#templateId').val();
        const input = document.getElementById(inputId);
        if (!input?.files?.length) return;

        const disallowedTypes = JSON.parse(decodeURIComponent($('#Extensions').val()));
        const maxFileSize = decodeURIComponent($('#EmailAttachmentMaxFileSize').val());

        let isAllowedTypeError = false;
        let isMaxFileSizeError = false;
        const formData = new FormData();

        for (let file of input.files) {
            const ext = file.name.slice(file.name.lastIndexOf('.') + 1).toLowerCase();
            if (disallowedTypes.includes(ext)) {
                isAllowedTypeError = true;
            }
            if (file.size * 0.000001 > maxFileSize) {
                isMaxFileSizeError = true;
            }
            formData.append('files', file);
        }

        if (isAllowedTypeError) {
            input.value = '';
            return abp.notify.error('Error', 'File type not supported');
        }
        if (isMaxFileSizeError) {
            input.value = '';
            return abp.notify.error(
                'File Too Large',
                'The selected file exceeds the maximum allowed size of ' + maxFileSize + ' MB for email attachments. Please select a smaller file.'
            );
        }

        const totalMaxFileSize = Number.parseFloat(
            decodeURIComponent($('#TotalEmailAttachmentMaxFileSize').val()) || '25'
        );
        let existingTotalBytes = 0;
        if (emailAttachmentsTable) {
            emailAttachmentsTable.rows().data().each(function (row) {
                existingTotalBytes += (row.fileSize || 0);
            });
        }
        let newFilesBytes = 0;
        for (let file of input.files) {
            newFilesBytes += file.size;
        }
        const combinedMB = (existingTotalBytes + newFilesBytes) * 0.000001;
        if (combinedMB > totalMaxFileSize) {
            input.value = '';
            return abp.notify.error(
                'Total Size Exceeded',
                'The total size of all attachments would exceed the maximum allowed ' + totalMaxFileSize +
                ' MB. Please remove existing attachments or select a smaller file.'
            );
        }

        $.ajax({
            url: `/api/app/attachment/template/${templateId}/upload`,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            xhr: function () {
                const xhr = new globalThis.XMLHttpRequest();
                xhr.upload.addEventListener('progress', function (e) {
                    if (e.lengthComputable) {
                        const pct = Math.round((e.loaded / e.total) * 100);
                        $('#attachment-upload-progress-bar')
                            .css('width', pct + '%')
                            .attr('value', pct)
                            .text(pct + '%');
                    }
                });
                return xhr;
            },
            beforeSend: function () {
                $('#email_attachment_upload_btn')
                    .html('<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>Uploading...')
                    .prop('disabled', true);
                $('#attachment-upload-progress-bar').css('width', '0%').text('0%');
                $('#attachment-upload-progress').show();
            },
            success: function () {
                PubSub.publish('reload_email_attachments_table');
            },
            error: function (xhr) {
                abp.notify.error(xhr.responseText || 'Failed to upload attachment.');
            },
            complete: function () {
                input.value = '';
                $('#email_attachment_upload_btn')
                    .html('<i class="fl fl-plus me-1"></i>Add Attachments')
                    .prop('disabled', false);
                $('#attachment-upload-progress').hide();
            }
        });
    }

    // ── Draggable divider ─────────────────────────────────────────────────────
    function initializeDivider() {
        
        const $divider   = $('#divider');
        const $leftPane  = $('#leftPane');
        const $rightPane = $('#rightPane');
        const $container = $('#splitContainer');

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
    }

    PubSub.subscribe('reload_email_attachments_table', () => {
        reloadEmailAttachmentsTable();
    });

    function reloadEmailAttachmentsTable() {
        if (emailAttachmentsTable) {
            emailAttachmentsTable.ajax.reload();
        }
    }

    function reloadTemplatesTable() {
        if (templatesDataTable) {
            templatesDataTable.ajax.reload();
        }
    }

    function reloadTemplatesTableAndClose() {
        if (templatesDataTable) {
            templatesDataTable.ajax.reload();
            closeRightPanel();
        }
    }

    PubSub.subscribe('reload_templates_table_no_close', () => {
        reloadTemplatesTable();
    });

    PubSub.subscribe('reload_templates_table_with_close', () => {
        reloadTemplatesTableAndClose();
    });


});

function initializeEditor(data, dropdownItems) {     
    const templateId = 'templateBody';
    
    // Remove existing editor instance if it exists
    const existingEditor = tinymce.get(templateId);
    if (existingEditor) {
        tinymce.remove(`#${templateId}`);
    }
    
    tinymce.init({
        license_key: 'gpl',
        selector: `#${templateId}`,
        plugins: 'lists link image preview code',
        toolbar: 'undo redo | styles | bold italic | alignleft aligncenter alignright alignjustify | bullist numlist | link image | code preview | variablesDropdownButton',
        resize: true,
        statusbar: true,
        elementpath: false,
        branding: false,
        promotion: false,
        content_css: false,
        skin: false,
        setup: function (editor) {                
            setupEditor(editor, templateId, templateId, data, dropdownItems);
        }
    });
}

function setupEditor(editor, id, editorId, data, dropdownItems) {
    editor.ui.registry.addMenuButton('variablesDropdownButton', {
        text: 'VARIABLES',
        fetch: fetchVariablesMenuItems(dropdownItems, editor)
    });

    editor.on('init', function () {
        editor.mode.set('design');
        if (data?.bodyHTML !== undefined) {
            editor.setContent(data.bodyHTML);
        }    
    });
}    

function fetchVariablesMenuItems(dropdownItems, editor) {
    return function (callback) {
        const items = createMenuItems(dropdownItems, editor);
        callback(items);
    };
}

function createMenuItems(dropdownItems, editor) {
    return dropdownItems.map(item => ({
        type: 'menuitem',
        text: item.text,
        onAction: () => {
            editor.insertContent(`{{${item.value}}}`);
        }
    }));
}

/**
 * Generates HTML for email attachment button
 * @param {string} attachmentId - Attachment ID
 * @returns {string} HTML for attachment button
 */
function generateEmailAttachmentButtonContent(attachmentId) {
    return `<button class="btn fullWidth" style="margin:10px" type="button" onclick="deleteEmailAttachment('${attachmentId}')">
                <i class="fl fl-cancel"></i>
            </button>`;
}


/**
 * Deletes email attachment with confirmation
 * @param {string} attachmentId - Attachment ID to delete
 */
function deleteEmailAttachment(attachmentId) {
    abp.message.confirm(
        'Are you sure you want to delete this attachment?',
        'Delete Attachment',
        function (confirmed) {
            if (confirmed) {
                unity.notifications.emails.emailLogAttachment
                    .delete(attachmentId)
                    .then(function () {
                        abp.notify.success('Attachment deleted successfully.');
                        PubSub.publish('reload_email_attachments_table');
                    })
                    .catch(function (e) {
                        console.warn('Failed to delete attachment:', e);
                        abp.notify.error('Failed to delete attachment.');
                    });
            }
        }
    );
}

function isValidEmail(email) {
    // Basic email validation regex
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}
