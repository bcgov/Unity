$(function () {
    let reportViewSyncManager = new abp.ModalManager({
        viewUrl: 'ReportViewSync/ReportViewSyncModal'
    });
    $('#runSyncJobsButton').on("click", function () {
        reportViewSyncManager.open();
    });
    reportViewSyncManager.onResult(function (_, response) {        
    });
});
