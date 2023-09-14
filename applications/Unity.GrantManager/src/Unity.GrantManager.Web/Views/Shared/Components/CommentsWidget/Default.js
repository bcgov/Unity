$(function () {
    $('#backBtn').click(function () {
        $('#adjudicationAddReviewView').fadeOut(1000);
        setTimeout(() => {
            $('#adjudicationMainView').fadeIn(1000);
        }, 800)
    });

    $('body').on('click', '#clearComment', function () {
        let text = $('#addCommentTextArea').val();        
        if (text.length === 0) {
            $(".add-comment").css("display", "none");
        } 
        $('#addCommentTextArea').val('');
    });

    $('body').on('click', '.edit-button', function () {
        let itemId = $(this).data('id');
        toggleEditMode(itemId);
    });

    $('body').on('click', '.cancel-edit', function () {
        let itemId = $(this).data('id');
        $('.add-comment').css('display', 'none');
        toggleEditMode(itemId);
    });

    $('body').on('focus', '.comment-input', function () {
        $('.add-comment').css('display', 'flex');
    });

    $('body').on('click', '.save-button', function () {                
        let itemId = $(this).data('id');
        let ownerId = $('#CommentWidgetOwnerId').val();
        let commentType = $('#CommentWidgetType').val();
        let editedValue = $(".comment-input-mutliple[data-id='" + itemId + "']").val();
        updateComment(ownerId, itemId, editedValue, commentType);
    });

    $('body').on('click', '#saveCommentBtn', function () {
        let comment = $('#addCommentTextArea').val();        
        let ownerId = $('#CommentWidgetOwnerId').val();
        let commentType = $('#CommentWidgetType').val();
        saveComment(ownerId, comment, commentType);
    });

    function toggleEditMode(itemId) {
        // Toggle visibility of textarea and span
        $(".edit-mode[data-id='" + itemId + "']").toggle();
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
                .done(function (result) {
                    abp.notify.success(
                        'The comment has been created.'
                    );

                    PubSub.publish('refresh_comments', ownerId);
                });

        }
        catch (error) {
            console.log(error);
        }
    }
});
