const l = abp.localization.getResource('GrantManager');
const pageApplicationId = decodeURIComponent(document.querySelector("#DetailsViewApplicationId").value);
const isAiScoringEnabled = document.querySelector("#ReviewListAIScoringEnabled")?.value === 'True';
const canUseAiScoring = isAiScoringEnabled;

const actionButtonConfigMap = {
    Generate: { buttonType: 'generateAiButton', order: 1 },
    Clone: { buttonType: 'cloneButton', order: 2 },
    Create: { buttonType: 'createButton', order: 3 },
    SendBack: { buttonType: 'unityWorkflow', order: 4 },
    Complete: { buttonType: 'unityWorkflow', order: 5 },
    _Fallback: { buttonType: 'unityWorkflow', order: 100 }
}

const actionButtonLabelMap = {
    Generate: 'Generate',
    Clone: 'Clone',
    Create: 'Create',
    SendBack: 'Send Back',
    Complete: 'Complete'
};

const finalApplicationStates = [
    'GRANT_NOT_APPROVED',
    'GRANT_APPROVED',
    'CLOSED',
    'WITHDRAWN'
];

$(function () {
    const nullPlaceholder = '—';

    let inputAction = function (requestData, dataTableSettings) {
        const applicationId = pageApplicationId
        return applicationId;
    }

    let responseCallback = function (result) {
        return {
            data: result.data,
            isUsingDefaultScoresheet: result.isApplicationUsingDefaultScoresheet
        };
    };

    $.fn.unityPlugin = {
        formatDate: function (data) {
            if (data === null) return nullPlaceholder;
            return luxon.DateTime.fromISO(data, {
                locale: abp.localization.currentCulture.name,
            }).toLocaleString();
        },
        renderEnum: (data) => l('Enum:AssessmentState.' + data),
    };

    $.extend(DataTable.ext.buttons, {
        unityWorkflow: {
            className: 'btn unt-btn-outline-primary btn-outline-primary',
            enabled: false,
            text: unityWorkflowButtonText,
            action: unityWorkflowButtonAction
        },
        generateAiButton: {
            extend: 'unityWorkflow',
            text: generateAiButtonText,
            action: generateAiButtonAction
        },
        createButton: {
            extend: 'unityWorkflow',
            init: createButtonInit,
            action: createButtonAction
        },
        cloneButton: {
            extend: 'unityWorkflow',
            text: cloneButtonText,
            action: cloneButtonAction
        }
    });

    $.fn.dataTable.Api.register('row().selectWithParams()', function (params) {
        this.params = params;
        return this.select();
    });

    const actionArray = getActionArray();

    let assessmentButtonsGroup = {
        name: 'assessmentActionButtons',
        buttons: getButtonArray(actionArray)
    };

    let assessmentGenerateButtonGroup = {
        name: 'assessmentGenerateButtonsGroup',
        buttons: new Array(renderUnityWorkflowButton('Generate'))
    };

    let assessmentCreateButtonGroup = {
        name: 'assessmentCreateButtonsGroup',
        buttons: new Array(renderUnityWorkflowButton('Create'))
    };

    let assessmentCloneButtonGroup = {
        name: 'assessmentCloneButtonsGroup',
        buttons: new Array(renderUnityWorkflowButton('Clone'))
    };

    const reviewListDiv = "ReviewListTable";

    let reviewListTable = $('#' + reviewListDiv).DataTable(
        abp.libs.datatables.normalizeConfiguration({
            dom: 'Bfrtip',
            serverSide: false,
            order: [[3, 'asc']],
            searching: false,
            paging: false,
            select: {
                style: 'single'
            },
            info: false,
            scrollX: true,
            lengthChange: false,
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.assessments.assessment.getDisplayList, inputAction, responseCallback
            ),
            buttons: assessmentButtonsGroup,
            columnDefs: [
                {
                    title: '',
                    data: 'id',
                    visible: false,
                },
                {
                    title: '<i class="fl fl-review-user" ></i>',
                    orderable: false,
                    render: function (data) {
                        return '<i class="fl fl-review-user" ></i>';
                    },
                },
                {
                    title: l('ReviewerList:AssessorName'),
                    data: 'assessorFullName',
                    className: 'data-table-header',
                    render: function (data, type, row) {
                        let name = data ?? nullPlaceholder;
                        if (row.isAiAssessment) {
                            name = '<span class="badge bg-info me-1">AI</span>' + name;
                        }
                        return name;
                    },
                },
                {
                    title: l('ReviewerList:StartDate'),
                    data: 'startDate',
                    className: 'data-table-header',
                    render: $.fn.unityPlugin.formatDate
                },
                {
                    title: l('ReviewerList:EndDate'),
                    data: 'endDate',
                    className: 'data-table-header',
                    render: $.fn.unityPlugin.formatDate
                },
                {
                    title: l('ReviewerList:Status'),
                    data: 'status',
                    className: 'data-table-header',
                    render: $.fn.unityPlugin.renderEnum,
                },
                {
                    title: l('ReviewerList:Recommended'),
                    data: 'approvalRecommended',
                    className: 'data-table-header',
                    render: renderApproval,
                },
                {
                    title: l('ReviewerList:FinancialAnalysis'),
                    width: "60px",
                    data: 'financialAnalysis',
                    className: 'data-table-header',
                },
                {
                    title: l('ReviewerList:EconomicImpact'),
                    width: "60px",
                    data: 'economicImpact',
                    className: 'data-table-header',
                },
                {
                    title: l('ReviewerList:InclusiveGrowth'),
                    width: "60px",
                    data: 'inclusiveGrowth',
                    className: 'data-table-header',
                },
                {
                    title: l('ReviewerList:CleanGrowth'),
                    width: "60px",
                    data: 'cleanGrowth',
                    className: 'data-table-header',
                },
                {
                    title: l('ReviewerList:Subtotal'),
                    width: "60px",
                    className: 'data-table-header',
                    data: 'subTotal',
                }
            ],
        })
    );

    $('#' + reviewListDiv).on('xhr.dt', function (e, settings, json, xhr) {
        if (!json.isUsingDefaultScoresheet) {
            reviewListTable.column(7).visible(false);  // 'FinancialAnalysis' column
            reviewListTable.column(8).visible(false);  // 'EconomicImpact' column
            reviewListTable.column(9).visible(false);  // 'InclusiveGrowth' column
            reviewListTable.column(10).visible(false); // 'CleanGrowth' column
        } else {
            reviewListTable.column(7).visible(true);
            reviewListTable.column(8).visible(true);
            reviewListTable.column(9).visible(true);
            reviewListTable.column(10).visible(true);
        }

        updateAiActionButtonsVisibility(reviewListTable, json.data ?? []);
    });

    if (canUseAiScoring) {
        GenerateAiAssessmentButton();
    }

    if (canUseAiScoring) {
        CloneAssessmentButton();
    }

    if (abp.auth.isGranted('Unity.GrantManager.ApplicationManagement.Review.AssessmentReviewList.Create')) {
        CreateAssessmentButton();
    }

    function GenerateAiAssessmentButton() {
        let generateButtons = new $.fn.dataTable.Buttons(reviewListTable, assessmentGenerateButtonGroup);
        generateButtons.container().appendTo("#AdjudicationTeamLeadActionBar");
        reviewListTable.buttons('Generate:name').enable();
    }

    async function CreateAssessmentButton() {
        let createButtons = new $.fn.dataTable.Buttons(reviewListTable, assessmentCreateButtonGroup);
        createButtons.container().appendTo("#AdjudicationTeamLeadActionBar");
        await updateCreateButtonState(reviewListTable);
    }

    function CloneAssessmentButton() {
        let cloneButtons = new $.fn.dataTable.Buttons(reviewListTable, assessmentCloneButtonGroup);
        cloneButtons.container().appendTo("#AdjudicationTeamLeadActionBar");
        reviewListTable.buttons('Clone:name').disable();
    }

    reviewListTable.buttons(0, null).container().appendTo("#AdjudicationTeamLeadActionBar");
    $("#AdjudicationTeamLeadActionBar .dt-buttons").contents().unwrap();
    updateAiActionButtonsVisibility(reviewListTable);

    reviewListTable.on('select', function (e, dt, type, indexes) {
        handleRowSelection(e, dt, type, indexes, reviewListTable);
    });

    reviewListTable.on('deselect', function (e, dt, type, indexes) {
        handleRowDeselection(e, dt, type, indexes, reviewListTable);
    });

    PubSub.subscribe('refresh_review_list', (msg, data) => {
        refreshReviewList(data, reviewListTable);
    });

    PubSub.subscribe('refresh_review_list_without_sidepanel', (msg, data) => {
        refreshReviewList(data, reviewListTable, false);
    });

    PubSub.subscribe(
        'assessment_action_completed',
        (msg, data) => {
            $('#detailsTab a[href="#nav-review-and-assessment"]').tab('show');
        }
    );
    PubSub.subscribe(
        'application_status_changed',
        async (msg, data) => {
            await updateCreateButtonState(reviewListTable);
        }
    );

    $('#nav-review-and-assessment-tab').one('click', function () {
        reviewListTable.columns.adjust();
    });
});

