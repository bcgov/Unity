$(function () {
    let inputAction = function (requestData, dataTableSettings) {
        const urlParams = new URL(window.location.toLocaleString()).searchParams;
        const applicationId = urlParams.get('ApplicationId');
        return applicationId;
    }
    let responseCallback = function (result) {
        if (result) {
            setTimeout(function () {
                PubSub.publish('update_application_attachment_count', { chefs: result.length });
            }, 10);
        }
        return {
            data: result
        };
    };
    const dataTable = $('#ChefsAttachmentsTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            order: [[2, 'asc']],
            searching: false,
            paging: false,
            select: false,
            info: false,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.grantApplications.attachment.getApplicationChefsFileAttachments, inputAction, responseCallback
            ),
            columnDefs: [
                {
                    title: '<i class="fl fl-paperclip" ></i>',
                    render: function (data) {
                        return '<i class="fl fl-paperclip" ></i>';
                    },
                    orderable: false
                },
                {
                    title: l('AssessmentResultAttachments:DocumentName'),
                    data: 'name',
                    className: 'data-table-header',
                },
                {
                    title: '',
                    data: 'chefsFileId',
                    render: function (data, type, full, meta) {
                        let html = '<a href="/api/app/attachment/chefs/' + encodeURIComponent(full.chefsSumbissionId) + '/download/' + encodeURIComponent(data) + '/' + encodeURIComponent(full.name) + '" target = "_blank" download = "' + full.name + '" >';
                        html += '<button class="btn" type="button"><i class="fl fl-download"></i><span>Download</span></button></a>';
                        return html;
                    },
                    orderable: false
                }
            ],
        })
    );

    $('#resyncSubmissionAttachments').click(function () {
        let applicationId = document.getElementById('AssessmentResultViewApplicationId').value;
        try {
            unity.grantManager.grantApplications.attachment
                .resyncSubmissionAttachments(applicationId)
                .done(function () {
                    abp.notify.success(
                        'Submission Attachment/s has been resynced.'
                    );
                    dataTable.ajax.reload();
                    dataTable.columns.adjust();
                });
        }
        catch (error) {
            console.log(error);
        }
    });

    $('#attachments-tab').one('click', function () {
        dataTable.columns.adjust();
    });

    function extractFileName(url) {
        const path = new URL(url).pathname;
        return path.split('/').pop();
    }
   
    $('#downloadAll').click(function () {
        /*const zip = new JSZip();*/
        const chefsAttactmentsTable = document.getElementById('ChefsAttachmentsTable');
        const anchorTags = chefsAttactmentsTable.querySelectorAll('a');
        const hrefValues = [];
        anchorTags.forEach(anchor => {
            hrefValues.push(anchor.href);
        });

        try {
            // Loop through all the file URLs
            for (const fileUrl of hrefValues) {
                const fileName = decodeURIComponent(extractFileName(fileUrl));  // Get the file name from the URL
                fetch(fileUrl).then(response => {
                    if (!response.ok) {
                        throw new Error('Network response was not ok');
                    }
                    return response.blob();
                })

                    //This downloads each file one by one
                    .then(blob => {
                        let url = window.URL.createObjectURL(blob);
                        let a = document.createElement('a');
                        a.style.display = 'none';
                        a.href = url;
                        a.download = fileName;
                        document.body.appendChild(a);
                        a.click();
                        window.URL.revokeObjectURL(url);
                    })
                    .catch(error => {
                        console.error('There was a problem with the fetch operation:', error);
                    });

                // Add file to the zip
               /* zip.file(fileName, blob); */
            }

            // Once all files are added, generate the zip file
            //zip.generateAsync({ type: 'blob' }).then(function (content) {
            //    // Create a link to download the zip file
            //    const link = document.createElement('a');
            //    link.href = URL.createObjectURL(content);
            //    link.download = 'files.zip'; // The name of the downloaded zip file

            //    // Trigger the download
            //    link.click();
            //}).catch(error => {
            //            console.error('Error generating ZIP:', error);
            //    });
        } catch (error) {
            console.error('Error downloading files: ', error);
        }



        //zip.generateAsync({ type: "blob" })
        //    .then(function (content) {
        //        const link = document.createElement('a');
        //        link.href = URL.createObjectURL(content);
        //        link.download = 'submissionAttachments.zip';
        //        link.click();
        //    });



        //const fileList = [
        //    { filename: 'sample budget form.pdf', url: 'https://localhost:44342/api/app/attachment/chefs/a7969aa0-f780-4d0a-8cdc-2e243bb1f1ba/download/3f4b94db-6f03-4fe1-a816-b8319a2489d0/sample%20budget%20form.pdf', id: 123 },
        //    { filename: 'Test Direct Deposit application form.pdf', url: 'https://localhost:44342/api/app/attachment/chefs/a7969aa0-f780-4d0a-8cdc-2e243bb1f1ba/download/b672745e-1450-4f68-8577-d5e2fd6e7dcd/Test%20Direct%20Deposit%20application%20form.pdf', id: 456 },
        //    // ... more file objects
        //];

        //downloadFilesAsZip(filePaths, 'TestZip');
        //worker.postMessage(filePaths);

        //worker.onmessage = (event) => {
        //    const { filename, blob } = event.data;
        //    zip.file(filename, blob);
        //};

        //worker.onerror = (event) => {
        //    console.error('Error fetching file:', event.data);
        //};

        //worker.onmessage = (event) => {
        //    if (event.data === 'all_files_added') {
        //        zip.generateAsync({ type: "blob" })
        //            .then(function (content) {
        //                const link = document.createElement('a');
        //                link.href = URL.createObjectURL(content);
        //                link.download = 'my_archive.zip';
        //                link.click();
        //            })
        //            .catch(error => {
        //                console.error('Error generating ZIP:', error);
        //            });
        //        }
        //    };

          


    });

});