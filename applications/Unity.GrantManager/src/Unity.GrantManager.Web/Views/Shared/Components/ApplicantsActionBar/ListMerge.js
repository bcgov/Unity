(function () {
    // Module-level state: the two applicants received when the modal is opened
    let _applicantA = null;
    let _applicantB = null;

    // Field definitions — label matches ApplicantInfo localization, key is ApplicantListDto camelCase.
    // key: null means the field is handled separately (not part of UpdateApplicantSummaryDto).
    // format: fn(applicant) overrides the plain key lookup for display.
    // The Principal Record row (merge_ApplicantId) is static HTML; these are the dynamic field rows.
    const MERGE_FIELDS = [
        { label: 'Applicant Id',                          key: 'unityApplicantId',            radioName: 'merge_UnityApplicantId' },
        { label: 'Applicant Name',                        key: 'applicantName',               radioName: 'merge_ApplicantName' },
        { label: 'Registered Organization Name',          key: 'orgName',                     radioName: 'merge_OrgName' },
        { label: 'Registered Organization Number',        key: 'orgNumber',                   radioName: 'merge_OrgNumber' },
        { label: 'Non-Registered Organization Name',      key: 'nonRegOrgName',               radioName: 'merge_NonRegOrgName' },
        { label: 'Organization Type',                     key: 'organizationType',            radioName: 'merge_OrganizationType' },
        { label: 'Organization Size (Approximate Number of Employees)', key: 'approxNumberOfEmployees', radioName: 'merge_ApproxNumberOfEmployees' },
        { label: 'Org book status',                       key: 'orgStatus',                   radioName: 'merge_OrgStatus' },
        { label: 'Indigenous',                            key: 'indigenousOrgInd',            radioName: 'merge_IndigenousOrgInd' },
        { label: 'Sector',                                key: 'sector',                      radioName: 'merge_Sector' },
        { label: 'Sub-sector',                            key: 'subSector',                   radioName: 'merge_SubSector' },
        { label: 'Other Sector/Sub/Industry Description', key: 'sectorSubSectorIndustryDesc', radioName: 'merge_SectorSubSectorIndustryDesc' },
        { label: 'Fiscal Year End Day',                   key: 'fiscalDay',                   radioName: 'merge_FiscalDay' },
        { label: 'Fiscal Year End Month',                 key: 'fiscalMonth',                 radioName: 'merge_FiscalMonth' },
        { label: 'Supplier',                              key: null,                          radioName: 'merge_SupplierId',
          format: a => [a.supplierNumber, a.supplierName, a.supplierStatus].filter(Boolean).join(' – ') || '(None)' },
    ];

    function openListMergeModal(a, b) {
        _applicantA = a;
        _applicantB = b;

        // Column headers show applicant names
        $('#listMergeColA').text(a.applicantName ?? a.id);
        $('#listMergeColB').text(b.applicantName ?? b.id);

        // Show "Flagged as Duplicated" badge if the applicant has IsDuplicated=true
        $('#listMergeDuplicateFlagA').toggleClass('d-none', !a.isDuplicated);
        $('#listMergeDuplicateFlagB').toggleClass('d-none', !b.isDuplicated);

        // Name match summary badge
        let score = compareStrings(a.applicantName || '', b.applicantName || '');
        let $badge = $('#listMergeNameMatchBadgeText');
        $badge.removeClass('unity-badge-warning');
        if (score >= 100) {
            $badge.text('100% Matched - Possible Duplicate');
        } else if (score >= 50) {
            $badge.text('Partially Matched');
        } else {
            $badge.text('Not Matched').addClass('unity-badge-warning');
        }

        // Build dynamic field rows
        const $tbody = $('#listMergeTableBody').empty();
        MERGE_FIELDS.forEach(f => {
            const aVal = f.format ? f.format(a) : (a[f.key] ?? '');
            const bVal = f.format ? f.format(b) : (b[f.key] ?? '');
            $tbody.append(`
                <tr>
                    <td>${f.label}</td>
                    <td>
                        <label class="d-flex align-items-center w-100 mb-0">
                            <input type="radio" name="${f.radioName}" value="a" checked class="me-2">
                            <span style="overflow-wrap:anywhere;word-break:normal;">${aVal}</span>
                        </label>
                    </td>
                    <td>
                        <label class="d-flex align-items-center w-100 mb-0">
                            <input type="radio" name="${f.radioName}" value="b" class="me-2">
                            <span style="overflow-wrap:anywhere;word-break:normal;">${bVal}</span>
                        </label>
                    </td>
                </tr>`);
        });

        // Reset to step 1
        $('#listMergeStep1').removeClass('d-none');
        $('#listMergeStep2').addClass('d-none');

        $('#applicantListMergeModal').modal('show');
    }

    $(function () {
        PubSub.subscribe('open_applicant_list_merge', (msg, data) => {
            openListMergeModal(data.a, data.b);
        });

        // Select All — covers both the static Principal Record row and all dynamic rows
        $('#listMergeSelectAllExisting').on('click', () => {
            $('#applicantListMergeModal input[type="radio"][value="a"]').prop('checked', true);
        });
        $('#listMergeSelectAllNew').on('click', () => {
            $('#applicantListMergeModal input[type="radio"][value="b"]').prop('checked', true);
        });

        // Step navigation
        $('#listMergeNextBtn').on('click', async () => {
            $('#listMergeStep1').addClass('d-none');
            $('#listMergeStep2').removeClass('d-none');
            $('#listMergeMergeBtn').prop('disabled', true);

            const principalChoice = $('input[name="merge_ApplicantId"]:checked').val();
            const principal = principalChoice === 'a' ? _applicantA : _applicantB;
            const nonPrincipal = principalChoice === 'a' ? _applicantB : _applicantA;

            try {
                const hasPending = await $.ajax({
                    url: '/api/app/applicant-supplier/has-pending-payments-for-merge',
                    method: 'GET',
                    data: { principalId: principal.id, nonPrincipalId: nonPrincipal.id }
                });
                $('#listMergePendingWarning').toggleClass('d-none', !hasPending);
                $('#listMergeMergeBtn').prop('disabled', !!hasPending);
            } catch (err) {
                console.warn('Pending payment check failed:', err);
                abp.notify.error('Unable to verify payment status. Please go back and try again.');
            }
        });
        $('#listMergeBackBtn').on('click', () => {
            $('#listMergeStep2').addClass('d-none');
            $('#listMergeStep1').removeClass('d-none');
        });

        // Execute merge
        $('#listMergeMergeBtn').on('click', () => {
            const a = _applicantA;
            const b = _applicantB;

            // Determine principal from the static merge_ApplicantId radio
            const principalChoice = $('input[name="merge_ApplicantId"]:checked').val();
            const principal = principalChoice === 'a' ? a : b;
            const nonPrincipal = principalChoice === 'a' ? b : a;

            // Build merged field values from dynamic radio selections.
            // Skip fields with key: null (handled separately, e.g. Supplier).
            const merged = {};
            MERGE_FIELDS.forEach(f => {
                if (f.key === null) return;
                const choice = $(`input[name="${f.radioName}"]:checked`).val();
                merged[f.key] = choice === 'a' ? a[f.key] : b[f.key];
            });

            const fiscalDay = merged['fiscalDay'];
            const summaryData = {
                applicantName:               merged['applicantName'] ?? null,
                unityApplicantId:            merged['unityApplicantId'] ?? null,
                orgName:                     merged['orgName'] ?? null,
                orgNumber:                   merged['orgNumber'] ?? null,
                nonRegOrgName:               merged['nonRegOrgName'] ?? null,
                organizationType:            merged['organizationType'] ?? null,
                approxNumberOfEmployees:     merged['approxNumberOfEmployees'] ?? null,
                orgStatus:                   merged['orgStatus'] ?? null,
                indigenousOrgInd:            merged['indigenousOrgInd'] ?? null,
                sector:                      merged['sector'] ?? null,
                subSector:                   merged['subSector'] ?? null,
                sectorSubSectorIndustryDesc: merged['sectorSubSectorIndustryDesc'] ?? null,
                fiscalDay:                   fiscalDay === null || fiscalDay === undefined || fiscalDay === ''
                    ? null
                    : Number(fiscalDay),
                fiscalMonth:                 merged['fiscalMonth'] ?? null,
            };

            $('#listMergeSpinner').removeClass('d-none');
            $('#listMergeMergeBtn').prop('disabled', true);

            const supplierSide = $('input[name="merge_SupplierId"]:checked').val();
            const selectedSupplierId = supplierSide === 'a' ? (a.supplierId || null) : (b.supplierId || null);

            $.ajax({
                url: '/api/app/applicant-merge',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    principalApplicantId: principal.id,
                    secondaryApplicantId: nonPrincipal.id,
                    summary: summaryData,
                    selectedSupplierId: selectedSupplierId,
                    source: 0
                })
            }).then(() => {
                $('#applicantListMergeModal').modal('hide');
                PubSub.publish('deselect_applicant', 'reset_data');
                $('#ApplicantsTable').DataTable().ajax.reload();
                abp.notify.success('Applicants merged successfully.');
            }).catch(err => {
                console.warn('Merge failed:', err);
                const msg = err?.responseJSON?.error?.message || 'Merge failed. Please try again.';
                abp.message.error(msg, 'Merge Failed');
            }).always(() => {
                $('#listMergeSpinner').addClass('d-none');
                $('#listMergeMergeBtn').prop('disabled', false);
            });
        });
    });
})();
