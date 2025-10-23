$(function () {
    const addressesData = JSON.parse($('#ApplicantAddresses_Data').val() || '[]');
    const nullPlaceholder = '—';

    const dataTable = $('#ApplicantAddressesTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            data: addressesData,
            serverSide: false,
            order: [[1, 'asc']],
            searching: true,
            paging: true,
            pageLength: 10,
            select: false,
            info: true,
            scrollX: true,
            drawCallback: function() {
                this.api().columns.adjust();
            },
            columnDefs: [
                {
                    title: 'Address Type',
                    data: 'addressType',
                    width: '15%',
                    render: function (data, type, full) {
                        if (type === 'display') {
                            const badgeClass = data === 'Mailing' ? 'address-type-badge mailing' : 'address-type-badge';
                            return `<span class="${badgeClass}">${data}</span>`;
                        }
                        return data;
                    }
                },
                {
                    title: 'Submission #',
                    data: 'referenceNo',
                    width: '12%',
                    render: function (data) {
                        return data || nullPlaceholder;
                    }
                },
                {
                    title: 'Street Address',
                    data: 'street',
                    width: '20%',
                    render: function (data) {
                        return data || nullPlaceholder;
                    }
                },
                {
                    title: 'Unit',
                    data: 'unit',
                    width: '8%',
                    render: function (data) {
                        return data || nullPlaceholder;
                    }
                },
                {
                    title: 'City',
                    data: 'city',
                    width: '12%',
                    render: function (data) {
                        return data || nullPlaceholder;
                    }
                },
                {
                    title: 'Province',
                    data: 'province',
                    width: '10%',
                    render: function (data) {
                        return data || nullPlaceholder;
                    }
                },
                {
                    title: 'Postal Code',
                    data: 'postal',
                    width: '10%',
                    render: function (data) {
                        return data || nullPlaceholder;
                    }
                },
                {
                    title: 'Country',
                    data: 'country',
                    width: '10%',
                    render: function (data) {
                        return data || nullPlaceholder;
                    }
                }
            ],
        })
    );
});
