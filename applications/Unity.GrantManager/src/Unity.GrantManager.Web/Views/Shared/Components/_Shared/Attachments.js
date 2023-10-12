function generateAttachmentButtonContent(data, type, full, meta, attachmentType) {
    let ownerId = getAttachmentOwnerId(attachmentType);
    let html = '<div class="dropdown" style="float:right;">';
    html += '<button class="btn btn-light dropbtn" type="button"><i class="fl fl-attachment-more" ></i></button>';
    html += '<div class="dropdown-content">';
    html += '<a href="/api/app/attachment/' + attachmentType + '/' + encodeURIComponent(ownerId) + '/download/' + encodeURIComponent(full.fileName);
    html += '" target="_blank" download="' + data + '" class="fullwidth">';
    html += '<button class="btn fullWidth" style="margin:10px" type="button"><i class="fl fl-download"></i><span>Download Attachment</span></button></a>';
    if (abp.currentUser.id == full.creatorId) {
        html += '<button class="btn fullWidth" style="margin:10px" type="button" onclick="deleteAttachment(' + `'${attachmentType}','${data}','${full.fileName}'` + ')">';
        html += '<i class="fl fl-cancel"></i><span>Delete Attachment</span></button > ';
    } else {
        //disable delete button
        html += '<button class="btn fullWidth" style="margin:10px" disabled type="button" ';
        html += '"><i class="fl fl-cancel"></i><span>Delete Attachment</span></button>';
    }
    html += '</div>';
    html += '</div>';
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
        switch (attachmentType) {
            case 'Assessment':
                PubSub.publish('refresh_assessment_attachment_list'); break;
            case 'Application':
                PubSub.publish('refresh_application_attachment_list'); break;
            default: break;
        }
    });

    deleteAttachmentModal.open({
        s3ObjectKey: s3ObjectKey,
        fileName: fileName,
        attachmentType: attachmentType,
        attachmentTypeId: getAttachmentOwnerId(attachmentType),
    });
}

