(function (global) {
    function request(url, type, data = null, contentType = null) {
        const options = { url, type };
        if (data !== null) {
            options.data = data;
        }
        if (contentType) {
            options.contentType = contentType;
        }

        return abp.ajax(options);
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
        global.applyAIRateLimitState?.(
            {
                isGenerating: generationStatus?.isGenerating === true,
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
                        const request = generationStatus?.generationRequest;
                        const status = String(request?.status ?? '').trim();

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
                            applyRateLimitState(generationStatus, { pollWhenGenerating: true });
                            options.onComplete?.(request);
                            return;
                        }

                        applyRateLimitState(generationStatus);
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

    global.AIGenerationApi = {
        queueApplicationAnalysis(applicationId) {
            return request(
                `/api/app/ai/generation/application-analysis?applicationId=${encodeURIComponent(applicationId)}`,
                'POST'
            );
        },
        queueApplicationScoring(applicationId) {
            return request(
                `/api/app/ai/generation/application-scoring?applicationId=${encodeURIComponent(applicationId)}`,
                'POST'
            );
        },
        queueAttachmentSummary(input) {
            return request(
                '/api/app/ai/generation/attachment-summary',
                'POST',
                JSON.stringify(input),
                'application/json'
            );
        },
        getStatus(applicationId, operationType) {
            return request(
                `/api/app/ai/generation/status?applicationId=${encodeURIComponent(applicationId)}&operationType=${encodeURIComponent(operationType)}`,
                'GET'
            );
        },
    };
})(globalThis);
