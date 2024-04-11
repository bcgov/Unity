$(function () {
    let selectedApplicationId = decodeURIComponent($("#DetailsViewApplicationId").val());    

    let applicationLinksModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'ApplicationLinks/ApplicationLinksModal',
    });

    $('body').on('click','#addLinksRecordsBtn',function(e){
        e.preventDefault();
        applicationLinksModal.open({
            applicationId: selectedApplicationId,
        });
    });

    applicationLinksModal.onOpen(function () {
        let linkInput = new LinksInput({
            selector: 'SelectedApplications',
            duplicate: false,
            max: 50
        });
        let suggestionsArray = [];
        let selectedApplications = $('#SelectedApplications').val();
        let allApplications = $('#AllApplications').val();
        if (allApplications) {
            suggestionsArray = allApplications.split(',');
        }
        linkInput.setSuggestions(suggestionsArray);

        if(selectedApplications.length) {
            linkInput.addData(selectedApplications.split(','));
        }
    });

    applicationLinksModal.onResult(function () {
        abp.notify.success(
            'The application links have been successfully updated.',
            'Application Links'
        );
        PubSub.publish("ApplicationLinks_refresh");
    });
});
