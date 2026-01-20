(async () => {
    const token = await unity.grantManager.identity.jwtToken.generateJWTToken();
    const iframe = document.createElement('iframe');

    iframe.style.width = '100%';
    iframe.style.height = '100%';
    iframe.style.border = 'none';

    const targetOrigin = new URL(window.reportingAiUrl).origin;

    // Listen for "READY" message from iframe before sending auth token
    const messageHandler = (event) => {
        if (event.origin !== targetOrigin) return;
        if (event.data?.type === 'READY') {
            try {
                iframe.contentWindow.postMessage(
                    { type: 'AUTH_TOKEN', token: token },
                    targetOrigin
                );
            } catch (error) {
                console.error('Failed to send authentication token to AI Reporting iframe:', error);
            }
            window.removeEventListener('message', messageHandler);
        }
    };

    window.addEventListener('message', messageHandler);

    iframe.onerror = () => {
        console.error('Failed to load AI Reporting iframe');
        window.removeEventListener('message', messageHandler);
    };

    iframe.src = window.reportingAiUrl;
    document.getElementById('container').appendChild(iframe);
})();


