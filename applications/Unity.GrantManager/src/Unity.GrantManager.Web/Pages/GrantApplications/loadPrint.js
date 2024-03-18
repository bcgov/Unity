
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

                    setTimeout(function () {
                        window.print();
                    }, 1000);

                });
            
    }
    
