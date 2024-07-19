$(function () {
    $('body').on('click','#goBack',function(e){
        e.preventDefault();
        location.href =
            '/GrantApplications';
    });
});

