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

    setTimezoneCookie();

    function setTimezoneCookie() {
        let timezone_cookie = "timezoneoffset";
        let cookie = getCookie(timezone_cookie);
        if (!cookie) {
            setCookie(timezone_cookie, new Date().getTimezoneOffset(), 90);
        }
    }

    function setCookie(cname, cvalue, exdays) {
        const d = new Date();
        d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
        let expires = "expires=" + d.toUTCString();
        document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
    }

    function getCookie(cname) {
        let name = cname + "=";
        let ca = document.cookie.split(';');
        for (const element of ca) {
            let c = element;
            while (c.charAt(0) == ' ') {
                c = c.substring(1);
            }
            if (c.indexOf(name) == 0) {
                return c.substring(name.length, c.length);
            }
        }
        return "";
    }
});