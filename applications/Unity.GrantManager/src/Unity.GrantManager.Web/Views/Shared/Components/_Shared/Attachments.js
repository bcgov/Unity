function generateAttachmentButtonContent(data, type, full, meta, attachmentType) {
    let ownerId = getAttachmentOwnerId(attachmentType);
    let downloadUrl = `/api/app/attachment/${attachmentType}/${encodeURIComponent(ownerId)}/download/${encodeURIComponent(full.fileName)}`;
    let isCreator = abp.currentUser.id == full.creatorId;
    let html = `
        <div class="dropdown" style="float:right;">
            <button class="btn btn-light dropbtn" type="button">
                <i class="fl fl-attachment-more"></i>
            </button>
            <div class="dropdown-content">
                <a href="${downloadUrl}" target="_blank" download="${data}" class="fullwidth">
                    <button class="btn fullWidth" style="margin:10px" type="button">
                        <i class="fl fl-download"></i><span>Download Attachment</span>
                    </button>
                </a>
                <button class="btn fullWidth" style="margin:10px" type="button" ${`onclick="updateAttachmentMetadata('${attachmentType}','${full.id}')"`}>
                    <i class="fl fl-edit"></i><span>Edit Attachment</span>
                </button>
                <button class="btn fullWidth" style="margin:10px" type="button" ${isCreator ? `onclick="deleteAttachment('${attachmentType}','${data}','${full.fileName}')"` : 'disabled'}>
                    <i class="fl fl-cancel"></i><span>Delete Attachment</span>
                </button>
            </div>
        </div>
    `;

    return html;
}

function getAttachmentOwnerId(attachmentType) {
    switch (attachmentType) {
        case 'Assessment':
            return decodeURIComponent($("#AssessmentId").val());
        case 'Application':
            return decodeURIComponent($("#DetailsViewApplicationId").val());
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

function refreshAttachmentWidget(attachmentType) {
    switch (attachmentType) {
        case 'Assessment':
            PubSub.publish('refresh_assessment_attachment_list'); break;
        case 'Application':
            PubSub.publish('refresh_application_attachment_list'); break;
        case 'CHEFS':
            PubSub.publish('refresh_chefs_attachment_list'); break;
        default: break;
    }
}

