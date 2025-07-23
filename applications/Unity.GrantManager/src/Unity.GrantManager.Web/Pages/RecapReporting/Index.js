

$(function () {

    const iframe = document.createElement('iframe');
    iframe.src =
        `http://127.0.0.1/?token=${encodeURIComponent('token')}`;
    iframe.style.width = '100%';
    iframe.style.height = '90vh';
    iframe.style.border = 'none';
    document.getElementById('container').appendChild(iframe);
});