function handleRowSelection(e, dt, type, indexes, reviewListTable) {
    let refreshSidePanel = dt?.params?.refreshSidePanel ?? true;
    if (type === 'row') {
        let selectedData = reviewListTable.row(indexes).data();
        document.getElementById("AssessmentId").value = selectedData.id;
        if (refreshSidePanel) {
            PubSub.publish('select_application_review', selectedData);
            PubSub.publish('refresh_assessment_attachment_list', selectedData.id);
        }
        e.currentTarget.classList.toggle('selected');
        refreshActionButtons(dt, selectedData.id, selectedData);
    }
}

function handleRowDeselection(e, dt, type, indexes, reviewListTable) {
    if (type === 'row') {
        let deselectedData = reviewListTable.row(indexes).data();
        PubSub.publish('deselect_application_review', deselectedData);
        e.currentTarget.classList.toggle('selected');
        refreshActionButtons(dt, null);
    }
}

function refreshReviewList(data, reviewListTable, refreshSidePanel = true) {
    reviewListTable.ajax.reload(function (json) {
        if (data) {
            let indexes = reviewListTable.rows().eq(0).filter(function (rowIdx) {
                return reviewListTable.cell(rowIdx, 0).data() === data;
            });

            reviewListTable.row(indexes).selectWithParams({ refreshSidePanel: refreshSidePanel });
        }
    });
}

