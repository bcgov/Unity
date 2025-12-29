(async () => {

    const token = await unity.grantManager.identity.jwtToken.generateJWTToken();
    const iframe = document.createElement('iframe');
    iframe.src = window.reportingAiUrl; // No token in URL - sent via postMessage
    iframe.style.width = '100%';
    iframe.style.height = '100%';
    iframe.style.border = 'none';

    // Wait for iframe to load before sending token securely
    iframe.onload = () => {
        const targetOrigin = new URL(window.reportingAiUrl).origin;
        iframe.contentWindow.postMessage(
            { type: 'AUTH_TOKEN', token: token },
            targetOrigin // Specify target origin for security
        );
    };

    document.getElementById('container').appendChild(iframe);
})();


