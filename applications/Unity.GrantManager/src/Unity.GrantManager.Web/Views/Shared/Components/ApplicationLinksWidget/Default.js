$(function () {
    const l = abp.localization.getResource('GrantManager');
    const nullPlaceholder = '—';
    let selectedApplicationId = decodeURIComponent($("#DetailsViewApplicationId").val());    

    let inputAction = function (requestData, dataTableSettings) {
        return selectedApplicationId;
    };

    let responseCallback = function (result) {
        if (result) {
            // Filter out the current application from the results
            let filteredResult = result.filter(function(item) {
                return item.applicationId !== selectedApplicationId;
            });
            
            // Update the data-count attribute
            $('.links-container').attr('data-count', filteredResult.length);
            
            // Update the tab count directly
            setTimeout(() => {
                const tag = $('.links-container').data('linkscounttag');
                const count = $('.links-container').attr('data-count');
                $('#' + tag).html(count);
            }, 50);
            
            return {
                data: filteredResult
            };
        }

        $('.links-container').attr('data-count', 0);
        $('#application_links_count').html(0);
        return {
            data: []
        };
    };

    const dataTable = $('#ApplicationLinksTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            order: [[1, 'asc']],
            searching: false,
            paging: false,
            select: false,
            info: false,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.grantApplications.applicationLinks.getListByApplication, inputAction, responseCallback
            ),
            drawCallback: function() {
                this.api().columns.adjust();
            },
            columnDefs: [
                {
                    title: l('ApplicationLinks:Category'),
                    data: 'category',
                    width: '30%'
                },
                {
                    title: l('ApplicationLinks:ID'),
                    data: 'referenceNumber',
                    width: '20%',
                    render: function (data, type, full) {
                        if (type === 'display') {
                            return '<a href="/GrantApplications/Details?ApplicationId=' + full.applicationId + '" target="_self" class="link-primary text-decoration-underline">' + data + '</a>';
                        }
                        return data;
                    }
                },
                {
                    title: l('ApplicationLinks:Status'),
                    data: 'applicationStatus',
                    width: '25%'
                },
                {
                    title: l('ApplicationLinks:LinkType'),
                    data: 'linkType',
                    width: '20%',
                    render: function (data) {
                        return data ?? nullPlaceholder;
                    }
                },
                {
                    title: '',
                    data: 'id',
                    width: '5%',
                    className: 'text-center',
                    render: function (data, type, full, meta) {
                        return '<button class="btn btn-link p-0 delete-link-btn" data-link-id="' + data + '" title="Delete Link" style="color: #0066cc; text-decoration: none;"><i class="fa fa-times"></i></button>';
                    },
                    orderable: false
                }
            ],
        })
    );

    // Handle delete button clicks using event delegation
    $('#ApplicationLinksTable').on('click', '.delete-link-btn', function(e) {
        e.preventDefault();
        var linkId = $(this).data('link-id');
        
        abp.message.confirm(
            'Are you sure you want to delete this application link?',
            'Delete Application Link',
            function (isConfirmed) {
                if (isConfirmed) {
                    unity.grantManager.grantApplications.applicationLinks.deleteWithPair(linkId)
                        .then(function () {
                            abp.notify.success('Application link deleted successfully.');
                            dataTable.ajax.reload();
                        })
                        .catch(function (error) {
                            abp.notify.error('Error deleting application link.');
                        });
                }
            }
        );
    });

    let applicationLinksModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'ApplicationLinks/ApplicationLinksModal',
    });

    $('body').on('click','#addLinksRecordsBtn',function(e){
        e.preventDefault();
        applicationLinksModal.open({
            applicationId: selectedApplicationId,
        });
    });

    applicationLinksModal.onOpen(function () {
        let linkInput = new LinksInput({
            selector: 'SelectedApplications',
            duplicate: false,
            max: 50
        });
        let suggestionsArray = [];
        let selectedApplications = $('#SelectedApplications').val();
        let allApplications = $('#AllApplications').val();
        if (allApplications) {
            suggestionsArray = allApplications.split(',');
        }
        linkInput.setSuggestions(suggestionsArray);

        if(selectedApplications.length) {
            linkInput.addData(selectedApplications.split(','));
        }
    });

    applicationLinksModal.onResult(function () {
        abp.notify.success(
            'The application links have been successfully updated.',
            'Application Links'
        );
        dataTable.ajax.reload();
       
    });
});
