$(function () {

    let contactModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'ApplicationContact/EditContactModal',
        modalClass: "editContactModal"
    });

    abp.modals.editContactModal = function () {
        let initModal = function (publicApi, args) {
            setupContactModal(args);
        };
        return { initModal: initModal };
    }

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

    let setupContactModal = function (args) {
        $('#DeleteContactButton').click(handleDeleteContact);

        function handleDeleteContact(e) {
            e.preventDefault();
            showDeleteConfirmation();
        }
        
        function showDeleteConfirmation() {
            abp.message.confirm('Are you sure to delete this contact?')
                .then(processDeleteConfirmation);
        }
        
        function processDeleteConfirmation(confirmed) {
            if (confirmed) {
                deleteContact();
            }
        }
        
        function deleteContact() {
            try {
                unity.grantManager.grantApplications.applicationContact
                    .delete(args.id)
                    .done(onContactDeleted)
                    .fail(onDeleteFailure);
            } catch (error) {
                onDeleteFailure(error);
            }
        }
        
        function onContactDeleted() {
            PubSub.publish("refresh_application_contacts");
            contactModal.close();
            abp.notify.success('The contact has been deleted.');
        }
        
        function onDeleteFailure(error) {
            abp.notify.error('Contact deletion failed.');
            if (error) {
                console.log(error);
            }
        }
    }
});
