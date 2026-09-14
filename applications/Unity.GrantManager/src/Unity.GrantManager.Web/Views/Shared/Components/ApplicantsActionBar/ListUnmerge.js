(function () {
    const l = abp.localization.getResource('GrantManager');
    let selectedMergeId = null;
    let selectedPreview = null;

    function escapeHtml(value) {
        return $('<div>').text(value ?? '').html();
    }

    function showUnmergeModalAsync() {
        const $modal = $('#applicantUnmergeModal');
        if ($modal.hasClass('show')) {
            return Promise.resolve();
        }

        return new Promise(resolve => {
            $modal.one('shown.bs.modal', resolve);
            $modal.modal('show');
        });
    }

    function hideUnmergeModalAsync() {
        const $modal = $('#applicantUnmergeModal');
        if (!$modal.hasClass('show')) {
            return Promise.resolve();
        }

        return new Promise(resolve => {
            $modal.one('hidden.bs.modal', resolve);
            $modal.modal('hide');
        });
    }

    function resetModal() {
        selectedMergeId = null;
        selectedPreview = null;
        $('#applicantUnmergeStep1').removeClass('d-none');
        $('#applicantUnmergeStep2').addClass('d-none');
        $('#applicantUnmergeLoading').removeClass('d-none');
        $('#applicantUnmergeHistoryContainer').addClass('d-none');
        $('#applicantUnmergeHistoryBody').empty();
        $('#applicantUnmergeNextBtn').prop('disabled', true);
        $('#applicantUnmergeReason').val('');
        $('#applicantUnmergeBlocked').addClass('d-none').text('');
        $('#applicantUnmergeConfirmBtn').prop('disabled', true);
        $('#applicantUnmergeSpinner').addClass('d-none');
    }

    function renderHistory(items) {
        const $body = $('#applicantUnmergeHistoryBody').empty();
        items.forEach(item => {
            const names = `${item.principalApplicantName} / ${item.secondaryApplicantName}`;
            const blocked = item.canUnmerge ? '' : `<div class="text-danger small">${escapeHtml(item.blockReason)}</div>`;
            $body.append(`
                <tr>
                    <td><input type="radio" name="applicantUnmergeOperation" value="${item.id}" ${item.canUnmerge ? '' : 'disabled'}></td>
                    <td>${escapeHtml(names)}${blocked}</td>
                    <td>${escapeHtml(new Date(item.mergedAt).toLocaleString())}</td>
                    <td>${item.transferredApplicationCount}</td>
                </tr>`);
        });

        $('#applicantUnmergeLoading').addClass('d-none');
        $('#applicantUnmergeHistoryContainer').removeClass('d-none');
    }

    async function openModal(applicant) {
        resetModal();
        const modalShown = showUnmergeModalAsync();

        try {
            const result = await $.ajax({
                url: '/api/app/applicant-merge/reversible',
                method: 'GET',
                data: { applicantId: applicant.id }
            });
            await modalShown;

            const items = result.items || [];
            if (items.length === 0) {
                await hideUnmergeModalAsync();
                abp.message.info(l('ApplicantMerge:NoReversibleMerges'));
                return;
            }

            renderHistory(items);
        } catch (error) {
            const message = error?.responseJSON?.error?.message || l('ApplicantMerge:UnmergeFailed');
            await modalShown;
            await hideUnmergeModalAsync();
            abp.message.error(message);
        }
    }

    async function loadPreview() {
        if (!selectedMergeId) {
            return;
        }

        $('#applicantUnmergeNextBtn').prop('disabled', true);
        try {
            selectedPreview = await $.ajax({
                url: `/api/app/applicant-merge/${encodeURIComponent(selectedMergeId)}/preview`,
                method: 'GET'
            });
            $('#applicantUnmergeSummary').text(
                `${selectedPreview.principalApplicantName} / ${selectedPreview.secondaryApplicantName} — ${selectedPreview.transferredApplicationCount} ${l('ApplicantMerge:ApplicationsMoved').toLowerCase()}`
            );
            $('#applicantUnmergeBlocked')
                .toggleClass('d-none', selectedPreview.canUnmerge)
                .text(selectedPreview.blockReason || '');
            $('#applicantUnmergeStep1').addClass('d-none');
            $('#applicantUnmergeStep2').removeClass('d-none');
            updateConfirmState();
        } catch (error) {
            const message = error?.responseJSON?.error?.message || l('ApplicantMerge:UnmergeFailed');
            abp.message.error(message);
            $('#applicantUnmergeNextBtn').prop('disabled', false);
        }
    }

    function updateConfirmState() {
        const hasReason = $('#applicantUnmergeReason').val().trim().length >= 3;
        $('#applicantUnmergeConfirmBtn').prop('disabled', !selectedPreview?.canUnmerge || !hasReason);
    }

    async function executeUnmerge() {
        const reason = $('#applicantUnmergeReason').val().trim();
        if (!selectedMergeId || !selectedPreview?.canUnmerge || reason.length < 3) {
            return;
        }

        $('#applicantUnmergeConfirmBtn').prop('disabled', true);
        $('#applicantUnmergeSpinner').removeClass('d-none');
        try {
            await $.ajax({
                url: `/api/app/applicant-merge/${encodeURIComponent(selectedMergeId)}/unmerge`,
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ reason: reason })
            });
            $('#applicantUnmergeModal').modal('hide');
            PubSub.publish('deselect_applicant', 'reset_data');
            $('#ApplicantsTable').DataTable().ajax.reload();
            abp.notify.success(l('ApplicantMerge:UnmergeSuccess'));
        } catch (error) {
            const message = error?.responseJSON?.error?.message || l('ApplicantMerge:UnmergeFailed');
            abp.message.error(message, l('ApplicantMerge:UnmergeTitle'));
        } finally {
            $('#applicantUnmergeSpinner').addClass('d-none');
            updateConfirmState();
        }
    }

    $(function () {
        PubSub.subscribe('open_applicant_unmerge', (message, applicant) => {
            openModal(applicant);
        });

        $(document).on('change', 'input[name="applicantUnmergeOperation"]', function () {
            selectedMergeId = $(this).val();
            $('#applicantUnmergeNextBtn').prop('disabled', false);
        });
        $('#applicantUnmergeNextBtn').on('click', loadPreview);
        $('#applicantUnmergeBackBtn').on('click', () => {
            $('#applicantUnmergeStep2').addClass('d-none');
            $('#applicantUnmergeStep1').removeClass('d-none');
            $('#applicantUnmergeNextBtn').prop('disabled', !selectedMergeId);
        });
        $('#applicantUnmergeReason').on('input', updateConfirmState);
        $('#applicantUnmergeConfirmBtn').on('click', executeUnmerge);
        $('#applicantUnmergeModal').on('hidden.bs.modal', resetModal);
    });
})();
