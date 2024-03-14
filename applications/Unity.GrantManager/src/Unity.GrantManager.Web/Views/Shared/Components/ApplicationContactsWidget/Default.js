$(function () {
    let contactModal = new abp.ModalManager(abp.appPath + 'ApplicationContact/EditContactModal');

    $('body').on('click','.contact-edit-btn',function(e){
        e.preventDefault();
        let itemId = $(this).data('id');
        contactModal.open({
            id: itemId
        });
    });

    contactModal.onResult(function () {
        abp.notify.success(
            'The application contact have been successfully updated.',
            'Application Contacts'
        );
        PubSub.publish("refresh_application_contacts");
    });
});
