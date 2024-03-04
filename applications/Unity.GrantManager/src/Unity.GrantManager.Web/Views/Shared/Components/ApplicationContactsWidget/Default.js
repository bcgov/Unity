$(function () {
    let contactModal = new abp.ModalManager(abp.appPath + 'ApplicationContact/EditContactModal');

    $('.contact-edit-btn').click(function (e){
        e.preventDefault();
        let itemId = $(this).data('id');  
        console.log(itemId);
        contactModal.open({
            id: itemId
          });
    });
});
