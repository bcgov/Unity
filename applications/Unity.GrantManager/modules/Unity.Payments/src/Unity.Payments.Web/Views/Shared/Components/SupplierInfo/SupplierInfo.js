$(function () {
    const l = abp.localization.getResource('Payments');
    let inputAction = function (requestData, dataTableSettings) {
        const urlParams = new URL(window.location.toLocaleString()).searchParams;
        const applicationId = urlParams.get('ApplicationId');
        return applicationId;
    }
    let responseCallback = function (result) {
        

        return {
            data: result
        };
    };

    const dataTable = $('#SiteInfoTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            order: [[2, 'asc']],
            searching: false,
            paging: false,
            select: false,
            info: false,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(
                unity.payments.supplierInfo.supplierInfo.getSites, inputAction, responseCallback
            ),
            columnDefs: [
                {
                    title: l('ApplicantInfoView:ApplicantInfo.SiteInfo:SiteNumber'),
                    data: 'number',
                    className: 'data-table-header',
                },
                {
                    title: l('ApplicantInfoView:ApplicantInfo.SiteInfo:PayGroup'),
                    data: 'payGroup',
                    className: 'data-table-header',
                },
                {
                    title: l('ApplicantInfoView:ApplicantInfo.SiteInfo:MailingAddress'),
                    data: 'addressLine1',
                    className: 'data-table-header',
                    render: function (data, type, full, meta) {
                        return nullToEmpty(full.addressLine1) + ' ' + nullToEmpty(full.addressLine2) + " " + nullToEmpty(full.addressLine3) + " " + nullToEmpty(full.city) + " " + nullToEmpty(full.province) + " " + nullToEmpty(full.postalCode);
                    },
                },
                {
                    title: '',
                    data: 'id',
                    render: function (data) {
                        return '<button class="btn site-info-btn" type="button" onclick="openSiteInfoModal(\''+data+'\',\'Edit Site\');"><i class="fl fl-edit"></i></button>';
                    },
                    orderable: false
                }
            ],
        })
    );
  
    $('#nav-organization-info-tab').one('click', function () {
        dataTable.columns.adjust();
    });

    PubSub.subscribe(
        'refresh_sites_list',
        (msg, data) => {
            dataTable.ajax.reload();
            dataTable.columns.adjust();
        }
    );

    function nullToEmpty(value) {
        return value === null ? '' : value;
    }
});

let siteInfoModal = new abp.ModalManager({
    viewUrl: '../SiteInfo/SiteInfoModal'
});
function openSiteInfoModal(siteId, actionType) {
    siteInfoModal.open({
        siteId: JSON.stringify(siteId),
        actionType: actionType
    });
}