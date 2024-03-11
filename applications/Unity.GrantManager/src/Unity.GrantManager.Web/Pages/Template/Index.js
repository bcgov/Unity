$(document).ready(function () {
    var data = [
        { category: 'Applications', templateName: 'Low Value Application', description: 'Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application' },
        { category: 'MJF', templateName: 'Template - Application 2', description: 'Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application' },
        { category: 'Applications', templateName: 'Template - Application 3', description: 'Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application' },
        { category: 'Report', templateName: 'Template - Application 4', description: 'Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application' },
        { category: 'Report', templateName: 'Template - Application 5', description: 'Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application' },
        { category: 'Report', templateName: 'Template - Application 6', description: 'Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application' },
        { category: 'Report', templateName: 'Template - Application 7', description: 'Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application' },
        { category: 'Report', templateName: 'Template - Application 8', description: 'Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application' },
        { category: 'Report', templateName: 'Template - Application 9', description: 'Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application Ministry of Teleportation - Retro-causality Grant Application' },


    ];

    var columns = [
        { title: 'Category', data: 'category', width: '20%' },
        { title: 'Template Name', data: 'templateName', width: '30%' },
        { title: 'Description', data: 'description', orderable: false, width: '40%' },
        {
            title: '',
            data: null,
            render: function (data, type, row) {
                return '<img class="table-action-icons" src="/images/icons/download.svg" alt="Download">' +
                        '<img class="table-action-icons" src="/images/icons/view.svg" alt="View">'+
                        '<img class="table-action-icons" id="editTemplate" src="/images/icons/edit.svg" alt="Edit">'                    ;
            },
            orderable: false, // Disable sorting for Actions column
            width: '10%' 
        }
    ];

    var table = $('#templateList').DataTable({
        data: data,//static data
        columns: columns,
        searching: true, // Enable searching
        lengthChange: false, // Disable length change
        info: false, // Disable information display
        paging: false, // Hide pagination controls
        autoWidth: false,
    });
    // Hide main search input
    $('.dataTables_filter').hide();

    // Add filter inputs to each column header
    $('#templateList thead tr').clone(false).addClass('filters').appendTo('#templateList thead');
    $('#templateList thead tr:eq(1) th:not(:last-child)').each(function (i) {
       
        $(this).html('<input type="text" class="form-control form-control-sm"/>');
        $('#templateList thead tr:eq(1) th').hide();

        // Disable sorting for filter columns
        //if (i < columns.length - 1) {
            $(this).removeClass('sorting_asc sorting_desc sorting').addClass('sorting_disabled');
        //}
        // Filter event listeners
        $('input', this).on('keyup change', function () {
            if (table.column(i).search() !== this.value) {
                table
                    .column(i)
                    .search(this.value)
                    .draw();
            }
        });
    });
    // Toggle search input visibility on filter button click
    $('#filter-btn').click(function () {
        $('#templateList thead tr:eq(1) th').toggle();
    });
});
    $(function () {
        let editTemplateModal = new abp.ModalManager({
        viewUrl: 'Template/TemplateEditModal'
    });
        $('#editTemplate').click(function () {
            editTemplateModal.open({
            });
        })
    });