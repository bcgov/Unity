$(function () {
    abp.log.debug('Index.js initialized!');
});

$('.landing-navigation-child').first().addClass("active");

$(function () {
    $('.landing-navigation-child').on("click", function (e) {
        $('.landing-navigation-child').removeClass("active");
        $(this).addClass("active");
    });
});


$(function () {
    setTimezoneCookie();

    function setTimezoneCookie() {
        let timezone_cookie = "timezoneoffset";
        setCookie(timezone_cookie, new Date().getTimezoneOffset(), 5);
    }

    function setCookie(cname, cvalue, exdays) {
        const d = new Date();
        d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
        let expires = "expires=" + d.toUTCString();
        document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/;domain=" +
            window.location.hostname + ";SameSite=Lax;secure;";
    }

    $('.btn-forms-templte').on("click", function (e) {
        document.getElementById("formsTemplateLibrary").style.display = "block";
        document.getElementById("welcomeContent").style.display = "none";
        document.getElementById("btn-templates").classList.add('active');
        document.getElementById("btn-features").classList.remove('active');
    });

    $('.btn-features').on("click", function (e) {
        document.getElementById("formsTemplateLibrary").style.display = "none";
        document.getElementById("welcomeContent").style.display = "block";
        document.getElementById("btn-templates").classList.remove('active');
        document.getElementById("btn-features").classList.add('active');
    });

    $('.scrol-to-steps').on("click", (function (e) {
        e.preventDefault();

        let element = document.getElementById('div-use-template');
        let navBarHeight = 108;

        element.scrollIntoView({ behavior: 'smooth', block: 'end' });

        setTimeout(function () {

            let scrolledY = window.scrollY;

            if (scrolledY) {
                window.scroll(0, scrolledY - navBarHeight);
            }
        }, 800);
    }));


    let addTemplateModal = new abp.ModalManager({
        viewUrl: 'Template/TemplateModal'
    });
    $('#addTemplate').on("click", function () {
        addTemplateModal.open({
        });
    })
});