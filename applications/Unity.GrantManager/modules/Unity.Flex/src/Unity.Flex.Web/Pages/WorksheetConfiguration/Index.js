$(function () {
    $('#worksheet_import_upload_btn').click(function () {
        $('#worksheet_import_upload').trigger('click');
    });
});

function importWorksheetFile(inputId) {
    importFlexFile(inputId, "/api/app/worksheet/import", "Worksheet", 'refresh_worksheet_list');
}

function importFlexFile(inputId, urlStr, flexType, refreshChannel) {
    let input = document.getElementById(inputId);
    let file = input.files[0]; // Only get the first file
    let formData = new FormData();
    const maxFileSize = decodeURIComponent($("#MaxFileSize").val());

    if (!file) {
        return;
    }

    if ((file.size * 0.000001) > maxFileSize) {
        input.value = null;
        return abp.notify.error(
            'Error',
            'File size exceeds ' + maxFileSize + 'MB'
        );
    }

    formData.append("file", file);

    $.ajax({
        url: urlStr,
        data: formData,
        processData: false,
        contentType: false,
        type: "POST",
        success: function (data) {
            abp.notify.success(
                data.responseText,
                flexType + ' Import Is Successful'
            );
            PubSub.publish(refreshChannel, { scoresheetId: null });
            input.value = null;
        },
        error: function (data) {
            abp.notify.error(
                data.responseText,
                flexType + ' Import Not Successful'
            );
            PubSub.publish(refreshChannel, { scoresheetId: null });
            input.value = null;
        }
    });
}
