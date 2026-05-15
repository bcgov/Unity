/* AB#32290 — per-user 60s cooldown for AI Generate buttons.
 * Server stamps the cooldown; this module only mirrors that state in the UI.
 * Strategy: on load, fetch the user's remaining seconds and disable every
 * .ai-generate-btn with a countdown. After any generate click resolves we
 * re-fetch (a successful click sets a new cooldown; a failed/blocked click
 * may report the existing one). KISS — no per-button logic, no mutex.
 */
(function () {
    const BUTTON_SELECTOR = '.ai-generate-btn';
    const ATTR_LABEL = 'data-original-label';
    const ATTR_COOLDOWN = 'data-ai-cooldown-active';
    const ATTR_CHECKING = 'data-ai-cooldown-checking';
    const ATTR_OWNED_DISABLED = 'data-ai-rate-limit-disabled';

    let countdownTimer = null;
    let lastFetchAt = 0;
    let remainingSeconds = 0;

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
        if (btn.getAttribute('data-ai-generating') === '1') {
            return;
        }

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
        if (btn.getAttribute(ATTR_COOLDOWN) !== '1') return;
        btn.removeAttribute(ATTR_COOLDOWN);
        if (btn.getAttribute(ATTR_OWNED_DISABLED) === '1') {
            btn.removeAttribute(ATTR_OWNED_DISABLED);
            btn.removeAttribute('disabled');
        }
        btn.classList.remove('disabled');
        const original = btn.getAttribute(ATTR_LABEL);
        if (original) setLabel(btn, original);
    }

    function setChecking(btn) {
        if (
            btn.getAttribute(ATTR_CHECKING) === '1' ||
            btn.getAttribute('data-ai-generating') === '1'
        ) {
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
        buttons().forEach(setChecking);
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

    function applyCooldownToButtons() {
        buttons().forEach(b => disable(b, remainingSeconds));
    }

    function applyCooldown(seconds) {
        clearCheckingButtons();
        clearTimer();
        remainingSeconds = Number(seconds) || 0;
        if (remainingSeconds <= 0) {
            buttons().forEach(restore);
            return;
        }
        applyCooldownToButtons();
        countdownTimer = setInterval(() => {
            remainingSeconds -= 1;
            if (remainingSeconds <= 0) {
                clearTimer();
                buttons().forEach(restore);
                return;
            }
            applyCooldownToButtons();
        }, 1000);
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
                clearCheckingButtons();
                return;
            }
            const data = await res.json();
            applyCooldown(Number(data.retryAfterSeconds) || 0);
        } catch (_) {
            // Best-effort; the server is the source of truth.
            clearCheckingButtons();
        }
    }

    globalThis.syncAIRateLimitButtons = () => {
        disableUntilChecked();
        fetchState(true);
    };
    globalThis.refreshAIRateLimitState = globalThis.syncAIRateLimitButtons;

    document.addEventListener('click', (e) => {
        const btn = e.target.closest(BUTTON_SELECTOR);
        if (!btn) return;
        // Re-check shortly after the click so a successful generate immediately
        // shows the fresh 60s cooldown.
        setTimeout(fetchState, 250);
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
