let selectedApplicationId = null;
let panelRequestId = 0;
let panelRefreshXhr = null;
let bulkEmailSendConfirmed = false;

const EMAILS_WIDGET_REFRESH_URL = '/GrantApplications/Widgets/Emails/RefreshEmails';

// Bumps the version counter every async step in the right-panel load flow is checked against, and aborts
// whatever widget-refresh request is currently in flight. Called from every place that changes what the panel
// should show without necessarily going through loadDraftIntoPanel itself (the no-draft/error branch, resetting
// the panel, modal close) — loadDraftIntoPanel bumps/aborts on its own, but those other paths don't touch it at
// all otherwise, which would leave a stale in-flight request free to land later with nothing to stop it.
function invalidatePendingPanelRequest() {
    panelRequestId++;
    if (panelRefreshXhr) {
        panelRefreshXhr.abort();
        panelRefreshXhr = null;
    }
}

function removeApplicationEmail(containerId) {
    if (selectedApplicationId && containerId === selectedApplicationId + '_container') {
        resetEditPanel();
    }

    $('#' + containerId).remove();
    let applicationsCount = $('#ApplicationsCount').val();
    $('#ApplicationsCount').val(applicationsCount - 1);
    runValidations();
}

function runValidations() {
    let isValid = true;
    let itemCount = 0;

    $('#bulkEmailNotificationForm input[name="BulkEmailNotifications.Index"]').each(function () {
        itemCount++;
        let index = $(this).val();
        let isValidField = $('#bulkEmailNotificationForm input[name="BulkEmailNotifications[' + index + '].IsValid"]').val();

        if (isValidField.toLowerCase() !== 'true') {
            isValid = false;
        }
    });

    if (itemCount === 0) {
        isValid = false;
    }

    if (!validBatchCount()) {
        isValid = false;
        setMaxCountError(true);
    } else {
        setMaxCountError(false);
    }

    if (isValid) {
        enableBulkEmailSubmit();
    } else {
        disableBulkEmailSubmit();
    }
}

function setMaxCountError(visible) {
    const summary = $('#batch-approval-summary');
    if (visible) {
        summary.css('display', 'block');
    } else {
        summary.css('display', 'none');
    }
}

function validBatchCount() {
    // .val() returns strings; comparing them with <= directly does a lexicographic (character-by-character)
    // comparison, not a numeric one — e.g. "6" <= "50" is false, because '6' sorts after '5'. Parse both sides
    // to numbers first so counts like 6-9 (and 60-69, 70-79, ...) aren't misreported as exceeding the max.
    let applicationsCount = Number.parseInt($('#ApplicationsCount').val(), 10);
    let maxBatchCount = Number.parseInt($('#MaxBatchCount').val(), 10);
    return applicationsCount <= maxBatchCount;
}

function enableBulkEmailSubmit() {
    $("#sendEmailNotificationModal")
        .find('#btnSubmitBulkEmail').prop("disabled", false);
}

function disableBulkEmailSubmit() {
    $("#sendEmailNotificationModal")
        .find('#btnSubmitBulkEmail').prop("disabled", true);
}

function closeEmailNotifications() {
    $('#sendEmailNotificationModal').modal('hide');
}

// ---- Right-panel draft edit/select behavior ----

function selectApplicationRow(applicationId, event) {
    if (event) {
        event.stopPropagation();
    }

    if (selectedApplicationId === applicationId) {
        return;
    }

    if (hasUnsavedEditorChanges()) {
        abp.message.confirm(
            'You have unsaved changes in the current draft. Switching applications will discard them. Continue?',
            'Unsaved Changes',
            function (confirmed) {
                if (confirmed) {
                    proceedWithRowSelection(applicationId);
                }
            }
        );
        return;
    }

    proceedWithRowSelection(applicationId);
}

function hasUnsavedEditorChanges() {
    // showErrorState()/showEmptyState()/showLoadingState() only hide #bulkEmailEditPanelWidget via CSS — they
    // don't clear its content. If the user switches away from a dirty draft to a row with no valid draft (or
    // an empty selection), the previous row's #EmailForm is still sitting in the DOM, hidden but with its
    // Save button still enabled. Without the visibility check, that stale, no-longer-shown draft would keep
    // reporting as "dirty" on every subsequent row switch and on the bottom Send button, even though there's
    // nothing left on screen for the user to have unsaved changes in.
    const $panel = $('#bulkEmailEditPanelWidget');
    if (!$panel.is(':visible')) {
        return false;
    }

    const $saveBtn = $panel.find('#btn-save-top');
    return $saveBtn.length > 0 && !$saveBtn.prop('disabled');
}

