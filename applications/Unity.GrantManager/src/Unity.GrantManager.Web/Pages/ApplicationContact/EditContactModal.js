(function ($) {
    abp.modals.editOrDeleteContactModal = function () {
        let initModal = function (publicApi, args) {
            let modalManager = publicApi;

            $('#DeleteContactButton').click(handleDeleteContact);

            function handleDeleteContact(e) {
                e.preventDefault();
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
                modalManager.close();
                PubSub.publish("refresh_application_contacts");
                abp.notify.success('The contact has been deleted.');
            }

            function onDeleteFailure(error) {
                abp.notify.error('Contact deletion failed.');
                if (error) {
                    console.log(error);
                }
            }
        };
        return { initModal: initModal };
    }
})(jQuery);