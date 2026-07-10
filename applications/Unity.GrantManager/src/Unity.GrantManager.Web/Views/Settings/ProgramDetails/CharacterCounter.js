(function ($) {
    window.UnityCharacterCounter = {
        init: function (containerSelector) {
            const $container = $(containerSelector || document);

            $container.find('[data-char-count]').each(function () {
                const $input = $(this);
                const maxLength = Number.parseInt($input.attr('data-char-count'), 10);

                if (Number.isNaN(maxLength)) {
                    return;
                }

                // Create wrapper with relative positioning (idempotent)
                if ($input.data('unityCharCounterInitialized')) {
                    return;
                }
                $input.data('unityCharCounterInitialized', true);

                if (!$input.parent().hasClass('char-counter-wrapper')) {
                    $input.wrap('<div class="char-counter-wrapper"></div>');
                }

                let $counter = $input.siblings('.char-counter');
                if (!$counter.length) {
                    $counter = $('<div class="char-counter"></div>').insertAfter($input);
                }
                function updateCounter() {
                    const currentLength = $input.val().length;
                    $counter.text(currentLength + '/' + maxLength);

                    const percentageUsed = (currentLength / maxLength) * 100;
                    $counter.toggleClass('char-counter-critical', percentageUsed >= 95);
                    $counter.toggleClass('char-counter-warning', percentageUsed >= 80 && percentageUsed < 95);
                }

                $input.on('input', updateCounter);
                updateCounter();
            });
        }
    };
})(jQuery);
