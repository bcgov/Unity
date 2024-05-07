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
    const currentNav = document.querySelector(`a[href="${currentUrl}"]`);
    if (currentNav) {
        currentNav.parentElement.classList.add('active');
    }    
});

document.addEventListener('DOMContentLoaded', function () {
    let userInitials = document.querySelector('.unity-user-initials');
    let userDropdown = document.getElementById('user-dropdown');

    if (userInitials) {
        userInitials.addEventListener('click', function (event) {
            // Prevent default bootstrap dropdown toggle behavior
            event.stopPropagation();

            // Toggle the visibility of the dropdown
            let isShown = userDropdown.classList.contains('show');
            userDropdown.classList.toggle('show', !isShown);

            // If the dropdown is being shown, make sure all submenus are also shown
            if (!isShown) {
                $(userDropdown).find('.dropdown-menu').addClass('show');
            } else {
                $(userDropdown).find('.dropdown-menu').removeClass('show');
            }
        });
    }

    // Close the dropdown when clicking outside of it
    window.addEventListener('click', function (event) {
        if (userInitials) {
            if (!userInitials.contains(event.target) && !userDropdown.contains(event.target)) {
                userDropdown.classList.remove('show');
                // Remove the highlight class when dropdown is hidden
                userInitials.classList.remove('active');
                // Hide all submenus
                $(userDropdown).find('.dropdown-menu').removeClass('show');
            }
        }
    });
});

