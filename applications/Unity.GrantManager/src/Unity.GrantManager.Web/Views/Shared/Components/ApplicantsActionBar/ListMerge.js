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
        { label: 'Organization Size',                     key: 'organizationSize',            radioName: 'merge_OrganizationSize' },
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
                            <span style="overflow-wrap:break-word;word-break:break-word;">${aVal}</span>
                        </label>
                    </td>
                    <td>
                        <label class="d-flex align-items-center w-100 mb-0">
                            <input type="radio" name="${f.radioName}" value="b" class="me-2">
                            <span style="overflow-wrap:break-word;word-break:break-word;">${bVal}</span>
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

            // Convert indigenousOrgInd "Yes"/"No"/null → bool?/null for UpdateApplicantSummaryDto
            let indigenousOrgIndBool = null;
            if (merged['indigenousOrgInd'] === 'Yes') indigenousOrgIndBool = true;
            else if (merged['indigenousOrgInd'] === 'No') indigenousOrgIndBool = false;

            // Build payload matching UpdateApplicantSummaryDto property names (camelCase via ABP)
            const summaryData = {
                applicantName:               merged['applicantName'] ?? null,
                unityApplicantId:            merged['unityApplicantId'] ?? null,
                orgName:                     merged['orgName'] ?? null,
                orgNumber:                   merged['orgNumber'] ?? null,
                nonRegOrgName:               merged['nonRegOrgName'] ?? null,
                organizationType:            merged['organizationType'] ?? null,
                organizationSize:            merged['organizationSize'] ?? null,
                orgStatus:                   merged['orgStatus'] ?? null,
                indigenousOrgInd:            indigenousOrgIndBool,
                sector:                      merged['sector'] ?? null,
                subSector:                   merged['subSector'] ?? null,
                sectorSubSectorIndustryDesc: merged['sectorSubSectorIndustryDesc'] ?? null,
                fiscalDay:                   merged['fiscalDay'] === null ? null : String(merged['fiscalDay']),
                fiscalMonth:                 merged['fiscalMonth'] ?? null,
            };

            const modifiedFields = Object.keys(summaryData);

            $('#listMergeSpinner').removeClass('d-none');
            $('#listMergeMergeBtn').prop('disabled', true);

            // Step 1: mark non-principal as duplicated
            $.ajax({
                url: '/api/app/applicant/set-duplicated',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({
                    principalApplicantId: principal.id,
                    nonPrincipalApplicantId: nonPrincipal.id
                })
            }).then(() => {
                // Step 2: transfer all non-principal applications to principal
                return $.ajax({
                    url: '/api/app/applicant/transfer-applicant-applications',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({
                        principalApplicantId: principal.id,
                        nonPrincipalApplicantId: nonPrincipal.id
                    })
                });
            }).then(() => {
                // Step 3: update principal's summary fields
                return unity.grantManager.applicants.applicant
                    .partialUpdateApplicantSummary(principal.id, {
                        modifiedFields: modifiedFields,
                        data: summaryData
                    });
            }).then(() => {
                // Step 4: apply supplier selection and null all DefaultSiteId
                const supplierSide = $('input[name="merge_SupplierId"]:checked').val();
                const selectedSupplierId = supplierSide === 'a' ? (a.supplierId || null) : (b.supplierId || null);
                return $.ajax({
                    url: '/api/app/applicant-supplier/handle-supplier-after-merge',
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({
                        principalId: principal.id,
                        nonPrincipalId: nonPrincipal.id,
                        selectedSupplierId: selectedSupplierId
                    })
                });
            }).then(() => {
                $('#applicantListMergeModal').modal('hide');
                PubSub.publish('deselect_applicant', 'reset_data');
                $('#ApplicantsTable').DataTable().ajax.reload();
                abp.notify.success('Applicants merged successfully.');
            }).catch(err => {
                console.warn('Merge failed:', err);
                abp.notify.error('Merge failed. Please try again.');
            }).always(() => {
                $('#listMergeSpinner').addClass('d-none');
                $('#listMergeMergeBtn').prop('disabled', false);
            });
        });
    });
})();
