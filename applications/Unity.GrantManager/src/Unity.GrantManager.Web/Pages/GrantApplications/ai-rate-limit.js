/* AB#32290 — shared per-user AI Generate button state.
 * Server owns active-generation and cooldown state; this module mirrors it
 * across every .ai-generate-btn so all AI surfaces behave consistently.
 */
(function () {
    const BUTTON_SELECTOR = '.ai-generate-btn';
    const ATTR_LABEL = 'data-original-label';
    const ATTR_COOLDOWN = 'data-ai-cooldown-active';
    const ATTR_CHECKING = 'data-ai-cooldown-checking';
    const ATTR_OWNED_DISABLED = 'data-ai-rate-limit-disabled';
    const ATTR_SHARED_GENERATING = 'data-ai-shared-generating';

    let countdownTimer = null;
    let statePollTimer = null;
    let lastFetchAt = 0;
    let remainingSeconds = 0;
    let currentState = { mode: 'checking' };

    function buttons() {
        return document.querySelectorAll(BUTTON_SELECTOR);
    }

    function rememberLabel(btn) {
        if (!btn.getAttribute(ATTR_LABEL)) {
            const label = btn.querySelector('.ai-button-content span:last-child') || btn;
            btn.setAttribute(ATTR_LABEL, label.textContent.trim());
        }
    }

    function setLabel(btn, text) {
        const label = btn.querySelector('.ai-button-content span:last-child');
        if (label) {
            label.textContent = text;
        } else {
            btn.textContent = text;
        }
    }

    function disable(btn, seconds) {
        btn.removeAttribute(ATTR_SHARED_GENERATING);
        rememberLabel(btn);
        if (!btn.disabled || btn.getAttribute(ATTR_OWNED_DISABLED) === '1') {
            btn.setAttribute(ATTR_OWNED_DISABLED, '1');
        }
        btn.setAttribute(ATTR_COOLDOWN, '1');
        btn.setAttribute('disabled', 'disabled');
        btn.classList.add('disabled');
        setLabel(btn, `Wait ${seconds}s`);
    }

    function restore(btn) {
        if (
            btn.getAttribute(ATTR_COOLDOWN) !== '1' &&
            btn.getAttribute(ATTR_SHARED_GENERATING) !== '1'
        ) {
            return;
        }

        btn.removeAttribute(ATTR_COOLDOWN);
        btn.removeAttribute(ATTR_SHARED_GENERATING);
        if (btn.getAttribute(ATTR_OWNED_DISABLED) === '1') {
            btn.removeAttribute(ATTR_OWNED_DISABLED);
            btn.removeAttribute('disabled');
        }
        btn.classList.remove('disabled');
        const original = btn.getAttribute(ATTR_LABEL);
        if (original) setLabel(btn, original);
    }

    function setChecking(btn) {
        if (btn.getAttribute(ATTR_CHECKING) === '1') {
            return;
        }

        btn.setAttribute(ATTR_CHECKING, '1');
        if (!btn.disabled || btn.getAttribute(ATTR_OWNED_DISABLED) === '1') {
            btn.setAttribute(ATTR_OWNED_DISABLED, '1');
            btn.setAttribute('disabled', 'disabled');
        }
    }

    function clearChecking(btn) {
        if (btn.getAttribute(ATTR_CHECKING) !== '1') {
            return;
        }

        btn.removeAttribute(ATTR_CHECKING);
        if (btn.getAttribute(ATTR_OWNED_DISABLED) === '1') {
            btn.removeAttribute(ATTR_OWNED_DISABLED);
            btn.removeAttribute('disabled');
        }
    }

    function disableUntilChecked() {
        currentState = { mode: 'checking' };
        renderState();
    }

    function clearCheckingButtons() {
        buttons().forEach(clearChecking);
    }

    function clearTimer() {
        if (countdownTimer) {
            clearInterval(countdownTimer);
            countdownTimer = null;
        }
    }

    function clearStatePollTimer() {
        if (statePollTimer) {
            clearTimeout(statePollTimer);
            statePollTimer = null;
        }
    }

    function setOwnedDisabled(btn) {
        if (!btn.disabled || btn.getAttribute(ATTR_OWNED_DISABLED) === '1') {
            btn.setAttribute(ATTR_OWNED_DISABLED, '1');
            btn.setAttribute('disabled', 'disabled');
        }
    }

    function applyGenerating(options = {}) {
        currentState = { mode: 'generating' };
        renderState();

        clearStatePollTimer();
        if (options.poll !== false) {
            statePollTimer = setTimeout(() => fetchState(true), 2000);
        }
    }

    function renderGenerating() {
        clearCheckingButtons();
        clearTimer();
        remainingSeconds = 0;
        buttons().forEach(btn => {
            rememberLabel(btn);
            btn.removeAttribute(ATTR_COOLDOWN);
            btn.setAttribute(ATTR_SHARED_GENERATING, '1');
            setOwnedDisabled(btn);
            btn.classList.add('disabled');
            setLabel(btn, 'Generating...');
        });
    }

    function applyCooldownToButtons() {
        buttons().forEach(b => disable(b, remainingSeconds));
    }

    function applyCooldown(seconds) {
        currentState = { mode: 'cooldown', seconds: Number(seconds) || 0 };
        renderState();
    }

    function renderCooldown(seconds) {
        clearCheckingButtons();
        clearTimer();
        clearStatePollTimer();
        remainingSeconds = seconds;
        if (remainingSeconds <= 0) {
            currentState = { mode: 'available' };
            buttons().forEach(restore);
            return;
        }
        applyCooldownToButtons();
        countdownTimer = setInterval(() => {
            remainingSeconds -= 1;
            currentState = { mode: 'cooldown', seconds: remainingSeconds };
            if (remainingSeconds <= 0) {
                clearTimer();
                currentState = { mode: 'available' };
                buttons().forEach(restore);
                return;
            }
            applyCooldownToButtons();
        }, 1000);
    }

    function renderState() {
        if (currentState.mode === 'generating') {
            renderGenerating();
            return;
        }

        if (currentState.mode === 'cooldown') {
            renderCooldown(currentState.seconds);
            return;
        }

        if (currentState.mode === 'checking') {
            buttons().forEach(setChecking);
            return;
        }

        clearCheckingButtons();
        clearTimer();
        clearStatePollTimer();
        buttons().forEach(restore);
    }

    function retryStateFetch() {
        clearStatePollTimer();
        statePollTimer = setTimeout(() => fetchState(true), 2000);
    }

    function handleStateFetchFailure() {
        renderState();
        retryStateFetch();
    }

    async function fetchState(force = false) {
        // Throttle to once per second to avoid hammering on chained clicks.
        const now = Date.now();
        if (!force && now - lastFetchAt < 1000) return;
        lastFetchAt = now;
        try {
            const res = await fetch('/api/app/ai/rate-limit/state', {
                credentials: 'same-origin',
                headers: { Accept: 'application/json' },
            });
            if (!res.ok) {
                handleStateFetchFailure();
                return;
            }
            const data = await res.json();
            applyRateLimitState(data);
        } catch (_) {
            // Best-effort; the server is the source of truth.
            handleStateFetchFailure();
        }
    }

    function applyRateLimitState(data, options = {}) {
        if (data?.isGenerating === true) {
            applyGenerating({ poll: options.pollWhenGenerating !== false });
            return;
        }

        applyCooldown(Number(data?.retryAfterSeconds) || 0);
    }

    globalThis.syncAIRateLimitButtons = () => {
        renderState();
        fetchState(true);
    };
    globalThis.setAIGenerationButtonsGenerating = applyGenerating;
    globalThis.applyAIRateLimitState = applyRateLimitState;
    globalThis.refreshAIRateLimitState = globalThis.syncAIRateLimitButtons;

    document.addEventListener('click', (e) => {
        const btn = e.target.closest(BUTTON_SELECTOR);
        if (!btn) return;
        applyGenerating({ poll: false });
    });

    document.addEventListener('DOMContentLoaded', () => {
        disableUntilChecked();
        fetchState();
    });

    if (document.readyState !== 'loading') {
        disableUntilChecked();
        fetchState();
    }
})();
