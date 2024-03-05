$(function () {
  let applicationId = document.getElementById('SummaryWidgetApplicationId').value;
  let isReadOnly = document.getElementById('SummaryWidgetIsReadOnly').value;
  let contactModal = new abp.ModalManager(abp.appPath + 'ApplicationContact/CreateContactModal');

  let applicationContactsWidgetManager = new abp.WidgetManager({
    wrapper: '#applicationContactsWidget',
    filterCallback: function () {
        return {
            'applicationId': applicationId,
            'isReadOnly': isReadOnly
        };
    }
  });

  $('#AddContactButton').click(function (e) {    
    e.preventDefault();
    contactModal.open({
      applicationId: applicationId
    });
  });

  contactModal.onResult(function () {
    abp.notify.success(
      'The application contact have been successfully added.',
      'Application Contacts'
    );
    applicationContactsWidgetManager.refresh();
  });

  PubSub.subscribe(
    'refresh_application_contacts',
    (msg, data) => {
      applicationContactsWidgetManager.refresh();
    }
  );
});

