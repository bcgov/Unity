(function ($) {
    'use strict';

    const l = abp.localization.getResource('GrantManager');
    const $form = $('#PortalMessageForm');
    const $toggle = $('#UseDefaultMultipleIdentitiesMessage');
    const $save = $('#SavePortalMessageButton');
    const $cancel = $('#CancelPortalMessageButton');
    const $reset = $('#ResetPortalMessageButton');
    const $error = $('#portal-message-error');
    const defaultHtml = $('#DefaultMultipleIdentitiesMessage').val();
    const maxLength = Number($form.attr('data-max-html-length'));
    let saved = {
        useDefaultMessage: $form.attr('data-use-default') === 'true',
        customMessageHtml: $('#CustomMultipleIdentitiesMessage').val() || null
    };
    let customDraftHtml = saved.customMessageHtml;
    let customDraftText = '';
    let editor;
    let initializing;
    let saving = false;
    let applyingDraft = false;

    function showError(message) {
        $error.text(message || '').prop('hidden', !message);
        $('#MultipleIdentitiesMessage').attr('aria-invalid', message ? 'true' : 'false');
        if (editor) {
            editor.getBody().setAttribute('aria-invalid', message ? 'true' : 'false');
        }
    }

    function validationError() {
        if ($toggle.prop('checked') && customDraftHtml === null) return '';
        // TinyMCE can return markup for a visually empty editor.
        const text = customDraftText.replace(/[\s\p{Cf}]/gu, '');
        let error = '';
        if (!text) error = l('ApplicantPortalSettings:MessageRequired');
        else if (customDraftHtml.length > maxLength) error = l('ApplicantPortalSettings:MessageTooLong', maxLength);
        return error && $toggle.prop('checked')
            ? error + ' ' + l('ApplicantPortalSettings:RetainedMessageValidationHelp') : error;
    }

    function updateControls() {
        if (!editor) return;
        const changed = $toggle.prop('checked') !== saved.useDefaultMessage || customDraftHtml !== saved.customMessageHtml;
        $save.prop('disabled', saving || !changed);
        $cancel.prop('disabled', saving || !changed);
        $reset.prop('disabled', saving);
        $toggle.prop('disabled', saving);
        $('#portal-message-length').text(l('ApplicantPortalSettings:MessageLength', editor.getContent().length, maxLength));
    }

    function rememberCustomDraft() {
        customDraftHtml = editor.getContent();
        customDraftText = editor.getContent({ format: 'text' });
    }

    function applyDraft(useDefault) {
        applyingDraft = true;
        $toggle.prop('checked', useDefault).attr('aria-checked', String(useDefault));
        editor.setContent(useDefault ? defaultHtml : customDraftHtml ?? defaultHtml);
        if (!useDefault) rememberCustomDraft();
        // Keep undo history within the displayed template, especially after Reset.
        editor.undoManager.clear();
        editor.mode.set(useDefault ? 'readonly' : 'design');
        applyingDraft = false;
        showError('');
        updateControls();
    }

    function restoreSaved() {
        applyingDraft = true;
        customDraftHtml = saved.customMessageHtml;
        customDraftText = '';
        if (customDraftHtml !== null) {
            editor.setContent(customDraftHtml);
            rememberCustomDraft();
            // Compare against TinyMCE's normalized HTML, not the server's source.
            saved.customMessageHtml = customDraftHtml;
        }
        applyDraft(saved.useDefaultMessage);
    }

    function initializeEditor() {
        if (editor) return Promise.resolve();
        if (initializing) return initializing;
        $toggle.prop('disabled', true);
        initializing = tinymce.init({
            license_key: 'gpl',
            selector: '#MultipleIdentitiesMessage',
            plugins: 'lists link',
            toolbar: 'undo redo | bold italic underline | bullist numlist | link unlink | removeformat',
            menubar: false,
            height: 280,
            branding: false,
            promotion: false,
            statusbar: false,
            elementpath: false,
            skin: false,
            content_css: false,
            content_style: 'body { font-family: Arial, sans-serif; font-size: 14px; line-height: 1.5; padding: 8px; }',
            valid_elements: 'p,br,strong/b,em/i,u,ul,ol,li,a[href|title]',
            formats: { underline: { inline: 'u', exact: true } },
            link_target_list: false,
            link_title: true,
            link_default_protocol: 'https',
            paste_data_images: false,
            setup: function (instance) {
                instance.on('init', function () {
                    editor = instance;
                    editor.getBody().setAttribute('aria-required', 'true');
                    editor.getBody().setAttribute('aria-label', l('ApplicantPortalSettings:Message'));
                    restoreSaved();
                });
                instance.on('input change undo redo', function () {
                    if (!editor || applyingDraft || $toggle.prop('checked')) return;
                    rememberCustomDraft();
                    showError(validationError());
                    updateControls();
                });
            }
        }).catch(function () {
            initializing = null;
            showError(l('ApplicantPortalSettings:EditorLoadError'));
        });
        return initializing;
    }

    $('#manage-statuses-menu-item, #manage-messages-menu-item').on('click', function () {
        const messages = this.id === 'manage-messages-menu-item';
        $('#manage-statuses-menu-item').toggleClass('active', !messages).attr('aria-pressed', String(!messages));
        $('#manage-messages-menu-item').toggleClass('active', messages).attr('aria-pressed', String(messages));
        $('#portal-status-div').toggleClass('d-none', messages);
        $('#portal-messages-div').toggleClass('d-none', !messages);
        if (messages) initializeEditor();
    });

    $toggle.on('change', function () {
        if (!editor) return;
        if ($toggle.prop('checked')) rememberCustomDraft();
        applyDraft($toggle.prop('checked'));
    });
    $reset.on('click', function () {
        if (!editor || saving) return;
        // In default mode the visible text is already the default. Leave the
        // retained custom template intact; Reset in custom mode replaces it.
        if (!$toggle.prop('checked')) customDraftHtml = defaultHtml;
        applyDraft($toggle.prop('checked'));
    });
    $cancel.on('click', function () { if (editor && !saving) restoreSaved(); });

    $form.on('submit', function (event) {
        event.preventDefault();
        if (!editor || saving) return;
        if (!$toggle.prop('checked')) rememberCustomDraft();
        const error = validationError();
        showError(error);
        if (error) { editor.focus(); return; }
        const input = {
            useDefaultMessage: $toggle.prop('checked'),
            messageHtml: editor.getContent(),
            customMessageHtml: customDraftHtml
        };
        saving = true;
        editor.mode.set('readonly');
        updateControls();
        abp.ui.setBusy($form);
        unity.grantManager.applicantPortal.applicantPortalMessage.updateMultipleIdentities(input)
            .then(function (result) {
                saved = { useDefaultMessage: result.useDefaultMessage, customMessageHtml: result.customMessageHtml ?? null };
                restoreSaved();
                abp.notify.success(l('ApplicantPortalSettings:MessageSaveSuccess'));
            })
            .catch(function (error) {
                const validation = error?.validationErrors?.map(item => item.message).join(' ');
                showError(validation || error?.message || l('ApplicantPortalSettings:MessageSaveError'));
            })
            .always(function () {
                saving = false;
                editor.mode.set($toggle.prop('checked') ? 'readonly' : 'design');
                updateControls();
                abp.ui.clearBusy($form);
            });
    });
})(jQuery);
