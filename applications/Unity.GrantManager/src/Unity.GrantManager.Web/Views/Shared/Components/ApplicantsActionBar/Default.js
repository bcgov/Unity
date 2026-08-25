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

            // OPEN and DELETE buttons are only visible when exactly 1 applicant is selected
            $('#openApplicant').addClass('action-bar-btn-unavailable');
            $('#deleteApplicant').addClass('action-bar-btn-unavailable');
            if (selectedApplicantIds.length === 1) {
                $('#openApplicant').removeClass('action-bar-btn-unavailable');
                $('#deleteApplicant').removeClass('action-bar-btn-unavailable');
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

    function executeDelete(applicantId, applicantName) {
        unity.grantManager.applicants.applicant
            .deleteApplicant(applicantId)
            .then(function () {
                PubSub.publish('deselect_applicant', 'reset_data');
                $('#ApplicantsTable').DataTable().ajax.reload();
                abp.notify.success('Applicant "' + applicantName + '" has been deleted.');
            })
            .catch(function (e) {
                console.warn('deleteApplicant error:', e);
                const msg = e?.responseJSON?.error?.message || 'An error occurred while deleting the applicant. Please try again.';
                abp.message.error(msg, 'Delete Failed');
            });
    }

    // DELETE button click — pre-check submissions, then show the correct modal
    $('#deleteApplicant').on('click', function () {
        if (selectedApplicantIds.length !== 1) return;
        const applicantId = selectedApplicantIds[0];
        const applicantName = (selectedApplicants[0].applicantName || 'Applicant Name');

        unity.grantManager.applicants.applicant
            .hasSubmissions(applicantId)
            .then(function (hasSubmissions) {
                if (hasSubmissions) {
                    abp.message.warn(
                        'This applicant cannot be deleted because it is associated with one or more submissions.',
                        'Cannot Delete Applicant'
                    );
                } else {
                    abp.message.confirm(
                        'Are you sure you want to delete the applicant "' + applicantName + '"?',
                        'Delete Applicant',
                        function (confirmed) {
                            if (confirmed) executeDelete(applicantId, applicantName);
                        }
                    );
                }
            })
            .catch(function (e) {
                console.warn('hasSubmissions error:', e);
            });
    });

    // Handle search input
    $('#search').on('input', function () {
        let table = $('#ApplicantsTable').DataTable();
        table.search($(this).val()).draw();
    });

    // Initialize button states
    manageActionButtons();
});
