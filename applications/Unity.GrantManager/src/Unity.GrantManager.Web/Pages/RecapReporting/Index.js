(async () => {

    const token = await unity.grantManager.identity.jwtToken.generateJWTToken();
    const iframe = document.createElement('iframe');
    iframe.src = `${window.reportingAiUrl}?token=${token}`;
    iframe.style.width = '100%';
    iframe.style.height = '100%';
    iframe.style.border = 'none';

    document.getElementById('container').appendChild(iframe);
})();