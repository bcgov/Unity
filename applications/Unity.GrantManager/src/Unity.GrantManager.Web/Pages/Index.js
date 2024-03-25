$(function () {
    abp.log.debug('Index.js initialized!');
});

$('.landing-navigation-child').first().addClass("active");

$(function () {
    $('.landing-navigation-child').click(function (e) {
        $('.landing-navigation-child').removeClass("active");
        $(this).addClass("active");
    });
});


$(function () {
    setTimezoneCookie();

    function setTimezoneCookie() {
        let timezone_cookie = "timezoneoffset";
        setCookie(timezone_cookie, new Date().getTimezoneOffset(),1);
    }

    function setCookie(cname, cvalue, exdays) {
        const d = new Date();
        d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
        let expires = "expires=" + d.toUTCString();
        document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/;domain=" +
            window.location.hostname;
    }
});