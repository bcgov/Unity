$(function () {
    abp.log.debug('Index.js initialized!');
});

$('.landing-navigation-child').first().addClass("active");

$(function () {
    $('.landing-navigation-child').click(function (e) {
        $('.landing-navigation-child').removeClass("active");
        $(this).addClass("active");
    });
    
    $('#unityLogoutBtn').click(function(e) {
        localStorage.removeItem("DataTables_GrantApplicationsTable_/GrantApplications");
    })
});
