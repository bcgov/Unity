$(function () {
    abp.log.debug('Index.js initialized!');
});

$('.landing-navigation-child').first().addClass("active");

$(function () {
    setTimezoneCookie();

    function setTimezoneCookie() {
        let timezone_cookie = "timezoneoffset";
        let cookie = getCookie(timezone_cookie);
        if (!cookie) {
            setCookie(timezone_cookie, new Date().getTimezoneOffset(), 90);
        }
    }

    function setCookie(cname, cvalue, exdays) {
        const d = new Date();
        d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
        let expires = "expires=" + d.toUTCString();
        document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/;domain=" +
            window.location.hostname;
    }

    function getCookie(cname) {
        let name = cname + "=";
        let ca = document.cookie.split(';');
        for (const element of ca) {
            let c = element;
            while (c.charAt(0) == ' ') {
                c = c.substring(1);
            }
            if (c.indexOf(name) == 0) {
                return c.substring(name.length, c.length);
            }
        }
        return "";
    }
    $('.btn-forms-templte').click(function (e) {
        document.getElementById("formsTemplateLibrary").style.display = "block";
        document.getElementById("welcomeContent").style.display = "none";
        document.getElementById("btn-templates").classList.add('active');
        document.getElementById("btn-features").classList.remove('active'); 
    });

    $('.btn-features').click(function (e) {
        document.getElementById("formsTemplateLibrary").style.display = "none";
        document.getElementById("welcomeContent").style.display = "block";
        document.getElementById("btn-templates").classList.remove('active');
        document.getElementById("btn-features").classList.add('active');
    });

    $('.scrol-to-steps').click(function (e) {
        e.preventDefault();

        var element = document.getElementById('div-use-template');
        var navBarHeight = 108;//6.75rem

        element.scrollIntoView({ behavior: 'smooth', block: 'end' });

        setTimeout(function () {

            var scrolledY = window.scrollY;

            if (scrolledY) {
                window.scroll(0, scrolledY - navBarHeight);
            }
        }, 800);
        });
  

        let addTemplateModal = new abp.ModalManager({
            viewUrl: 'Template/TemplateModal'
        });
        $('#addTemplate').click(function () {
            addTemplateModal.open({
            });
        })
    
});