function proceedWithRowSelection(applicationId) {
    selectedApplicationId = applicationId;

    $('.bulk-email-row').removeClass('row-selected');
    const $row = $('#' + applicationId + '_container');
    $row.addClass('row-selected');

    showCorrectStateForSelection();
}

// Re-derives and shows whatever the right panel should currently display for selectedApplicationId, from the
// left panel's own row data (never touched by widget refreshes).
function showCorrectStateForSelection() {
    const $row = $('#' + selectedApplicationId + '_container');
    const emailId = $row.find('input[name$=".EmailId"]').val();

    if (!emailId) {
        // Not going through loadDraftIntoPanel here, so it won't invalidate whatever request a *previous*
        // selection left in flight on its own — do it explicitly, or a stale response for that previous
        // application could still land on top of this row's error state once it resolves.
        invalidatePendingPanelRequest();
        showErrorState(getRowErrorHtml($row));
        return;
    }

    loadDraftIntoPanel(selectedApplicationId, emailId);
}

function getRowErrorHtml($row) {
    const $errorNote = $row.find('.bulk-approval-notes-column').filter(function () {
        return $(this).css('display') !== 'none';
    }).first();

    return $errorNote.length ? $errorNote.html() : 'This application has no editable draft.';
}

function resetEditPanel() {
    selectedApplicationId = null;
    invalidatePendingPanelRequest();
    showEmptyState();
}

function showEmptyState() {
    $('#bulkEmailEditPanelEmpty').show();
    $('#bulkEmailEditPanelError').hide();
    $('#bulkEmailEditPanelLoading').hide();
    $('#bulkEmailEditPanelWidget').hide();
}

function showErrorState(html) {
    $('#bulkEmailEditPanelEmpty').hide();
    $('#bulkEmailEditPanelError').html(html).show();
    $('#bulkEmailEditPanelLoading').hide();
    $('#bulkEmailEditPanelWidget').hide();
}

function showLoadingState() {
    $('#bulkEmailEditPanelEmpty').hide();
    $('#bulkEmailEditPanelError').hide();
    $('#bulkEmailEditPanelLoading').show();
    $('#bulkEmailEditPanelWidget').hide();
}

function showWidgetState() {
    $('#bulkEmailEditPanelEmpty').hide();
    $('#bulkEmailEditPanelError').hide();
    $('#bulkEmailEditPanelLoading').hide();
    $('#bulkEmailEditPanelWidget').show();
}

