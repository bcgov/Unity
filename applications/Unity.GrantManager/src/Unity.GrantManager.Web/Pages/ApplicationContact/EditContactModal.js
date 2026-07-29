(function ($) {
    function onDeleteFailure(error) {
        abp.notify.error('Contact deletion failed.');
        if (error) {
            console.log(error);
        }
    }

    function initModal(publicApi, args) {
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
    }

    abp.modals.editOrDeleteContactModal = function () {
        return { initModal: initModal };
    }
})(jQuery);
