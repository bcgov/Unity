function escapeHtmlAttribute(value) {
    return String(value ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#39;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;');
}

function generateAttachmentButtonContent(data, type, full, meta, attachmentType) {
    let ownerId = getAttachmentOwnerId(attachmentType);
    let downloadUrl = `/api/app/attachment/${encodeURIComponent(attachmentType)}/${encodeURIComponent(ownerId)}/download/${encodeURIComponent(full.fileName)}`;
    let isCreator = abp.currentUser.id == full.creatorId;
    let escapedAttachmentType = escapeHtmlAttribute(attachmentType);
    let escapedOwnerId = escapeHtmlAttribute(ownerId);
    let escapedFileName = escapeHtmlAttribute(full.fileName);
    let escapedDisplayName = escapeHtmlAttribute(full.displayName || full.fileName);
    let escapedObjectKey = escapeHtmlAttribute(data);
    let escapedAttachmentId = escapeHtmlAttribute(full.id);
    let html = `
        <div class="dropdown" style="float:right;">
            <button class="btn btn-light dropbtn" type="button">
                <i class="fa-solid fa-ellipsis"></i>
            </button>
            <div class="dropdown-content">
                <button class="btn fullWidth js-preview-attachment" style="margin:10px;margin-left:8px;" type="button" data-attachment-type="${escapedAttachmentType}" data-owner-id="${escapedOwnerId}" data-file-name="${escapedFileName}" data-display-name="${escapedDisplayName}">
                    <i class="fa-regular fa-eye p-0"></i><span>Preview Attachment</span>
                </button>
                <a href="${escapeHtmlAttribute(downloadUrl)}" target="_blank" download="${escapedObjectKey}" class="fullwidth">
                    <button class="btn fullWidth" style="margin:10px" type="button">
                        <i class="fa-solid fa-download"></i><span>Download Attachment</span>
                    </button>
                </a>
                <button class="btn fullWidth" style="margin:10px" type="button" data-attachment-type="${escapedAttachmentType}" data-attachment-id="${escapedAttachmentId}" onclick="updateAttachmentMetadata(this.dataset.attachmentType, this.dataset.attachmentId)">
                    <i class="fa-solid fa-pencil"></i><span>Edit Attachment</span>
                </button>
                <button class="btn fullWidth" style="margin:10px" type="button" data-attachment-type="${escapedAttachmentType}" data-s3-object-key="${escapedObjectKey}" data-file-name="${escapedFileName}" ${isCreator ? 'onclick="deleteAttachment(this.dataset.attachmentType, this.dataset.s3ObjectKey, this.dataset.fileName)"' : 'disabled'}>
                    <i class="fa-solid fa-xmark"></i><span>Delete Attachment</span>
                </button>
            </div>
        </div>
    `;

    return html;
}

function previewAttachment(attachmentType, ownerId, fileName, displayName) {
    let previewModal = new abp.ModalManager({
        viewUrl: '../Attachments/PreviewAttachmentModal'
    });
    previewModal.open({
        attachmentType: attachmentType,
        ownerId: ownerId,
        fileName: fileName,
        displayName: displayName
    });
}

$(document).on('click', '.js-preview-attachment', function () {
    previewAttachment(
        $(this).attr('data-attachment-type'),
        $(this).attr('data-owner-id'),
        $(this).attr('data-file-name'),
        $(this).attr('data-display-name')
    );
});

function getAttachmentOwnerId(attachmentType) {
    switch (attachmentType) {
        case 'Assessment':
            return decodeURIComponent($("#AssessmentId").val());
        case 'Application':
            return decodeURIComponent($("#DetailsViewApplicationId").val());
        case 'Applicant':
            return decodeURIComponent($("#DetailsViewApplicantId").val());
        default:
            return null;
    }
}

function deleteAttachment(attachmentType, s3ObjectKey, fileName) {
    let deleteAttachmentModal = new abp.ModalManager({
        viewUrl: '../Attachments/DeleteAttachmentModal'
    });    

    deleteAttachmentModal.onResult(function () {
        abp.notify.success(
            'Attachment is successfully deleted.',
            'Delete Attachment'
        );

        refreshAttachmentWidget(attachmentType);
    });

    deleteAttachmentModal.open({
        s3ObjectKey: s3ObjectKey,
        fileName: fileName,
        attachmentType: attachmentType,
        attachmentTypeId: getAttachmentOwnerId(attachmentType),
    });
}

function updateAttachmentMetadata(attachmentType, attachmentId) {
    let updateAttachmentModal = new abp.ModalManager({
        viewUrl: '../Attachments/UpdateAttachmentModal'
    });
    updateAttachmentModal.onResult(function () {
        abp.notify.success(
            'Attachment is successfully updated.',
            'Update Attachment'
        );
        refreshAttachmentWidget(attachmentType);
    });
    updateAttachmentModal.open({
        attachmentType: attachmentType,
        attachmentId: attachmentId
    });
}

$(document).on('mouseenter', '.attachments-table .dropdown', function () {
    const rect = this.getBoundingClientRect();
    const $content = $(this).find('.dropdown-content');
    $content.css({ visibility: 'hidden', display: 'block', top: '', bottom: '' });
    const dropdownHeight = $content.outerHeight();
    $content.css({ visibility: '', display: '' });
    if (rect.bottom + dropdownHeight > window.innerHeight) {
        $content.css({ bottom: (window.innerHeight - rect.top) + 'px', top: '' });
    } else {
        $content.css({ top: rect.bottom + 'px', bottom: '' });
    }
});

function refreshAttachmentWidget(attachmentType) {
    switch (attachmentType) {
        case 'Assessment':
            PubSub.publish('refresh_assessment_attachment_list'); break;
        case 'Application':
            PubSub.publish('refresh_application_attachment_list'); break;
        case 'Applicant':
            PubSub.publish('refresh_applicant_attachment_list'); break;
        case 'CHEFS':
            PubSub.publish('refresh_chefs_attachment_list'); break;
        default: break;
    }
}