function loadDraftIntoPanel(applicationId, emailId) {
    const requestId = ++panelRequestId;

    // Deliberately not using abp.WidgetManager for this: it shares one wrapper across every row selection, and
    // its internal completion handling inserts whatever HTML a completed request returns into
    // #bulkEmailEditPanelWidget unconditionally — there's no documented way to abort a previous in-flight
    // request or gate the actual DOM write by a version/token. If row A is still refreshing when row B is
    // selected and the user starts editing B's draft, A's response arriving late would silently overwrite the
    // editor with A's blank markup — destroying whatever the user was typing, not just showing the wrong
    // content (correcting it afterward can restore the right *saved* state, but it can't recover keystrokes
    // that only ever existed in a DOM node that's already been replaced).
    //
    // Making the request ourselves gives an actual handle to abort the previous one. An aborted request's
    // .done() is guaranteed by jQuery to never fire, so a stale response can't reach the DOM at all — this
    // removes the race by construction instead of detecting and correcting it after the fact. The requestId
    // check below covers the second leg of this flow (loadSelectedDraftData's own separate AJAX call), which
    // has no abortable handle of its own — a version counter checked on arrival works there regardless.
    if (panelRefreshXhr) {
        panelRefreshXhr.abort();
        panelRefreshXhr = null;
    }

    showLoadingState();

    // EmailsWidget/Default.js reads its owning application from this ambient hidden field (normally
    // rendered once by GrantApplications/Details.cshtml for a single fixed application). ActionBar/Default.cshtml
    // renders an empty placeholder for it on page load; update it here before every refresh so the widget
    // picks up whichever application is currently selected in this modal.
    $('#DetailsViewApplicationId').val(applicationId);

    const currentUserId = decodeURIComponent($('#CurrentUserId').val());

    panelRefreshXhr = $.ajax({
        url: EMAILS_WIDGET_REFRESH_URL,
        type: 'GET',
        data: { applicationId: applicationId, currentUserId: currentUserId }
    }).done(function (html) {
        panelRefreshXhr = null;
        if (requestId !== panelRequestId) {
            // Superseded by a newer row selection since this request was made — its own load already owns
            // the panel now (or will shortly); this response is stale and must not touch the DOM.
            return;
        }

        $('#bulkEmailEditPanelWidget').html(html);

        // jquery-validation-unobtrusive only converts data-val-* attributes into jQuery Validate rules once,
        // automatically, against whatever is already in the DOM when $(document).ready() fires. This markup
        // arrives later over AJAX, so without re-parsing it here, EmailsWidget's own $(emailForm).valid() call
        // (used for Subject/From/Body) would silently pass no matter what those fields contain — only the
        // hand-validated "To" field would still be enforced client-side.
        if (window.jQuery && $.validator && $.validator.unobtrusive) {
            $.validator.unobtrusive.parse('#bulkEmailEditPanelWidget');
        }

        // EmailsWidget/Default.js normally sets itself up exactly once, at page load, against markup that's
        // already server-rendered on the page. Its edit fields/buttons/attachment handlers only just got
        // inserted into the DOM by the refresh above, so its setup needs to run again against them —
        // window.EmailsWidget.reinitialize() (added to Default.js for this reuse) does that.
        window.EmailsWidget.reinitialize();
        loadSelectedDraftData(applicationId, emailId, requestId);
    }).fail(function (jqXHR) {
        panelRefreshXhr = null;

        if (jqXHR.statusText === 'abort' || requestId !== panelRequestId) {
            // Either superseded (aborted by us) or, if it somehow still fired, superseded by a newer selection
            // — either way that newer request owns what the panel shows next, not this failure.
            return;
        }

        console.warn('Failed to load draft editor for application:', applicationId, jqXHR);
        showErrorState('Failed to load the draft email. Please try again.');
    });
}

function loadSelectedDraftData(applicationId, emailId, requestId) {
    unity.notifications.emailNotifications.emailNotification.getHistoryByApplicationId(applicationId)
        .then(function (history) {
            if (requestId !== panelRequestId) {
                // A newer row selection has started since this call began — that selection's own load is
                // responsible for the panel now, so don't publish this stale draft's data over whatever the
                // user may already be doing there.
                return;
            }

            const draft = (history || []).find(function (item) { return item.id === emailId; });

            if (!draft) {
                showErrorState('The selected draft could not be loaded. It may have changed — try re-selecting this application.');
                return;
            }

            PubSub.publish('email_selected', draft);
            showWidgetState();
        })
        .catch(function (e) {
            if (requestId !== panelRequestId) {
                return;
            }

            console.warn('Failed to load draft email for bulk edit panel:', e);
            showErrorState('Failed to load the draft email. Please try again.');
        });
}

function formatDateOnly(isoString) {
    return isoString ? isoString.substring(0, 10) : '';
}

function applyRevalidationResult(applicationId, result) {
    const $row = $('#' + applicationId + '_container');
    $row.find('input[name$=".IsValid"]').val(result.isValid ? 'true' : 'false');
    $row.find('input[name$=".EmailId"]').val(result.emailId || '');
    $row.find('input[name$=".CreatedByName"]').val(result.createdByName || '');
    $row.find('input[name$=".LastModified"]').val(formatDateOnly(result.lastModified));

    (result.notes || []).forEach(function (note) {
        $('#' + applicationId + '_container_' + note.key).css('display', note.active ? 'block' : 'none');
    });

    runValidations();
}

