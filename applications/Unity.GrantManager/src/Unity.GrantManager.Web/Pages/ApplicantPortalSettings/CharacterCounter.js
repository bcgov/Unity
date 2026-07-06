(function ($) {
    window.UnityCharacterCounter = {
        init: function (containerSelector) {
            const $container = $(containerSelector || document);
            
            $container.find('[data-char-count]').each(function () {
                const $input = $(this);
                const maxLength = parseInt($input.attr('data-char-count'), 10);
                
                if (isNaN(maxLength)) {
                    return;
                }

                // Create wrapper with relative positioning
                const $wrapper = $('<div class="char-counter-wrapper"></div>');
                $input.wrap($wrapper);
                
                const $counter = $('<div class="char-counter"></div>')
                    .insertAfter($input);

                function updateCounter() {
                    const currentLength = $input.val().length;
                    $counter.text(currentLength + '/' + maxLength);
                    
                    const percentageUsed = (currentLength / maxLength) * 100;
                    $counter.toggleClass('char-counter-warning', percentageUsed >= 80);
                    $counter.toggleClass('char-counter-critical', percentageUsed >= 95);
                }

                $input.on('input', updateCounter);
                updateCounter();
            });
        }
    };
})(jQuery);
