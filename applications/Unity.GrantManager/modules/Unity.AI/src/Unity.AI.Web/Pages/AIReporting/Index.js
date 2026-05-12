const showInitializationError = (container, message, error) => {
    console.error(message, error);
    container.textContent = message;
};
const reportingAiUrl = globalThis.reportingAiUrl;
const container = document.getElementById('container');

const initializeAIReporting = async () => {
    if (!container) {
        return;
    }

    if (!reportingAiUrl) {
        showInitializationError(container, 'AI Reporting is not configured.');
        return;
    }

    let reportingUrl;
    try {
        reportingUrl = new URL(reportingAiUrl);
    } catch (error) {
        showInitializationError(container, 'AI Reporting is not configured correctly.', error);
        return;
    }

    let token;
    try {
        token = await unity.grantManager.identity.jwtToken.generateJWTToken();
    } catch (error) {
        showInitializationError(container, 'Failed to initialize AI Reporting. Please refresh the page and try again.', error);
        return;
    }

    const iframe = document.createElement('iframe');

    iframe.style.width = '100%';
    iframe.style.height = '100%';
    iframe.style.border = 'none';

    const targetOrigin = reportingUrl.origin;

    const messageHandler = (event) => {
        if (event.origin !== targetOrigin) {
            return;
        }

        if (event.data?.type === 'READY') {
            try {
                iframe.contentWindow.postMessage(
                    { type: 'AUTH_TOKEN', token },
                    targetOrigin
                );
            } catch (error) {
                console.error('Failed to send authentication token to AI Reporting iframe:', error);
            }

            globalThis.removeEventListener('message', messageHandler);
        }
    };

    globalThis.addEventListener('message', messageHandler);

    iframe.onerror = () => {
        console.error('Failed to load AI Reporting iframe');
        globalThis.removeEventListener('message', messageHandler);
    };

    iframe.src = reportingUrl.href;
    container.appendChild(iframe);
};

initializeAIReporting();
