$(function () {
    let dataTable;
    const l = abp.localization.getResource('Payments');

    const UIElements = {
        navOrgInfoTab: $('#nav-organization-info-tab'),
        applicantId: $("#ApplicantId"),
        siteId: $("#SiteId")
    };

    function init() {
        $(document).ready(function () {
            loadSiteInfoTable();
            bindUIEvents();
        });
    }

    init();

    function bindUIEvents() {
        UIElements.navOrgInfoTab.one('click', function () { 
            if (dataTable) {
                dataTable.columns.adjust(); 
            }
        });
    }

    function loadSiteInfoTable() {
        let dt = $('#SiteInfoTable');
        let dataTable;

        let inputAction = function() {
            const supplierId = $("#SupplierId").val();
            return String(supplierId);
        }

        let responseCallback = function (result) {
            return {
                recordsTotal: result.totalCount,
                recordsFiltered: result.items.length,
                data: result.items
            };
        };
        
        const listColumns = getColumns();
        const defaultVisibleColumns = ['number','paymentGroup','addressLine1','bankAccount','status','id'];

        let actionButtons = [
            {
                text: 'Filter',
                className: 'custom-table-btn flex-none btn btn-secondary',
                id: "btn-toggle-filter",
                action: function (e, dt, node, config) { },
                attr: {
                    id: 'btn-toggle-filter'
                }
            }
        ];

        dataTable = initializeDataTable({
            dt,
            defaultVisibleColumns,
            listColumns,
            maxRowsPerPage: 10,
            defaultSortColumn: 0,
            dataEndpoint: unity.grantManager.applicants.applicantSupplier.getSitesBySupplierId,
            data: inputAction,
            responseCallback,
            actionButtons,
            pagingEnabled: false,
            dataTableName: 'SiteInfoTable',
            dynamicButtonContainerId: 'siteDynamicButtonContainerId'});
    
        dataTable.on('search.dt', () => handleSearch());
        
        $('#search').on('input', function () {
            let table = $('#SiteInfoTable').DataTable();
            table.search($(this).val()).draw();
        });
        
        function handleSearch() {
            let filter = $('.dataTables_filter input').val();
            console.info(filter);
        }
    
        function getColumns() {
            let columnIndex = 0;

            return [
                getSiteNumber(columnIndex++),
                getPayGroup(columnIndex++),
                getMailingAddress(columnIndex++),
                getBankAccount(columnIndex++),
                getStatus(columnIndex++),
                getSiteDefaultRadio(columnIndex++)
            ].map((column) => ({ ...column, targets: [column.index], orderData: [column.index, 0] }));
        }    
    }

    function getSiteNumber(columnIndex) {
        return {
            title: l('ApplicantInfoView:ApplicantInfo.SiteInfo:SiteNumber'),
            data: 'number',
            className: 'data-table-header',
            index: columnIndex
        }
    }

    function getPayGroup(columnIndex) {
        return {
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
            index: columnIndex
        }
    }

    function getMailingAddress(columnIndex) {
        return {
            title: l('ApplicantInfoView:ApplicantInfo.SiteInfo:MailingAddress'),
            data: 'addressLine1',
            className: 'data-table-header',
            render: function (data, type, full, meta) {
                return nullToEmpty(full.addressLine1) + ' ' + nullToEmpty(full.addressLine2) + " " + nullToEmpty(full.addressLine3) + " " + nullToEmpty(full.city) + " " + nullToEmpty(full.province) + " " + nullToEmpty(full.postalCode);
            },
            index: columnIndex
        }
    }

    function getBankAccount(columnIndex) {
        return {
            title: 'Bank Account',
            data: 'bankAccount',
            className: 'data-table-header',
            index: columnIndex
        }
    }

    function getStatus(columnIndex) {
        return {
            title: 'Status',
            data: 'status',
            className: 'data-table-header',
            index: columnIndex
        }
    }

    function getSiteDefaultRadio(columnIndex) {
        return {
            title: 'Default',
            data: 'id',
            className: 'data-table-header',
            sortable: false,
            render: function (data, type, full, meta) {
                let checked = UIElements.siteId.val() == data ? 'checked' : '';
                return `<input type="radio" class="site-radio" name="default-site" onclick="saveSiteDefault('${data}')" ${checked} />`;
            },
            index: columnIndex
        }
    }

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
            UIElements.siteId.val(data);
            loadSiteInfoTable();
        }
    );

    function nullToEmpty(value) {
        return value == null ? '' : value;
    }
});

function saveSiteDefault(siteId) {
    let applicantId = $("#ApplicantId").val();
    $.ajax({ url: `/api/app/applicant/${applicantId}/site/${siteId}`,
        type: "POST",
        data: JSON.stringify({ ApplicantId: applicantId, SiteId: siteId }),
    })
    .then(response => {
        abp.notify.success('Default site has been successfully saved.', 'Default Site Saved');
    })
    .catch(error => {
        console.error('There was a problem with the post operation:', error);
    });
}

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