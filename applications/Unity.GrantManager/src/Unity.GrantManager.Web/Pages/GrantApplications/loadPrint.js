
function executeOperations(data) {
  
                Formio.createForm(
                    document.getElementById('new-rendering'),
                    data.version.schema, {
                    readOnly: true,
                    renderMode: 'form',
                    flatten: true,
                }
                ).then(async function (form) {
                    await form.setSubmission(data.submission.submission);
                    $('button[disabled="disabled"]').hide();
                    disableLinks();

                    setTimeout(function () {
                        window.print();
                    }, 1000);

                });
            
}
function disableLinks() {
    let links = document.querySelectorAll('a');
    links.forEach(function (link) {
        link.addEventListener('click', function (event) {
            event.preventDefault();
        });
        link.style.pointerEvents = 'none'; // Optionally style the link to indicate it's disabled
        link.style.color = 'gray'; // Optionally change the color to indicate it's disabled
    });
}
    
