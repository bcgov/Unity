(async () => {

    token = "";
    await unity.grantManager.identity.jWTToken.generateJWTToken().then(function (returnedToken) { token = returnedToken; });

    const iframe = document.createElement('iframe');
    iframe.src = `http://localhost?token=${token}`; // TODO: Change to OpenShift URL
    iframe.style.width = '100%';
    iframe.style.height = '100%';
    iframe.style.border = 'none';

    document.getElementById('container').appendChild(iframe);
})();

