$(function () {
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
        let itemId = $(this).data('id');
        let editedValue = $(".comment-input-mutliple[data-id='" + itemId + "']").val();
        updateComment($(this).data('ownerid'),
            itemId,
            editedValue,
            $(this).data('type'));
    });

    $('body').on('click', '.add-comment-save-button', function () {
        saveComment($(this).data('ownerid'),
            $('#addCommentTextArea' + $(this).data('ownerid')).val(),
            $(this).data('type'));
    });

    $('body').on('click', '.add-comment-cancel-button', function () {
        let text = $('#addCommentTextArea' + $(this).data('ownerid')).val();
        if (text.length === 0) {
            $('#addCommentContainer' + $(this).data('ownerid')).css('display', 'none');
        }
        $('#addCommentTextArea' + $(this).data('ownerid')).val('');
    });

    function toggleEditMode(itemId) {        
        $(".edit-mode[data-id='" + itemId + "']").toggle();
        let editedValue = $(".comment-input-mutliple[data-id='" + itemId + "']").val();
        $(".comment-input-mutliple[data-id='" + itemId + "']").val(editedValue.trim());
        $(".read-mode[data-id='" + itemId + "']").toggle();
    }

    function updateComment(ownerId, commentId, comment, commentType) {
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
                });
        }
        catch (error) {
            console.log(error);
        }
    }

    function saveComment(ownerId, comment, commentType) {
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
                });

        }
        catch (error) {
            console.log(error);
        }
    }   
});