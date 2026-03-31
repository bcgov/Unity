const LAYOUT_NOTIFICATION_DELAYS = [0, 120, 280, 600];

// Helper functions
function titleCase(str) {
    if (!str) return '';
    str = str.toLowerCase().split(' ');
    for (let i = 0; i < str.length; i++) {
        str[i] = str[i].charAt(0).toUpperCase() + str[i].slice(1);
    }
    return str.join(' ');
}

function convertToYesNo(str) {
    switch (str) {
        case true:
            return "Yes";
        case false:
            return "No";
        default:
            return '';
    }
}

function getFullType(code) {
    const companyTypes = [
        { code: "BC", name: "BC Company" },
        { code: "CP", name: "Cooperative" },
        { code: "GP", name: "General Partnership" },
        { code: "S", name: "Society" },
        { code: "SP", name: "Sole Proprietorship" },
        { code: "A", name: "Extraprovincial Company" },
        { code: "B", name: "Extraprovincial" },
        { code: "BEN", name: "Benefit Company" },
        { code: "C", name: "Continuation In" },
        { code: "CC", name: "BC Community Contribution Company" },
        { code: "CS", name: "Continued In Society" },
        { code: "CUL", name: "Continuation In as a BC ULC" },
        { code: "EPR", name: "Extraprovincial Registration" },
        { code: "FI", name: "Financial Institution" },
        { code: "FOR", name: "Foreign Registration" },
        { code: "LIB", name: "Public Library Association" },
        { code: "LIC", name: "Licensed (Extra-Pro)" },
        { code: "LL", name: "Limited Liability Partnership" },
        { code: "LLC", name: "Limited Liability Company" },
        { code: "LP", name: "Limited Partnership" },
        { code: "MF", name: "Miscellaneous Firm" },
        { code: "PA", name: "Private Act" },
        { code: "PAR", name: "Parish" },
        { code: "QA", name: "CO 1860" },
        { code: "QB", name: "CO 1862" },
        { code: "QC", name: "CO 1878" },
        { code: "QD", name: "CO 1890" },
        { code: "QE", name: "CO 1897" },
        { code: "REG", name: "Registraton (Extra-pro)" },
        { code: "ULC", name: "BC Unlimited Liability Company" },
        { code: "XCP", name: "Extraprovincial Cooperative" },
        { code: "XL", name: "Extrapro Limited Liability Partnership" },
        { code: "XP", name: "Extraprovincial Limited Partnership" },
        { code: "XS", name: "Extraprovincial Society" }
    ];
    const match = companyTypes.find(entry => entry.code === code);
    return match ? match.name : "Unknown";
}

function payoutDefinition(approvedAmount, totalPaid) {
    if ((approvedAmount > 0 && totalPaid > 0) && (approvedAmount === totalPaid)) {
        return 'Fully Paid';
    } else if (totalPaid === 0) {
        return '';
    } else {
        return 'Partially Paid';
    }
}

function getNames(data) {
    let name = '';
    data.forEach((d, index) => {
        name = name + (' ' + d.fullName + getDutyText(d));
        if (index != (data.length - 1)) {
            name = name + ',';
        }
    });

    return name;
}

function getDutyText(data) {
    return data.duty ? (" [" + data.duty + "]") : '';
}

function formatItems(items) {
    const newData = items.map((item, index) => {
        return {
            ...item,
            rowCount: index
        };
    });
    return newData;
}

function notifySubmissionsLayoutChange() {
    globalThis.dispatchEvent(new CustomEvent('applicant-submissions-layout-changed'));
}

function scheduleLayoutNotifications() {
    LAYOUT_NOTIFICATION_DELAYS.forEach((delay) => {
        setTimeout(notifySubmissionsLayoutChange, delay);
    });
}

function bindLayoutNotificationEvents(dataTable) {
    dataTable.on('draw', notifySubmissionsLayoutChange);
}

function updateOpenButtonState(dataTable) {
    const selectedRows = dataTable.rows({ selected: true }).data();
    const $openBtn = $('#openSubmissionBtn');

    if (selectedRows.length === 1) {
        $openBtn.prop('disabled', false).show();
    } else {
        $openBtn.prop('disabled', true).hide();
    }
}

