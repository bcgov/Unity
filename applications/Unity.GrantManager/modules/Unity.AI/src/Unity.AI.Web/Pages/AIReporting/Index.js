const showInitializationError = (container, message, error) => {
    console.error(message, error);
    container.textContent = message;
};
const reportingAiUrl = globalThis.reportingAiUrl;
const container = document.getElementById('container');

const buildReportingIframe = (reportingUrl, token) => {
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
    return iframe;
};

if (container) {
    if (!reportingAiUrl) {
        showInitializationError(container, 'AI Reporting is not configured.');
    } else {
        let reportingUrl;
        try {
            reportingUrl = new URL(reportingAiUrl);
        } catch (error) {
            reportingUrl = null;
            showInitializationError(container, 'AI Reporting is not configured correctly.', error);
        }

        if (reportingUrl) {
            const initializeReporting = async () => {
                try {
                    const token = await unity.grantManager.identity.jwtToken.generateJWTToken();
                    container.appendChild(buildReportingIframe(reportingUrl, token));
                } catch (error) {
                    showInitializationError(container, 'Failed to initialize AI Reporting. Please refresh the page and try again.', error);
                }
            };

            initializeReporting();
        }
    }
}
