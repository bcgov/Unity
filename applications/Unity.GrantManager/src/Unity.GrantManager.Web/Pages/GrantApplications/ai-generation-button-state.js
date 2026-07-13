(function (global) {
    function getRequest(generationStatus) {
        if (generationStatus?.generationRequest) {
            return generationStatus.generationRequest;
        }

        if (generationStatus?.status || generationStatus?.isActive === true) {
            return generationStatus;
        }

        return null;
    }

    function resolveStatus(status) {
        return String(status ?? '').trim();
    }

    function extractRetryAfterSeconds(error) {
        const message = error?.responseJSON?.error?.message
            || error?.responseJSON?.message
            || error?.responseText
            || '';

        const match = String(message).match(/try again in\s+(\d+)\s+second/i);
        return match ? Number(match[1]) : 0;
    }

    function restoreButton($button, html) {
        global.AIGenerationButtonState.restore($button);
        $button.html(html).prop('disabled', false);
    }

    function restoreButtonForCooldownCheck($button, html) {
        global.AIGenerationButtonState.restore($button);
        $button
            .html(html)
            .attr('data-ai-cooldown-checking', '1')
            .attr('data-ai-rate-limit-disabled', '1')
            .prop('disabled', true);
    }

    function applyRateLimitState(generationStatus, options = {}) {
        const request = getRequest(generationStatus);
        global.applyAIRateLimitState?.(
            {
                isGenerating: generationStatus?.isGenerating === true || request?.isActive === true,
                retryAfterSeconds: Number(generationStatus?.retryAfterSeconds) || 0
            },
            { pollWhenGenerating: options.pollWhenGenerating === true }
        );
    }

    global.AIGenerationButtonState = {
        setGenerating($button) {
            global.setAIGenerationButtonsGenerating?.({ poll: false });
        },
        restore($button) {
            $button.removeClass('disabled');
        },
        restoreForCooldownCheck($button, html) {
            restoreButtonForCooldownCheck($button, html);
        },
        applyStatusState(generationStatus) {
            applyRateLimitState(generationStatus, { pollWhenGenerating: true });
        },
        handleQueueFailure(error) {
            const retryAfterSeconds = extractRetryAfterSeconds(error);
            if (retryAfterSeconds <= 0) {
                return false;
            }

            global.setAIGenerationButtonsCooldown?.(retryAfterSeconds);
            return true;
        },
        resolveStatus(status) {
            return resolveStatus(status);
        },
        monitor(options) {
            const intervalMs = options.intervalMs || 5000;
            const activeIntervalMs = options.activeIntervalMs || 1000;
            const activePollCount = options.activePollCount || 30;
            const maxFailures = options.maxFailures || 3;
            let timeoutId = null;
            let failures = 0;
            let pollCount = 0;

            const stop = () => {
                if (timeoutId) {
                    clearTimeout(timeoutId);
                    timeoutId = null;
                }
            };

            const scheduleNextPoll = () => {
                pollCount += 1;
                const nextIntervalMs = pollCount <= activePollCount
                    ? activeIntervalMs
                    : intervalMs;

                timeoutId = setTimeout(poll, nextIntervalMs);
            };

            const poll = () => {
                options.getStatus()
                    .done((generationStatus) => {
                        failures = 0;
                        const request = getRequest(generationStatus);
                        const status = resolveStatus(request?.status);

                        if (status === 'Failed') {
                            stop();
                            restoreButton(options.$button, options.originalHtml);
                            applyRateLimitState(generationStatus, { pollWhenGenerating: true });
                            options.onPollFailed?.(new Error(generationStatus?.failureReason || 'AI generation failed.'));
                            options.onFailed?.(request);
                            return;
                        }
                        if (!request) {
                            stop();
                            restoreButton(options.$button, options.originalHtml);
                            global.refreshAIRateLimitState?.();
                            options.onMissing?.();
                            return;
                        }

                        if (request.isActive !== true) {
                            stop();
                            restoreButtonForCooldownCheck(options.$button, options.originalHtml);
                            global.refreshAIRateLimitState?.();
                            options.onComplete?.(request);
                            return;
                        }

                        global.setAIGenerationButtonsGenerating?.({ poll: false });
                        scheduleNextPoll();
                    })
                    .fail((error) => {
                        failures += 1;
                        if (failures > maxFailures) {
                            stop();
                            restoreButton(options.$button, options.originalHtml);
                            options.onPollFailed?.(error);
                            return;
                        }

                        scheduleNextPoll();
                    });
            };

            stop();
            timeoutId = setTimeout(poll, options.initialDelayMs ?? 500);
            return { stop };
        },
    };

})(globalThis);