// Column getter functions
function getSelectColumn(columnIndex) {
    return {
        title: '',
        data: 'rowCount',
        name: 'select',
        orderable: false,
        className: 'notexport dt-checkboxes-cell',
        checkboxes: {
            selectRow: true,
            selectAllRender: '<input type="checkbox" class="form-check-input checkbox-select chkbox">',
        },
        render: function (data, type, row) {
            return '<input type="checkbox" class="form-check-input checkbox-select chkbox row-checkbox" id="row-' + data + '">';
        },
        index: columnIndex
    };
}

function getReferenceNoColumn(columnIndex) {
    return {
        title: 'Submission #',
        data: 'referenceNo',
        name: 'referenceNo',
        className: 'data-table-header text-nowrap',
        render: function (data, type, row) {
            return `<a href="/GrantApplications/Details?ApplicationId=${row.id}">${data || ''}</a>`;
        },
        index: columnIndex
    };
}

function getApplicantNameColumn(columnIndex) {
    return {
        title: 'Applicant Name',
        data: 'applicant.applicantName',
        name: 'applicantName',
        className: 'data-table-header',
        index: columnIndex
    };
}

function getCategoryColumn(columnIndex, l) {
    return {
        title: 'Category',
        data: 'category',
        name: 'category',
        className: 'data-table-header',
        index: columnIndex
    };
}

function getSubmissionDateColumn(columnIndex, l) {
    return {
        title: l('SubmissionDate'),
        data: 'submissionDate',
        name: 'submissionDate',
        className: 'data-table-header',
        index: columnIndex,
        render: function (data, type) {
            return DateUtils.formatUtcDateToLocal(data, type);
        }
    };
}

function getProjectNameColumn(columnIndex) {
    return {
        title: 'Project Name',
        data: 'projectName',
        name: 'projectName',
        className: 'data-table-header',
        index: columnIndex
    };
}

