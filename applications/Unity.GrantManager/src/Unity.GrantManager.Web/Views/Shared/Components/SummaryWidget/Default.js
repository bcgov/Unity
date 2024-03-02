$(function () {
  let applicationId = document.getElementById('SummaryWidgetApplicationId').value;
  let contactModal = new abp.ModalManager(abp.appPath + 'ApplicationContact/CreateContactModal');

  let applicationContactsWidgetManager = new abp.WidgetManager({
    wrapper: '#applicationContactsWidget',
    filterCallback: function () {
        return {
            'applicationId': applicationId
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
      console.log('Contact Modal result');
      applicationContactsWidgetManager.refresh();
  });
  

});

