(function (global) {
    const generatingStyles = {
        'background-color': '#f1f3f5',
        'border-color': '#adb5bd',
        color: '#495057',
        opacity: '1',
    };

    function applyStyles($button, styles) {
        $button.css(styles);
    }

    function restoreButton($button, html) {
        global.AIGenerationButtonState.restore($button);
        $button.html(html).prop('disabled', false);
    }

    global.AIGenerationButtonState = {
        resolveStatus(status) {
            switch (Number(status)) {
                case 0:
                    return 'Queued';
                case 1:
                    return 'Running';
                case 2:
                    return 'Completed';
                case 3:
                    return 'Failed';
                default:
                    return '';
            }
        },
        setGenerating($button) {
            $button.removeAttr('data-ai-cooldown-active');
            $button.attr('data-ai-generating', '1');
            $button
                .html('<span class="ai-button-content"><span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span><span>Generating...</span></span>')
                .prop('disabled', true);
            applyStyles($button, generatingStyles);
        },
        restore($button) {
            $button.removeAttr('data-ai-generating');
            $button.css({
                'background-color': '',
                'border-color': '',
                color: '',
                opacity: '',
            }).removeClass('disabled');
        },
        monitor(options) {
            const intervalMs = options.intervalMs || 15000;
            const maxFailures = options.maxFailures || 3;
            let timeoutId = null;
            let failures = 0;

            const stop = () => {
                if (timeoutId) {
                    clearTimeout(timeoutId);
                    timeoutId = null;
                }
            };

            const poll = () => {
                options.getStatus()
                    .done((request) => {
                        failures = 0;
                        const status = this.resolveStatus(request?.status);

                        if (status === 'Failed') {
                            stop();
                            restoreButton(options.$button, options.originalHtml);
                            options.onFailed?.(request);
                            return;
                        }

                        if (!request || request.isActive === false || status === 'Completed') {
                            stop();
                            restoreButton(options.$button, options.originalHtml);
                            options.onComplete?.(request);
                            global.refreshAIRateLimitState?.();
                            return;
                        }

                        timeoutId = setTimeout(poll, intervalMs);
                    })
                    .fail((error) => {
                        failures += 1;
                        if (failures > maxFailures) {
                            stop();
                            restoreButton(options.$button, options.originalHtml);
                            options.onPollFailed?.(error);
                            return;
                        }

                        timeoutId = setTimeout(poll, intervalMs);
                    });
            };

            stop();
            timeoutId = setTimeout(poll, options.initialDelayMs ?? 500);
            return { stop };
        },
    };
})(globalThis);
