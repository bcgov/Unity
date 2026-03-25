$(function () {
    let selectedApplicantIds = [];
    let selectedApplicants = [];

    // Subscribe to PubSub events for row selection
    PubSub.subscribe("select_applicant", (msg, data) => {
        if (!selectedApplicantIds.includes(data.id)) {
            selectedApplicantIds.push(data.id);
            selectedApplicants.push(data);
        }
        manageActionButtons();
    });

    PubSub.subscribe("deselect_applicant", (msg, data) => {
        if (data === "reset_data") {
            selectedApplicantIds = [];
            selectedApplicants = [];
        } else {
            selectedApplicantIds = selectedApplicantIds.filter(item => item !== data.id);
            selectedApplicants = selectedApplicants.filter(item => item.id !== data.id);
        }
        manageActionButtons();
    });

    PubSub.subscribe("clear_selected_applicant", (msg, data) => {
        selectedApplicantIds = [];
        selectedApplicants = [];
        manageActionButtons();
    });

    // Manage button states based on selection - matching GrantApplications pattern
    function manageActionButtons() {
        if (selectedApplicantIds.length === 0) {
            $('*[data-selector="applicants-table-actions"]').addClass('action-bar-btn-unavailable');
            $('.action-bar').removeClass('active');
        } else {
            $('*[data-selector="applicants-table-actions"]').removeClass('action-bar-btn-unavailable');
            $('.action-bar').addClass('active');

            // OPEN button is only visible when exactly 1 applicant is selected
            $('#openApplicant').addClass('action-bar-btn-unavailable');
            if (selectedApplicantIds.length === 1) {
                $('#openApplicant').removeClass('action-bar-btn-unavailable');
            }
        }

        // Show MERGE button only when exactly 2 applicants are selected
        if (selectedApplicantIds.length === 2) {
            $('#mergeApplicants').removeClass('d-none');
        } else {
            $('#mergeApplicants').addClass('d-none');
        }
    }

    // Handle OPEN button click
    $('#openApplicant').click(function () {
        if (selectedApplicantIds.length === 1) {
            window.location.href = `/GrantApplicants/Details?ApplicantId=${selectedApplicantIds[0]}`;
        }
    });

    // MERGE button click — open modal with the 2 selected applicants
    $('#mergeApplicants').on('click', () => {
        if (selectedApplicants.length === 2) {
            PubSub.publish('open_applicant_list_merge', {
                a: selectedApplicants[0],
                b: selectedApplicants[1]
            });
        }
    });

    // Handle search input
    $('#search').on('input', function () {
        let table = $('#ApplicantsTable').DataTable();
        table.search($(this).val()).draw();
    });

    // Initialize button states
    manageActionButtons();
});