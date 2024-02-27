$(function () {
  let contactModal = new abp.ModalManager(abp.appPath + 'ApplicationContact/CreateContactModal');

  $('#AddContactButton').click(function (e) {
    let applicationId = document.getElementById('SummaryWidgetApplicationId').value;
    
    e.preventDefault();
    contactModal.open({
      applicationId: applicationId
    });
  });

  contactModal.onResult(function () {
      console.log('Contact Modal result');
  });

});
