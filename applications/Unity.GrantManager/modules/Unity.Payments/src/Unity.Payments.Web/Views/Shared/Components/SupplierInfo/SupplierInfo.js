$(function () {
    const l = abp.localization.getResource('Payments');    

    let dataTable;
    function loadSiteInfoTable() {
        let inputAction = function (requestData, dataTableSettings) {
            const correlationId = $("#SupplierCorrelationId").val();
            const correlationProvider = $("#SupplierCorrelationProvider").val();
            const includeDetails = true;
            return { correlationId, correlationProvider, includeDetails };
        }
        let responseCallback = function (result) {
            let response = { data: [] };
            if(result != null && result.sites != null) {
                response.data = result.sites
            } 
            return response;
        };
                
        dataTable = $('#SiteInfoTable').DataTable(
            abp.libs.datatables.normalizeConfiguration({
                serverSide: false,
                order: [[2, 'asc']],
                searching: false,
                paging: false,
                select: false,
                info: false,
                scrollX: true,
                ajax: abp.libs.datatables.createAjax(
                    unity.payments.suppliers.supplier.getByCorrelation, inputAction, responseCallback
                ),
                columnDefs: [
                    {
                        title: l('ApplicantInfoView:ApplicantInfo.SiteInfo:SiteNumber'),
                        data: 'number',
                        className: 'data-table-header',
                    },
                    {
                        title: l('ApplicantInfoView:ApplicantInfo.SiteInfo:PayGroup'),
                        data: 'paymentGroup',
                        className: 'data-table-header',
                        render: function (data) {
                            switch (data) {
                                case 1:
                                    return 'EFT';
                                case 2:
                                    return 'Cheque';
                                default:
                                    return 'Unknown PaymentGroup';
                            }
                        },
                    },
                    {
                        title: l('ApplicantInfoView:ApplicantInfo.SiteInfo:MailingAddress'),
                        data: 'addressLine1',
                        className: 'data-table-header',
                        render: function (data, type, full, meta) {
                            return nullToEmpty(full.addressLine1) + ' ' + nullToEmpty(full.addressLine2) + " " + nullToEmpty(full.addressLine3) + " " + nullToEmpty(full.city) + " " + nullToEmpty(full.province) + " " + nullToEmpty(full.postalCode);
                        },
                    }
                ],
            })
        );
    }

    setTimeout(function () { loadSiteInfoTable(); },1000);
    $('#nav-organization-info-tab').one('click', function () {
        dataTable?.columns?.adjust();
    });

    PubSub.subscribe(
        'refresh_sites_list',
        (msg, data) => {
            dataTable.ajax.reload();
            dataTable?.columns?.adjust();
        }
    );

    PubSub.subscribe(
        'reload_sites_list',
        (msg, data) => {
            loadSiteInfoTable();
        }
    );

    function nullToEmpty(value) {
        return value === null ? '' : value;
    }
});

let siteInfoModal = new abp.ModalManager({
    viewUrl: '../SiteInfo/SiteInfoModal'
});

siteInfoModal.onResult(function () {
    PubSub.publish('refresh_sites_list');
    abp.notify.success(
        'Site Information is successfully saved.',
        'Site Information'
    );
});

function openSiteInfoModal(siteId, actionType) {    
    const applicantId = $("#ApplicantInfoViewApplicantId").val(); 
    const supplierNumber = encodeURIComponent($("#SupplierNumber").val());
    const supplierId = $("#SupplierId").val();

    siteInfoModal.open({
        applicantId: applicantId,
        siteId: siteId,
        actionType: actionType,
        supplierNumber: supplierNumber,
        supplierId: supplierId
    });
}