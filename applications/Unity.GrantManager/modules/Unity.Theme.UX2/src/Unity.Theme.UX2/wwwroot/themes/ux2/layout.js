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

    function isTextOverflowing(element) {
        return element.scrollWidth > element.clientWidth;
    }

    function createTooltipOnOverflow(inputElement, tooltipTarget) {
        if (inputElement.tagName !== 'INPUT') {
            console.warn('createTooltipOnOverflow: The provided inputElement is not an input element');
            return;
        }

        const elementTitle = inputElement.value;
        if (elementTitle && isTextOverflowing(inputElement)) {
            bootstrap.Tooltip.getOrCreateInstance(tooltipTarget, {
                title: elementTitle,
                trigger: 'hover',
                placement: 'right',
                fallbackPlacements: ['right', 'bottom', 'left', 'top'],
                delay: { show: 500, hide: 100 }
            });
        }
    }

    $('.overflow-tooltip, input.form-control[type="text"]:not(.exclude-tooltip)').each(function () {
        const $input = $(this);

        // Wrap element in tooltip wrapper to support input disabled/enabled transitions
        $input.wrap('<span class="tooltip-wrapper"></span>');
        const tooltipTarget = $input.parent()[0];

        // Create tooltip from value on initial load
        createTooltipOnOverflow(this, tooltipTarget);

        $input.on("input paste", function () {
            const tooltipInstance = bootstrap.Tooltip.getInstance(tooltipTarget);
            if (tooltipInstance) {
                tooltipInstance.dispose();
            }

            // Update tooltip from value on input change
            createTooltipOnOverflow(this, tooltipTarget);
        });
    });


    if(abp.auth.isGranted('SettingManagement.UserInterface')) {
        // Toggle hidden export buttons for Ctrl+Alt+Shift+Z globally
        $(document).keydown(function (e) {
            if (e.ctrlKey && e.altKey &&
                e.shiftKey && e.key === 'Z') {
                // Toggle d-none class on elements with hidden-export class
                $('.zone-debugger-alert').each(function () {
                    if ($(this).hasClass('d-none')) {
                        $(this).removeClass('d-none').hide().fadeIn(500);
                    } else {
                        $(this).fadeOut(function () {
                            $(this).addClass('d-none');
                        });
                    }
                });


                // Prevent default behavior
                e.preventDefault();
                return false;
            }
        });
    }
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

