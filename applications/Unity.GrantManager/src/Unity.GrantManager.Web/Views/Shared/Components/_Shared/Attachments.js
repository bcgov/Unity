function getAttachmentSelectAllMarkup(selectAllClass) {
    return `<input type="checkbox" class="form-check-input checkbox-select chkbox ${selectAllClass}">`;
}

function getAttachmentSelectColumn(selectAllClass, rowIdPrefix = 'row_') {
    return {
        title: '',
        data: 'rowCount',
        name: 'select',
        orderable: false,
        className: 'notexport dt-checkboxes-cell attachment-select-cell text-center',
        checkboxes: {
            selectRow: true,
            selectAllRender: getAttachmentSelectAllMarkup(selectAllClass),
        },
        render: function (data) {
            return `<input type="checkbox" class="form-check-input checkbox-select chkbox row-checkbox" id="${rowIdPrefix}${data}">`;
        },
    };
}

function renderAttachmentLabelCell(label, attachmentId) {
    let $cellWrapper = $('<div>').addClass('d-flex align-items-center');
    let $textWrapper = $('<div>')
        .addClass('w-100')
        .append(label ?? '-');
    let $buttonWrapper = $('<div>').addClass('flex-shrink-1');

    let $editButton = $('<button>')
        .addClass('btn btn-sm edit-button px-0 float-end')
        .attr({
            'aria-label': 'Edit',
            title: 'Edit',
        })
        .data('attachment-id', attachmentId)
        .append($('<i>').addClass('fl fl-edit'));

    $cellWrapper.append($textWrapper);
    $buttonWrapper.append($editButton);
    $cellWrapper.append($buttonWrapper);

    return $cellWrapper.prop('outerHTML');
}

function buildAttachmentHeaderCallback(selectAllClass) {
    return function (thead) {
        const $headerCell = $(thead).find('th').first();
        $headerCell.html(getAttachmentSelectAllMarkup(selectAllClass));
    };
}

function bindAttachmentSelectionBehavior({
    dataTable,
    tableWrapper,
    selectAllClass,
    rowIdPrefix = 'row_',
    onSelect,
    onDeselect,
}) {
    const $tableWrapper = $(tableWrapper);

    function syncSelectAllHeader() {
        const allRows = $tableWrapper.find('.row-checkbox').length;
        const checkedRows = $tableWrapper.find('.row-checkbox:checked').length;
        $tableWrapper.find(`.${selectAllClass}`).prop(
            'checked',
            allRows > 0 && checkedRows === allRows
        );
    }

    dataTable.on('select', function (e, dt, type, indexes) {
        if (!indexes?.length) {
            return;
        }

        indexes.forEach((index) => {
            $tableWrapper.find(`#${rowIdPrefix}${index}`).prop('checked', true);
            onSelect?.(index);
        });

        syncSelectAllHeader();
    });

    dataTable.on('deselect', function (e, dt, type, indexes) {
        if (!indexes?.length) {
            return;
        }

        indexes.forEach((index) => {
            $tableWrapper.find(`#${rowIdPrefix}${index}`).prop('checked', false);
            onDeselect?.(index);
        });

        syncSelectAllHeader();
    });

    dataTable.on('draw', function () {
        syncSelectAllHeader();
    });

    $tableWrapper.on('click', `.${selectAllClass}`, function () {
        if ($(this).is(':checked')) {
            dataTable.rows().select();
            return;
        }

        dataTable.rows().deselect();
    });
}

function observeAttachmentTableResize(tableWrapperSelector, adjustTableColumns) {
    const tableWrapper = document.querySelector(tableWrapperSelector);
    if (!tableWrapper || typeof ResizeObserver === 'undefined') {
        return null;
    }

    let rafId = null;
    const observer = new ResizeObserver(() => {
        if (rafId) {
            cancelAnimationFrame(rafId);
        }

        rafId = requestAnimationFrame(() => {
            adjustTableColumns();
        });
    });

    observer.observe(tableWrapper);
    return observer;
}

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