function revalidateRow(applicationId) {
    $.ajax({
        url: '/BulkEmailNotifications/SendEmailNotificationModal?handler=Revalidate',
        type: 'GET',
        data: { applicationId: applicationId }
    }).done(function (result) {
        // Defensive: a 2xx response with no/empty body (e.g. a proxy or future server-side change) would
        // otherwise crash applyRevalidationResult on result.isValid. The server-side fix returns a real error
        // status on failure, so this is a second line of defense, not the primary one.
        if (!result) {
            console.warn('Revalidation returned an empty response for application:', applicationId);
            return;
        }

        applyRevalidationResult(applicationId, result);

        // Only touch the visible panel if the user is still on the row that was actually just saved. Save
        // disables #btn-save-top immediately (before its AJAX call even resolves), which is also what
        // hasUnsavedEditorChanges() checks — so the user is free to switch to a different row while a save is
        // still in flight, with no "unsaved changes" prompt in the way. If they did, reloading the panel here
        // would yank away whatever they're now doing on the newly-selected row; the row that was actually
        // saved still gets its IsValid/notes/Created By/Last Modified refreshed above regardless.
        if (applicationId !== selectedApplicationId) {
            return;
        }

        const $row = $('#' + applicationId + '_container');
        const emailId = $row.find('input[name$=".EmailId"]').val();

        if (emailId) {
            loadDraftIntoPanel(applicationId, emailId);
        } else {
            resetEditPanel();
        }
    }).fail(function (e) {
        console.warn('Failed to revalidate application after save:', applicationId, e);
    });
}

// ---- Draggable divider between the list and edit panels ----

const PANEL_DIVIDER_MIN_LIST_WIDTH = 280;
const PANEL_DIVIDER_MIN_EDIT_WIDTH = 400;
const PANEL_DIVIDER_KEYBOARD_STEP = 24;

function getPanelDividerElements() {
    return {
        $container: $('.bulk-email-modal-body'),
        $list: $('#bulkEmailListPanel'),
        $divider: $('#bulkEmailPanelDivider')
    };
}

// Sets the list panel to an explicit pixel width (clamped so neither panel can be dragged/keyed below a
// usable minimum — the edit panel in particular needs enough room for TinyMCE's toolbar to lay out sanely)
// and keeps the divider's aria-value* attributes in sync for screen reader users, whether or not they ever
// actually drag it.
function applyListPanelWidth(widthPx) {
    const { $container, $list, $divider } = getPanelDividerElements();
    if (!$container.length || !$list.length) {
        return;
    }

    const containerWidth = $container.width();
    const dividerWidth = $divider.outerWidth() || 0;
    const maxListWidth = Math.max(containerWidth - dividerWidth - PANEL_DIVIDER_MIN_EDIT_WIDTH, PANEL_DIVIDER_MIN_LIST_WIDTH);
    const clampedWidth = Math.min(Math.max(widthPx, PANEL_DIVIDER_MIN_LIST_WIDTH), maxListWidth);

    $list.css('flex', '0 0 ' + clampedWidth + 'px');

    $divider.attr({
        'aria-valuenow': Math.round(clampedWidth),
        'aria-valuemin': PANEL_DIVIDER_MIN_LIST_WIDTH,
        'aria-valuemax': Math.round(maxListWidth)
    });
}

function startPanelDividerDrag(event) {
    event.preventDefault();

    const { $list, $divider } = getPanelDividerElements();
    const startX = event.pageX;
    const startWidth = $list.outerWidth();

    $divider.addClass('dragging');
    $('body').addClass('bulk-email-resizing');

    function onDragMove(moveEvent) {
        applyListPanelWidth(startWidth + (moveEvent.pageX - startX));
    }

    function onDragEnd() {
        $(document).off('mousemove.bulkEmailDivider', onDragMove);
        $(document).off('mouseup.bulkEmailDivider', onDragEnd);
        $divider.removeClass('dragging');
        $('body').removeClass('bulk-email-resizing');
    }

    $(document).on('mousemove.bulkEmailDivider', onDragMove);
    $(document).on('mouseup.bulkEmailDivider', onDragEnd);
}

function handlePanelDividerKeydown(event) {
    if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') {
        return;
    }

    event.preventDefault();
    const { $list } = getPanelDividerElements();
    const step = event.key === 'ArrowLeft' ? -PANEL_DIVIDER_KEYBOARD_STEP : PANEL_DIVIDER_KEYBOARD_STEP;
    applyListPanelWidth($list.outerWidth() + step);
}

