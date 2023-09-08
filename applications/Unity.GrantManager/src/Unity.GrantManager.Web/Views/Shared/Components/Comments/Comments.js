$(function () {

    var myWidgetManager = new abp.WidgetManager({
        wrapper: '#MyDashboardWidgetsArea',
        filterForm: '#MyDashboardFilterForm'
    });
    $('#backBtn').click(function () {
        $('#adjudicationAddReviewView').fadeOut(1000);
        setTimeout(() => {
            $('#adjudicationMainView').fadeIn(1000);
        }, 800)

    });
    $('#clearComment').click(function () {
        $('#addCommentTextArea').val('');

    });

    const add_review = PubSub.subscribe(
        'add_review',
        (msg, data) => {
            $('#detailsTab a[href="#nav-review-and-adudication"]').tab('show');
           
        }
    );

    function toggleEditMode(itemId) {
        // Toggle visibility of textarea and span
        $(".edit-mode[data-id='" + itemId + "']").toggle();
        $(".read-mode[data-id='" + itemId + "']").toggle();
    }

    $(".edit-button").click(function () {
        var itemId = $(this).data("id");
        toggleEditMode(itemId);
    });
    $(".cancel-edit").click(function () {
        var itemId = $(this).data("id");
        toggleEditMode(itemId);
    });

    $(".comment-input").focus(function () {
        $(".add-comment").css("display", "flex");
    });

    $(".save-button").click(function () {
        var itemId = $(this).data("id");
        var editedValue = $(".comment-input-mutliple[data-id='" + itemId + "']").val();
        updateComments(itemId, editedValue);

    });

    function updateComments(commentId, commentValue) {
        console.log("commentValue", commentValue)

        try {
            unity.grantManager.grantApplications.assessmentComment
                .updateAssessmentComment(commentId, commentValue, {})
                .done(function () {
                    abp.notify.success(
                        'The comment has been updated.'
                    );
                    toggleEditMode(commentId);
                    $(".comment-lbl[data-id='" + commentId + "']").text(commentValue);
                });

        } catch (error) { }
    }

    $("#saveCommentBtn").click(function () {

        var comment = $("#addCommentTextArea").val();
        var submissionId = $("#ApplicationFormSubmissionId").val();
        saveComment(comment, submissionId);

    });
    function saveComment(comment, submissionId) {
        try {
            unity.grantManager.grantApplications.assessmentComment
                .createAssessmentComment(comment, submissionId, {})
                .then((response) => {
                    return response;
                })
                .done(function (result) {
                    abp.notify.success(
                        'The comment has been created.'
                    );


                    refreshComments(submissionId);
                });

        } catch (error) { }
    }

    function refreshComments(submissionId) {
        console.log("reload")
        var currentlocationUrl = $(location).attr('href');
        $.ajax({
            url: currentlocationUrl,
            success: function (data) {
                myWidgetManager.refresh();
            },
            error: function (error) {
                myWidgetManager.refresh();
            }
        })
    
        myWidgetManager.refresh();
    }
});
