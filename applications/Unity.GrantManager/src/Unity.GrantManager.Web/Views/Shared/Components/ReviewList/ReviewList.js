$(function () {
    console.log('Script loaded');
    const l = abp.localization.getResource('GrantManager');
    let inputAction = function (requestData, dataTableSettings) {
        const urlParams = new URL(window.location.toLocaleString()).searchParams;
        const applicationId = urlParams.get('ApplicationId');
        return applicationId;
    }
    
    let responseCallback = function (result) {
        // your custom code.
        console.log(result)

        return {
            data: result
        };
    };

    const reviewListTable = $('#ReviewListTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: true,
            order: [[1, 'asc']],
            searching: false,
            paging: false,
            select: true,
            info: false,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.assessments.assessment.getList, inputAction, responseCallback
            ),
            columnDefs: [
                {
                    title: '',
                    data: 'id',
                    visible : false,
                },
                {
                    title: '',
                    render: function (data) {
                        return '<i class="fl fl-review-user" ></i>';
                    }
                },
                {
                    title: l('ReviewerList:ReviewerName'),
                    data: 'reviewerName',
                    className: 'data-table-header',
                    render: function (data) {
                        return data || 'Reviewer Name';
                    },
                },
                {
                    title: l('ReviewerList:StartDate'),
                    data: 'startDate',
                    className: 'data-table-header',
                    render: function (data) {
                        return  data ? new Date(data).toDateString() : '';
                    },
                },
                {
                    title: l('ReviewerList:EndDate'),
                    data: 'endDate',
                    className: 'data-table-header',
                    render: function (data) {
                        return data ? new Date(data).toDateString() : '';
                    },
                },
                {
                    title: l('ReviewerList:Status'),
                    data: 'status',
                    className: 'data-table-header',
                    render: function (data) {
                        return data;
                    },
                },
                {
                    title: l('ReviewerList:Recommended'),
                    data: 'approvalRecommended',
                    className: 'data-table-header',
                    render: function (data) {
                        if (data !== null) {
                            return data === true ? 'Recommended for Approval' : 'Recommended for Denial'
                        } else {
                            return '';
                        }                        
                    },
                },
            ],
        })
    );

    reviewListTable.on('select', function (e, dt, type, indexes) {
        if (type === 'row') {
            let selectedData = reviewListTable.row(indexes).data();
            console.log('Selected Data:', selectedData);
            PubSub.publish('select_application_review', selectedData);
            e.currentTarget.classList.toggle('selected');
        }
    });

  

    reviewListTable.on('deselect', function (e, dt, type, indexes) {
        if (type === 'row') {
            let deselectedData = reviewListTable.row(indexes).data();
            PubSub.publish('select_application_review', null);
            e.currentTarget.classList.toggle('selected');
        }
    });


    const refresh_review_list_subscription = PubSub.subscribe(
        'refresh_review_list',
         (msg, data) => {
             reviewListTable.ajax.reload(function (json) {
                 if (data) {
                     //var row = reviewListTable.row(0).select();
                     let indexes = reviewListTable.rows().eq(0).filter(function (rowIdx) {
                         return reviewListTable.cell(rowIdx, 0).data() === data ? true : false;
                     });

                     reviewListTable.row(indexes).select();
                 }
             });
        
        }
    );
});
