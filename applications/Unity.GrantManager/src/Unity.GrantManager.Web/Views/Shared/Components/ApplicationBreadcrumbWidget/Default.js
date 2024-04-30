$(function () {
    $('body').on('click','#goBack',function(e){
        e.preventDefault();
        window.history.back();
    });
});

