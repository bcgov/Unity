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
            return {
                data: result.sites
            };
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
                        className: 'data-table-header'
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
                            return '<button class="btn site-info-btn" type="button" onclick="openSiteInfoModal(\'' + data + '\',\'Edit Site\');"><i class="fl fl-edit"></i></button>';
                        },
                        orderable: false
                    }
                ],
            })
        );
    }

    setTimeout(function () { loadSiteInfoTable(); },1000);
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