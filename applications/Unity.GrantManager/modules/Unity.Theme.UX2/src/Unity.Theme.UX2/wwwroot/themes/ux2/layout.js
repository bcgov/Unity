$(function () {
    $('.dropdown-menu a.dropdown-toggle').on('click', function (e) {
        if (!$(this).next().hasClass('show')) {
            $(this).parents('.dropdown-menu').first().find('.show').removeClass("show");
        }

        const $subMenu = $(this).next(".dropdown-menu");
        $subMenu.toggleClass('show');

        $(this).parents('li.nav-item.dropdown.show').on('hidden.bs.dropdown', function (e) {
            $('.dropdown-submenu .show').removeClass("show");
        });

        return false;
    });
});
window.addEventListener('DOMContentLoaded', (event) => {
    const currentUrl = window.location.pathname;
    const dashboardButton = document.querySelector('button[data-url="/Dashboard"]');
    const applicationsButton = document.querySelector('button[data-url="/GrantApplications"]');

    if (currentUrl === '/Dashboard') {
        dashboardButton.classList.add('active');
    } else if (currentUrl === '/GrantApplications') {
        applicationsButton.classList.add('active');
    }
});
document.addEventListener('DOMContentLoaded', function () {
    var userInitials = document.querySelector('.unity-user-initials');
    var userDropdown = document.getElementById('user-dropdown');

    userInitials.addEventListener('click', function (event) {

        // Toggle the visibility of the dropdown
        userDropdown.classList.toggle('show');

        // Open the dropdown items
        //dropdownMenu.classList.add('show'); //not defined error in console
    });

    // Close the dropdown when clicking outside of it
    window.addEventListener('click', function (event) {
        if (!userInitials.contains(event.target) && !userDropdown.contains(event.target)) {
            userDropdown.classList.remove('show');
            userInitials.classList.remove('active'); // Remove the highlight class when dropdown is hidden
            //dropdownMenu.classList.remove('show'); //not defined error in console
        }
    });
});

