$(function () {

    console.log('bind datatables!!!!');

    let table = $('#gridToBeExplicitNamed').DataTable({
        paging: false,
        bInfo: false,
        searching: false,
        serverside: false,
        info: false,
        lengthChange: false
    });   
});
