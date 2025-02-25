$(function () {
    const assigneeListElement = document.getElementById("assigneeListData");
    const assigneeList = JSON.parse(assigneeListElement.dataset.assignees);
    const mentionDataList = assigneeList.map(item => ({
        key: item.FullName,
        value: item.FullName,
        email: item.Email,
    }));

    initTribute(mentionDataList);
   
    $('body').on('click', '.edit-button', function () {
        let itemId = $(this).data('id');
        toggleEditMode(itemId);
    });

    $('body').on('focus', '.comment-input', function () {     
        $('#addCommentContainer' + $(this).data('ownerid')).css('display', 'flex');
    });

    $('body').on('click', '.edit-comment-cancel-button', function () {        
        let itemId = $(this).data('id');                        
        toggleEditMode(itemId);
        $(".comment-input-mutliple[data-id='" + itemId + "']").val($(".comment-lbl[data-id='" + itemId + "']").text());
    });

    $('body').on('click', '.edit-comment-save-button', function () {
        const isEdit = true;
        let itemId = $(this).data('id');
        const tempOwnerId = $(this).data('ownerid');
        const tempType = $(this).data('type');
        let editedValue = $(".comment-input-mutliple[data-id='" + itemId + "']").val();
        const mentions = mentionDataList.filter(person => editedValue.includes(`@${person.value}`));

        if (mentions.length > 0) {
            const mentionedNamesEmail = mentions.map(m => m.email);
            sendComment(isEdit, tempOwnerId, tempType, editedValue, mentionedNamesEmail, itemId, mentionDataList);
        } else {
            updateComment(tempOwnerId, itemId, editedValue, tempType, mentionDataList);
        }
    });

    $('body').on('click', '.add-comment-save-button', function () {
        const isEdit = false;
        const tempOwnerId = $(this).data('ownerid');
        const tempType = $(this).data('type');
        const tempComment = $('#addCommentTextArea' + tempOwnerId).val();
        const mentions = mentionDataList.filter(person => tempComment.includes(`${person.value}`));

        if (mentions.length > 0) {
            const mentionedNamesEmail = mentions.map(m => m.email);
            sendComment(isEdit, tempOwnerId, tempType, tempComment, mentionedNamesEmail, null, mentionDataList)
        } else {
            saveComment(tempOwnerId, tempComment, tempType, mentionDataList);
        }
    });

    $('body').on('click', '.add-comment-cancel-button', function () {
        let text = $('#addCommentTextArea' + $(this).data('ownerid')).val();
        if (text.length === 0) {
            $('#addCommentContainer' + $(this).data('ownerid')).css('display', 'none');
        }
        $('#addCommentTextArea' + $(this).data('ownerid')).val('');
    });
});


function toggleEditMode(itemId) {
    $(".edit-mode[data-id='" + itemId + "']").toggle();
    let editedValue = $(".comment-input-mutliple[data-id='" + itemId + "']").val();
    $(".comment-input-mutliple[data-id='" + itemId + "']").val(editedValue.trim());
    $(".read-mode[data-id='" + itemId + "']").toggle();
}

function updateComment(ownerId, commentId, comment, commentType, mentionList) {
    if (comment.length < 1) return;
    try {
        unity.grantManager.comments.comment
            .update({ ownerId: ownerId, commentId: commentId, comment: comment, commentType: commentType })
            .done(function () {
                abp.notify.success(
                    'The comment has been updated.'
                );
                toggleEditMode(commentId);
                $(".comment-lbl[data-id='" + commentId + "']").text(comment);
                PubSub.publish(commentType + '_refresh');
                setTimeout(() => {
                    initTribute(mentionList);
                }, 500);
            });
    }
    catch (error) {
        console.log(error);
    }
}

function saveComment(ownerId, comment, commentType, mentionList) {
    if (comment.length < 1) return;
    try {
        unity.grantManager.comments.comment
            .create({ ownerId: ownerId, comment: comment, commentType: commentType })
            .then((response) => {
                return response;
            })
            .done(function () {
                abp.notify.success(
                    'The comment has been created.'
                );
                PubSub.publish(commentType + '_refresh');
                setTimeout(() => {
                    initTribute(mentionList);
                }, 500);
            });
    }
    catch (error) {
        console.log(error);
    }
}

function sendComment(tempIsEdit, tempOwnerId, tempCommentType, tempComment, tempMentionedNamesEmail, tempItemId, mentionList) {
    const submissionNo = $(".reference-no").first().text();
    const applicantName = $(".applicant-name").first().text();
    const currentUserName = $("#CurrentUserName").val();
    const appId = $("#DetailsViewApplicationId").val();

    const requestData = {
        subject: `${submissionNo}-${applicantName}`,
        from: currentUserName,
        body: tempComment,
        applicationId: appId,
        mentionNamesEmail: tempMentionedNamesEmail
    };

    return $.ajax({
        url: `/api/app/email-notification/send-comment-notification`,
        type: "POST",
        contentType: "application/json",
        data: JSON.stringify(requestData),
    }).then(response => {
        if (tempIsEdit) {
            updateComment(tempOwnerId, tempItemId, tempComment, tempCommentType, mentionList);
        } else {
            saveComment(tempOwnerId, tempComment, tempCommentType, mentionList);
        }
    }).catch(error => {
        console.error('There was a problem with the post operation:', error);
    });
}

function initTribute(mentionData) {
    //Tributejs getting textarea dynamic id
    const areaWithMentions = document.querySelectorAll('.comment-with-mention');

    let tribute = new Tribute({
        values: mentionData,
        selectTemplate: function (item) {
            if (typeof item === 'undefined') return null;
            if (this.range.isContentEditable(this.current.element)) {
                return (`<span contenteditable="false"><a class="name-highlighted" href="#" onclick="return false;">${item.original.value}</a><span>`);
            }
            return `@${item.original.value}`;
        },
        requireLeadingSpace: false,
    })

    areaWithMentions.forEach(item => {
        tribute.attach(document.getElementById(item.id));
    });
}