$(function () {
    PubSub.subscribe('refresh_application_emails', function (_msg, data) {
        // EmailsWidget publishes the applicationId that was actually saved/sent (see Default.js) — use that
        // rather than assuming it's whatever row happens to be selected right now, since the user may have
        // switched rows while the save was still in flight. Fall back to the current selection for any other
        // publisher of this topic that doesn't pass one.
        const savedApplicationId = data?.applicationId || selectedApplicationId;
        if (!savedApplicationId) {
            return;
        }
        revalidateRow(savedApplicationId);
    });

    // The bottom Send button only posts this modal's own form (ApplicationId/EmailId/IsValid hidden fields) —
    // it has no idea what's currently typed into EmailsWidget's separate #EmailForm in the right panel, and the
    // backend always sends whatever is last *saved* to the draft. Without this guard, unsaved edits would be
    // silently dropped: the user would see "success" for content they never actually saved.
    $(document).on('click', '#btnSubmitBulkEmail', function (event) {
        if (bulkEmailSendConfirmed) {
            bulkEmailSendConfirmed = false;
            return;
        }

        // EmailsWidget's own attachment-total-size check (checkTotalAttachmentSize in Default.js) only ever
        // toggles a button inside its own markup, which this modal hides — it has no way to know about, and
        // never disables, our own #btnSubmitBulkEmail. Reachable via applying a different email template
        // while attachments already exist (copyTemplateAttachments bypasses the normal per-upload size gate).
        // Unlike unsaved edits, there's no reasonable "send anyway" here — the total genuinely exceeds what's
        // allowed — so this is a hard block, not a confirm-to-proceed prompt. is(':visible') is false whenever
        // the panel itself is hidden (error/empty/loading state), so this can't fire for a stale, no-longer-
        // shown row the same way hasUnsavedEditorChanges() could before its own visibility fix.
        if ($('#email-attachment-size-error').is(':visible')) {
            event.preventDefault();
            event.stopImmediatePropagation();
            abp.message.error(
                'The currently open draft has attachments exceeding the total size limit. Remove attachments before sending.',
                'Attachments Too Large'
            );
            return;
        }

        if (!hasUnsavedEditorChanges()) {
            return;
        }

        event.preventDefault();
        event.stopImmediatePropagation();

        abp.message.confirm(
            'The currently open draft has unsaved changes. They will not be included in this send unless you save first. Send anyway?',
            'Unsaved Changes',
            function (confirmed) {
                if (confirmed) {
                    bulkEmailSendConfirmed = true;
                    $('#btnSubmitBulkEmail').trigger('click');
                }
            }
        );
    });

    // Delegated (rather than bound directly to #bulkEmailPanelDivider) because this script is bundled once at
    // page load, while the divider itself is part of the modal's DOM, which ABP's ModalManager destroys and
    // freshly re-renders on every open.
    $(document).on('mousedown', '#bulkEmailPanelDivider', startPanelDividerDrag);
    $(document).on('keydown', '#bulkEmailPanelDivider', handlePanelDividerKeydown);

    // Initializes the divider's aria-value* attributes against the panel's default CSS-defined width (40%),
    // without changing anything visually — applyListPanelWidth just re-expresses the current rendered width
    // as an explicit pixel flex-basis, which future drags/keypresses then adjust from. Needed so a keyboard
    // user tabbing to the divider without ever dragging it still gets correct aria-valuenow/min/max.
    $(document).on('shown.bs.modal', '#sendEmailNotificationModal', function () {
        const $list = $('#bulkEmailListPanel');
        if ($list.length) {
            applyListPanelWidth($list.outerWidth());
        }
    });

    // This script is bundled once per page load, but the modal's DOM (including #bulkEmailEditPanelWidget)
    // is destroyed and freshly re-rendered by ABP's ModalManager on every open. Reset module state on close
    // so a stale selectedApplicationId or an in-flight request from a previous open doesn't leak into the
    // next one — invalidatePendingPanelRequest() bumps the version counter (so any still-pending response,
    // for either async leg of loadDraftIntoPanel, gets ignored on arrival) and aborts the refresh request.
    $(document).on('hidden.bs.modal', '#sendEmailNotificationModal', function () {
        selectedApplicationId = null;
        invalidatePendingPanelRequest();
        bulkEmailSendConfirmed = false;
        $('#DetailsViewApplicationId').val('');
    });
});