function getSectorColumn(columnIndex) {
    return {
        title: 'Sector',
        name: 'sector',
        data: 'applicant.sector',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getSubSectorColumn(columnIndex) {
    return {
        title: 'SubSector',
        name: 'subsector',
        data: 'applicant.subSector',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getTotalProjectBudgetColumn(columnIndex, formatter) {
    return {
        title: 'Total Project Budget',
        name: 'totalProjectBudget',
        data: 'totalProjectBudget',
        className: 'data-table-header currency-display',
        render: function (data) {
            return formatter.format(data);
        },
        index: columnIndex
    };
}

function getAssigneesColumn(columnIndex, l) {
    return {
        title: l('Assignee'),
        data: 'assignees',
        name: 'assignees',
        className: 'dt-editable',
        render: function (data, type, row) {
            let displayText = ' ';

            if (data?.length == 1) {
                displayText = type === 'fullName' ? getNames(data) : (data[0].fullName + getDutyText(data[0]));
            } else if (data.length > 1) {
                displayText = getNames(data);
            }

            return `<span class="d-flex align-items-center dt-select-assignees">

                            <span class="ps-2 flex-fill" data-toggle="tooltip" title="`
                + getNames(data) + '">' + displayText + '</span>' +
                `</span>`;
        },
        index: columnIndex
    };
}

function getStatusColumn(columnIndex, l) {
    return {
        title: l('GrantApplicationStatus'),
        data: 'status',
        name: 'status',
        className: 'data-table-header',
        index: columnIndex
    };
}

function getRequestedAmountColumn(columnIndex, l, formatter) {
    return {
        title: l('RequestedAmount'),
        data: 'requestedAmount',
        name: 'requestedAmount',
        className: 'data-table-header currency-display',
        render: function (data) {
            return formatter.format(data);
        },
        index: columnIndex
    };
}

function getApprovedAmountColumn(columnIndex, formatter) {
    return {
        title: 'Approved Amount',
        name: 'approvedAmount',
        data: 'approvedAmount',
        className: 'data-table-header currency-display',
        render: function (data) {
            return formatter.format(data);
        },
        index: columnIndex
    };
}

function getEconomicRegionColumn(columnIndex) {
    return {
        title: 'Economic Region',
        name: 'economicRegion',
        data: 'economicRegion',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getRegionalDistrictColumn(columnIndex) {
    return {
        title: 'Regional District',
        name: 'regionalDistrict',
        data: 'regionalDistrict',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getCommunityColumn(columnIndex) {
    return {
        title: 'Community',
        name: 'community',
        data: 'community',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getOrganizationNumberColumn(columnIndex, l) {
    return {
        title: l('ApplicantInfoView:ApplicantInfo.OrgNumber'),
        name: 'orgNumber',
        data: 'applicant.orgNumber',
        className: 'data-table-header',
        visible: false,
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getOrgBookStatusColumn(columnIndex) {
    return {
        title: 'Org Book Status',
        name: 'orgBookStatus',
        data: 'applicant.orgStatus',
        className: 'data-table-header',
        render: function (data) {
            if (data === 'ACTIVE') {
                return 'Active';
            } else if (data === 'HISTORICAL') {
                return 'Historical';
            } else {
                return data ?? '';
            }
        },
        index: columnIndex
    };
}

function getProjectStartDateColumn(columnIndex) {
    return {
        title: 'Project Start Date',
        name: 'projectStartDate',
        data: 'projectStartDate',
        className: 'data-table-header',
        render: function (data) {
            return data ? luxon.DateTime.fromISO(data, {
                locale: abp.localization.currentCulture.name,
            }).toUTC().toLocaleString() : '';
        },
        index: columnIndex
    };
}

function getProjectEndDateColumn(columnIndex) {
    return {
        title: 'Project End Date',
        name: 'projectEndDate',
        data: 'projectEndDate',
        className: 'data-table-header',
        render: function (data) {
            return data ? luxon.DateTime.fromISO(data, {
                locale: abp.localization.currentCulture.name,
            }).toUTC().toLocaleString() : '';
        },
        index: columnIndex
    };
}

function getProjectedFundingTotalColumn(columnIndex, formatter) {
    return {
        title: 'Projected Funding Total',
        name: 'projectFundingTotal',
        data: 'projectFundingTotal',
        className: 'data-table-header currency-display',
        render: function (data) {
            return formatter.format(data) ?? '';
        },
        index: columnIndex
    };
}

function getTotalProjectBudgetPercentageColumn(columnIndex) {
    return {
        title: '% of Total Project Budget',
        name: 'percentageTotalProjectBudget',
        data: 'percentageTotalProjectBudget',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getTotalPaidAmountColumn(columnIndex, formatter) {
    return {
        title: 'Total Paid Amount $',
        name: 'totalPaidAmount',
        data: 'paymentInfo',
        className: 'data-table-header currency-display',
        render: function (data) {
            let totalPaid = data?.totalPaid ?? '';
            return formatter.format(totalPaid);
        },
        index: columnIndex
    };
}

function getElectoralDistrictColumn(columnIndex) {
    return {
        title: 'Project Electoral District',
        name: 'electoralDistrict',
        data: 'electoralDistrict',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getApplicantElectoralDistrictColumn(columnIndex) {
    return {
        title: 'Applicant Electoral District',
        name: 'applicantElectoralDistrict',
        data: 'applicantElectoralDistrict',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getForestryOrNonForestryColumn(columnIndex) {
    return {
        title: 'Forestry or Non-Forestry',
        name: 'forestryOrNonForestry',
        data: 'forestry',
        className: 'data-table-header',
        render: function (data) {
            if (data)
                return data == 'FORESTRY' ? 'Forestry' : 'Non Forestry';
            else
                return '';
        },
        index: columnIndex
    };
}

function getForestryFocusColumn(columnIndex) {
    return {
        title: 'Forestry Focus',
        name: 'forestryFocus',
        data: 'forestryFocus',
        className: 'data-table-header',
        render: function (data) {
            if (!data) {
                return '';
            }
            if (data == 'PRIMARY') {
                return 'Primary processing';
            } else if (data == 'SECONDARY') {
                return 'Secondary/Value-Added/Not Mass Timber';
            } else if (data == 'MASS_TIMBER') {
                return 'Mass Timber';
            } else {
                return data;
            }
        },
        index: columnIndex
    };
}

function getAcquisitionColumn(columnIndex) {
    return {
        title: 'Acquisition',
        name: 'acquisition',
        data: 'acquisition',
        className: 'data-table-header',
        render: function (data) {
            if (data) {
                return titleCase(data);
            }
            else {
                return '';
            }
        },
        index: columnIndex
    };
}

function getCityColumn(columnIndex) {
    return {
        title: 'City',
        name: 'city',
        data: 'city',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getCommunityPopulationColumn(columnIndex) {
    return {
        title: 'Community Population',
        name: 'communityPopulation',
        data: 'communityPopulation',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getLikelihoodOfFundingColumn(columnIndex) {
    return {
        title: 'Likelihood of Funding',
        name: 'likelihoodOfFunding',
        data: 'likelihoodOfFunding',
        className: 'data-table-header',
        render: function (data) {
            if (data) {
                return titleCase(data);
            }
            else {
                return '';
            }
        },
        index: columnIndex
    };
}

function getSubStatusColumn(columnIndex) {
    return {
        title: 'Sub-Status',
        name: 'subStatusDisplayValue',
        data: 'subStatusDisplayValue',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getTagsColumn(columnIndex) {
    return {
        title: 'Tags',
        name: 'applicationTag',
        data: 'applicationTag',
        className: '',
        render: function (data) {
            if (data && Array.isArray(data)) {
                let tagNames = data
                    .filter(x => x?.tag?.name)
                    .map(x => x.tag.name);
                return tagNames.join(', ') ?? '';
            }
            return '';
        },
        index: columnIndex
    };
}

function getTotalScoreColumn(columnIndex) {
    return {
        title: 'Total Score',
        name: 'totalScore',
        data: 'totalScore',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getAssessmentResultColumn(columnIndex) {
    return {
        title: 'Assessment Result',
        name: 'assessmentResult',
        data: 'assessmentResultStatus',
        className: 'data-table-header',
        render: function (data) {
            if (data) {
                return titleCase(data);
            }
            else {
                return '';
            }
        },
        index: columnIndex
    };
}

function getRecommendedAmountColumn(columnIndex, formatter) {
    return {
        title: 'Recommended Amount',
        name: 'recommendedAmount',
        data: 'recommendedAmount',
        className: 'data-table-header currency-display',
        render: function (data) {
            return formatter.format(data) ?? '';
        },
        index: columnIndex
    };
}

function getDueDateColumn(columnIndex) {
    return {
        title: 'Due Date',
        name: 'dueDate',
        data: 'dueDate',
        className: 'data-table-header',
        render: function (data) {
            return data ? luxon.DateTime.fromISO(data, {
                locale: abp.localization.currentCulture.name,
            }).toUTC().toLocaleString() : '';
        },
        index: columnIndex
    };
}

function getOwnerColumn(columnIndex) {
    return {
        title: 'Owner',
        name: 'Owner',
        data: 'owner',
        className: 'data-table-header',
        render: function (data) {
            return data ? data.fullName : '';
        },
        index: columnIndex
    };
}

function getDecisionDateColumn(columnIndex) {
    return {
        title: 'Decision Date',
        name: 'finalDecisionDate',
        data: 'finalDecisionDate',
        className: 'data-table-header',
        render: function (data) {
            return data ? luxon.DateTime.fromISO(data, {
                locale: abp.localization.currentCulture.name,
            }).toUTC().toLocaleString() : '';
        },
        index: columnIndex
    };
}

function getProjectSummaryColumn(columnIndex) {
    return {
        title: 'Project Summary',
        name: 'projectSummary',
        data: 'projectSummary',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getOrganizationTypeColumn(columnIndex) {
    return {
        title: 'Organization Type',
        name: 'organizationType',
        data: 'organizationType',
        className: 'data-table-header',
        render: function (data) {
            return getFullType(data) ?? '';
        },
        index: columnIndex
    };
}

function getOrganizationNameColumn(columnIndex, l) {
    return {
        title: l('Summary:Application.OrganizationName'),
        name: 'organizationName',
        data: 'organizationName',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getBusinessNumberColumn(columnIndex, l) {
    return {
        title: l('Summary:Application.BusinessNumber'),
        name: 'businessNumber',
        data: 'applicant.businessNumber',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getNonRegisteredOrganizationNameColumn(columnIndex, l) {
    return {
        title: l('Summary:Application.NonRegOrgName'),
        name: 'nonRegOrgName',
        data: 'nonRegOrgName',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getUnityApplicationIdColumn(columnIndex) {
    return {
        title: 'Unity Application ID',
        name: 'unityApplicationId',
        data: 'unityApplicationId',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getDueDiligenceStatusColumn(columnIndex) {
    return {
        title: 'Due Diligence Status',
        name: 'dueDiligenceStatus',
        data: 'dueDiligenceStatus',
        className: 'data-table-header',
        render: function (data) {
            return titleCase(data ?? '') ?? '';
        },
        index: columnIndex
    };
}

function getDeclineRationaleColumn(columnIndex) {
    return {
        title: 'Decline Rationale',
        name: 'declineRationale',
        data: 'declineRational',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getContactFullNameColumn(columnIndex) {
    return {
        title: 'Contact Full Name',
        name: 'contactFullName',
        data: 'contactFullName',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getContactTitleColumn(columnIndex) {
    return {
        title: 'Contact Title',
        name: 'contactTitle',
        data: 'contactTitle',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getContactEmailColumn(columnIndex) {
    return {
        title: 'Contact Email',
        name: 'contactEmail',
        data: 'contactEmail',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getContactBusinessPhoneColumn(columnIndex) {
    return {
        title: 'Contact Business Phone',
        name: 'contactBusinessPhone',
        data: 'contactBusinessPhone',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getContactCellPhoneColumn(columnIndex) {
    return {
        title: 'Contact Cell Phone',
        name: 'contactCellPhone',
        data: 'contactCellPhone',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getSectorSubSectorIndustryDescColumn(columnIndex) {
    return {
        title: 'Other Sector/Sub/Industry Description',
        name: 'sectorSubSectorIndustryDesc',
        data: 'applicant.sectorSubSectorIndustryDesc',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getSigningAuthorityFullNameColumn(columnIndex) {
    return {
        title: 'Signing Authority Full Name',
        name: 'signingAuthorityFullName',
        data: 'signingAuthorityFullName',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getSigningAuthorityTitleColumn(columnIndex) {
    return {
        title: 'Signing Authority Title',
        name: 'signingAuthorityTitle',
        data: 'signingAuthorityTitle',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getSigningAuthorityEmailColumn(columnIndex) {
    return {
        title: 'Signing Authority Email',
        name: 'signingAuthorityEmail',
        data: 'signingAuthorityEmail',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getSigningAuthorityBusinessPhoneColumn(columnIndex) {
    return {
        title: 'Signing Authority Business Phone',
        name: 'signingAuthorityBusinessPhone',
        data: 'signingAuthorityBusinessPhone',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getSigningAuthorityCellPhoneColumn(columnIndex) {
    return {
        title: 'Signing Authority Cell Phone',
        name: 'signingAuthorityCellPhone',
        data: 'signingAuthorityCellPhone',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getPlaceColumn(columnIndex) {
    return {
        title: 'Place',
        name: 'place',
        data: 'place',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getRiskRankingColumn(columnIndex) {
    return {
        title: 'Risk Ranking',
        name: 'riskranking',
        data: 'riskRanking',
        className: 'data-table-header',
        render: function (data) {
            return titleCase(data ?? '') ?? '';
        },
        index: columnIndex
    };
}

function getNotesColumn(columnIndex) {
    return {
        title: 'Notes',
        name: 'notes',
        data: 'notes',
        className: 'data-table-header multi-line',
        width: "20rem",
        createdCell: function (td) {
            $(td).css('min-width', '20rem');
        },
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getRedStopColumn(columnIndex) {
    return {
        title: 'Red-Stop',
        name: 'redstop',
        data: 'applicant.redStop',
        className: 'data-table-header',
        render: function (data) {
            return convertToYesNo(data);
        },
        index: columnIndex
    };
}

function getIndigenousColumn(columnIndex) {
    return {
        title: 'Indigenous',
        name: 'indigenous',
        data: 'applicant.indigenousOrgInd',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getFyeDayColumn(columnIndex) {
    return {
        title: 'FYE Day',
        name: 'fyeDay',
        data: 'applicant.fiscalDay',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getFyeMonthColumn(columnIndex) {
    return {
        title: 'FYE Month',
        name: 'fyeMonth',
        data: 'applicant.fiscalMonth',
        className: 'data-table-header',
        render: function (data) {
            if (data) {
                return titleCase(data);
            }
            else {
                return '';
            }
        },
        index: columnIndex
    };
}

function getApplicantIdColumn(columnIndex) {
    return {
        title: 'Applicant Id',
        name: 'applicantId',
        data: 'applicant.unityApplicantId',
        className: 'data-table-header',
        render: function (data) {
            return data ?? '';
        },
        index: columnIndex
    };
}

function getPayoutColumn(columnIndex) {
    return {
        title: 'Payout',
        name: 'paymentInfo',
        data: 'paymentInfo',
        className: 'data-table-header',
        render: function (data) {
            return payoutDefinition(data?.approvedAmount ?? 0, data?.totalPaid ?? 0);
        },
        index: columnIndex
    };
}

function responseCallback(result) {
    return {
        recordsTotal: result.totalCount,
        recordsFiltered: result.totalCount,
        data: formatItems(result.items)
    };
}

function getColumns(formatter, l) {
    let columnIndex = 0;
    const sortedColumns = [
        getSelectColumn(columnIndex++),
        getReferenceNoColumn(columnIndex++),
        getCategoryColumn(columnIndex++, l),
        getSubmissionDateColumn(columnIndex++, l),
        getStatusColumn(columnIndex++, l),
        getRequestedAmountColumn(columnIndex++, l, formatter),
        getApprovedAmountColumn(columnIndex++, formatter),
        getApplicantNameColumn(columnIndex++),
        getProjectNameColumn(columnIndex++),
        getSectorColumn(columnIndex++),
        getSubSectorColumn(columnIndex++),
        getTotalProjectBudgetColumn(columnIndex++, formatter),
        getAssigneesColumn(columnIndex++, l),
        getEconomicRegionColumn(columnIndex++),
        getRegionalDistrictColumn(columnIndex++),
        getCommunityColumn(columnIndex++),
        getOrganizationNumberColumn(columnIndex++, l),
        getOrgBookStatusColumn(columnIndex++),
        getProjectStartDateColumn(columnIndex++),
        getProjectEndDateColumn(columnIndex++),
        getProjectedFundingTotalColumn(columnIndex++, formatter),
        getTotalProjectBudgetPercentageColumn(columnIndex++),
        getTotalPaidAmountColumn(columnIndex++, formatter),
        getElectoralDistrictColumn(columnIndex++),
        getApplicantElectoralDistrictColumn(columnIndex++),
        getForestryOrNonForestryColumn(columnIndex++),
        getForestryFocusColumn(columnIndex++),
        getAcquisitionColumn(columnIndex++),
        getCityColumn(columnIndex++),
        getCommunityPopulationColumn(columnIndex++),
        getLikelihoodOfFundingColumn(columnIndex++),
        getSubStatusColumn(columnIndex++),
        getTagsColumn(columnIndex++),
        getTotalScoreColumn(columnIndex++),
        getAssessmentResultColumn(columnIndex++),
        getRecommendedAmountColumn(columnIndex++, formatter),
        getDueDateColumn(columnIndex++),
        getOwnerColumn(columnIndex++),
        getDecisionDateColumn(columnIndex++),
        getProjectSummaryColumn(columnIndex++),
        getOrganizationTypeColumn(columnIndex++),
        getOrganizationNameColumn(columnIndex++, l),
        getBusinessNumberColumn(columnIndex++, l),
        getDueDiligenceStatusColumn(columnIndex++),
        getDeclineRationaleColumn(columnIndex++),
        getContactFullNameColumn(columnIndex++),
        getContactTitleColumn(columnIndex++),
        getContactEmailColumn(columnIndex++),
        getContactBusinessPhoneColumn(columnIndex++),
        getContactCellPhoneColumn(columnIndex++),
        getSectorSubSectorIndustryDescColumn(columnIndex++),
        getSigningAuthorityFullNameColumn(columnIndex++),
        getSigningAuthorityTitleColumn(columnIndex++),
        getSigningAuthorityEmailColumn(columnIndex++),
        getSigningAuthorityBusinessPhoneColumn(columnIndex++),
        getSigningAuthorityCellPhoneColumn(columnIndex++),
        getPlaceColumn(columnIndex++),
        getRiskRankingColumn(columnIndex++),
        getNotesColumn(columnIndex++),
        getRedStopColumn(columnIndex++),
        getIndigenousColumn(columnIndex++),
        getFyeDayColumn(columnIndex++),
        getFyeMonthColumn(columnIndex++),
        getApplicantIdColumn(columnIndex++),
        getPayoutColumn(columnIndex++),
        getNonRegisteredOrganizationNameColumn(columnIndex++, l),
        getUnityApplicationIdColumn(columnIndex++)
    ].map((column) => ({ ...column, targets: [column.index], orderData: [column.index, 0] }))
        .sort((a, b) => a.index - b.index);
    return sortedColumns;
}

$(function () {
    // Check if createNumberFormatter exists
    if (typeof createNumberFormatter !== 'function') {
        console.error('createNumberFormatter is not defined. Ensure table-utils.js is loaded before this script');
        return;
    }

    const formatter = createNumberFormatter();
    const l = abp.localization.getResource('GrantManager');

    // Default visible columns
    const defaultVisibleColumns = [
        'select',
        'referenceNo',        // Submission #
        'category',           // Category
        'submissionDate',     // Submission Date
        'status',             // Status
        'requestedAmount',    // Requested Amount
        'approvedAmount'      // Approved Amount
    ];

    // Default sort column
    const defaultSortOrderColumn = {
        name: 'submissionDate',
        dir: 'desc'
    };

    // Action buttons configuration
    const actionButtons = [
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

    // Parse embedded data
    const submissionsDataJson = $('#ApplicantSubmissions_Data').val();
    const submissionsData = submissionsDataJson ? JSON.parse(submissionsDataJson) : [];

    // Get all columns
    const listColumns = getColumns(formatter, l);

    // Mock service that returns embedded data (simulating API endpoint)
    // Must return a jQuery Deferred object (not native Promise) for ABP compatibility
    const mockDataService = {
        getList: function() {
            const deferred = $.Deferred();
            deferred.resolve({
                items: submissionsData,
                totalCount: submissionsData.length
            });
            return deferred.promise();
        }
    };

    // Initialize DataTable - same pattern as Application List
    const dataTable = initializeDataTable({
        dt: $('#ApplicantSubmissionsTable'),
        defaultVisibleColumns: defaultVisibleColumns,
        listColumns: listColumns,
        maxRowsPerPage: 10,
        defaultSortColumn: defaultSortOrderColumn,
        dataEndpoint: mockDataService.getList,
        data: function () {
            return {};
        },
        responseCallback: responseCallback,
        actionButtons: actionButtons,
        serverSideEnabled: false,
        pagingEnabled: true,
        reorderEnabled: true,
        dataTableName: 'ApplicantSubmissionsTable',
        dynamicButtonContainerId: 'submissionsDynamicButtonContainerId'
    });

    scheduleLayoutNotifications();
    bindLayoutNotificationEvents(dataTable);

    // External search binding
    dataTable.externalSearch('#submissions-search', { delay: 300 });

    // Open button handling
    dataTable.on('select deselect', function () {
        updateOpenButtonState(dataTable);
    });

    $('#openSubmissionBtn').on('click', function () {
        const selectedRows = dataTable.rows({ selected: true }).data();
        if (selectedRows.length === 1) {
            globalThis.location.href = `/GrantApplications/Details?ApplicationId=${selectedRows[0].id}`;
        }
    });

    // Initialize button state
    updateOpenButtonState(dataTable);

});