function getActionArray() {
    let actionArray = [];
    unity.grantManager.assessments.assessment.getAllActions({
        async: false,
        success: function (data) {
            actionArray.push(...data);
        }
    });
    return actionArray;
}

function getButtonArray(actionArray) {
    return Array.from(actionArray, (item) => renderUnityWorkflowButton(item))
        .sort((a, b) => a.sortOrder - b.sortOrder);
}

function refreshActionButtons(dataTableContext, assessmentId, selectedData) {
    dataTableContext.buttons(0, null).disable();
    dataTableContext.buttons('Clone:name').disable();

    if (assessmentId) {
        if (!selectedData?.isAiAssessment) {
            // Human assessment: enable workflow buttons based on permitted actions
            unity.grantManager.assessments.assessment.getPermittedActions(assessmentId, {})
                .then(function (actionListResult) {
                    let enabledButtons = actionListResult.map((x) => x + ':name');
                    dataTableContext.buttons(enabledButtons).enable();
                });
        }
    }

    updateCloneButtonState(dataTableContext);
    updateCreateButtonState(dataTableContext);
}

function renderApproval(data) {
    if (data !== null) {
        return data === true ? 'Recommended for Approval' : 'Recommended for Denial'
    } else {
        return nullPlaceholder;
    }
}
async function getActionButtonConfigMap() {
    let applicationId = document.getElementById('DetailsViewApplicationId').value;
    let applicationStatus = await unity.grantManager.grantApplications.grantApplication.getApplicationStatus(applicationId).then(data => {
        return data;
    });
    return applicationStatus;
}

async function canCreateAssessment() {
    const applicationStatus = await getActionButtonConfigMap();
    return !finalApplicationStates.includes(applicationStatus.statusCode);
}

async function updateCreateButtonState(dataTableContext) {
    if (!dataTableContext.button('Create:name').any()) {
        return;
    }

    const [isPermittedByStatus, currentAssessmentId] = await Promise.all([
        canCreateAssessment(),
        unity.grantManager.assessments.assessment.getCurrentUserAssessmentId(pageApplicationId, {})
    ]);

    if (isPermittedByStatus && currentAssessmentId == null) {
        dataTableContext.buttons('Create:name').enable();
    } else {
        dataTableContext.buttons('Create:name').disable();
    }
}

async function updateCloneButtonState(dataTableContext) {
    if (!dataTableContext.button('Clone:name').any()) {
        return;
    }

    const hasAiAssessment = dataTableContext.rows().data().toArray().some(row => row?.isAiAssessment === true);
    const currentAssessmentId = await unity.grantManager.assessments.assessment.getCurrentUserAssessmentId(pageApplicationId, {});

    if (hasAiAssessment && currentAssessmentId == null) {
        dataTableContext.buttons('Clone:name').enable();
    } else {
        dataTableContext.buttons('Clone:name').disable();
    }
}

function updateAiActionButtonsVisibility(dataTableContext, rowsData) {
    const rowData = rowsData ?? dataTableContext.rows().data().toArray();
    const hasAiAssessment = rowData.some(row => row?.isAiAssessment === true);

    if (dataTableContext.button('Generate:name').any()) {
        $('#GenerateButton').toggle(!hasAiAssessment);
    }

    if (dataTableContext.button('Clone:name').any()) {
        $('#CloneButton').toggle(hasAiAssessment);
    }
}
function renderUnityWorkflowButton(actionValue) {
    let buttonConfig = actionButtonConfigMap[actionValue] ?? actionButtonConfigMap['_Fallback']

    return {
        extend: buttonConfig.buttonType,
        name: actionValue,
        sortOrder: buttonConfig.order ?? 100,
        attr: { id: `${actionValue}Button` }
    };
}

/* Cutom Unity Workflow Buttons */
function unityWorkflowButtonText(dt, button, config) {
    let buttonText = actionButtonLabelMap[config.name] ?? l(`Enum:AssessmentAction.${config.name}`);
    return '<span>' + buttonText + '</span>';
}

function cloneButtonText(dt, button, config) {
    return '<span class="ai-button-content"><i class="unt-icon-sm fa-solid fa-wand-sparkles"></i><span>' + actionButtonLabelMap.Clone + '</span></span>';
}

function generateAiButtonText(dt, button, config) {
    return '<span class="ai-button-content"><i class="unt-icon-sm fa-solid fa-wand-sparkles"></i><span>Generate</span></span>';
}

function unityWorkflowButtonAction(e, dt, button, config) {
    let selectedRow = dt.rows({ selected: true }).data()[0];
    if (typeof (selectedRow) === 'object') {
        executeAssessmentAction(selectedRow.id, config.name);
    }
}

function generateAiButtonAction(e, dt, button, config) {
    const $btn = $(this.node());
    const promptVersion = globalThis.getSelectedPromptVersion?.() || null;

    this.disable();
    $btn.html('<span class="ai-button-content"><span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span><span>Queueing...</span></span>');

    unity.grantManager.grantApplications.applicationScoring.generateApplicationScoring(pageApplicationId, promptVersion)
        .done(function () {
            abp.notify.success('AI scoring queued. Refresh later to see updated results.');
        })
        .fail(function () {
            abp.message.error('Failed to queue AI scoring. Please try again.');
        })
        .always(() => {
            this.enable();
            $btn.html(generateAiButtonText(null, null, null));
        });
}

function executeAssessmentAction(assessmentId, triggerAction) {
    unity.grantManager.assessments.assessment.executeAssessmentAction(assessmentId, triggerAction, {})
        .then(function (result) {
            PubSub.publish('assessment_action_completed');
            PubSub.publish('refresh_review_list', assessmentId);
            abp.notify.success(
                "Completed Successfully",
                l(`Enum:AssessmentAction.${triggerAction}`)
            );
        });
}

function createButtonInit(dt, button, config) {
    let that = this;
    unity.grantManager.assessments.assessment.getCurrentUserAssessmentId(pageApplicationId, {})
        .done(function (data) {
            if (data == null) {
                that.enable();
            } else {
                that.disable();
            }
        });
}

function cloneButtonAction(e, dt, button, config) {
    const aiRowData = dt.rows().data().toArray().find(row => row?.isAiAssessment === true);

    if (typeof (aiRowData) === 'object') {
        unity.grantManager.assessments.assessment.cloneFromAi(aiRowData.id, {})
            .done(function (data) {
                dt.buttons('Create:name').disable();
                dt.buttons('Clone:name').disable();
                PubSub.publish('assessment_action_completed');
                PubSub.publish('refresh_review_list', data.id);
                PubSub.publish("application_status_changed");
                PubSub.publish("refresh_detail_panel_summary");
                abp.notify.success(
                    l('ReviewerList:CloneAssessment'),
                    "Completed Successfully"
                );
            });
    }
}

function createButtonAction(e, dt, button, config) {
    unity.grantManager.assessments.assessment.create({ "applicationId": pageApplicationId }, {})
        .done(function (data) {
            PubSub.publish('assessment_action_completed');
            PubSub.publish('refresh_review_list', data.id);
            PubSub.publish("application_status_changed");
            PubSub.publish("refresh_detail_panel_summary");
            PubSub.publish("init_date_pickers");
        });
    this.disable();
}
