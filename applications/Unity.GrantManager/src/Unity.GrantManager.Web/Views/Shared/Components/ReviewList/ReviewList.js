const l = abp.localization.getResource('GrantManager');
const pageApplicationId = decodeURIComponent(
    document.querySelector('#DetailsViewApplicationId').value
);

const actionButtonConfigMap = {
    Create: { buttonType: 'createButton', order: 1 },
    Complete: { buttonType: 'unityWorkflow', order: 2 },
    SendBack: { buttonType: 'unityWorkflow', order: 3 },
    _Fallback: { buttonType: 'unityWorkflow', order: 100 },
};

const finalApplicationStates = [
    'GRANT_NOT_APPROVED',
    'GRANT_APPROVED',
    'CLOSED',
    'WITHDRAWN',
];

$(function () {
    console.log('Initializing ReviewList Table component');
    const nullPlaceholder = '—';

    let inputAction = function (requestData, dataTableSettings) {
        const applicationId = pageApplicationId;
        return applicationId;
    };

    let responseCallback = function (result) {
        return {
            data: result.data,
            isUsingDefaultScoresheet:
                result.isApplicationUsingDefaultScoresheet,
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

    $.fn.dataTable.Buttons.defaults.dom.button.className = 'btn btn-light';
    $.fn.dataTable.Buttons.defaults.dom.button.liner.tag = false;

    $.extend(DataTable.ext.buttons, {
        unityWorkflow: {
            className: 'btn btn-light',
            enabled: false,
            text: unityWorkflowButtonText,
            action: unityWorkflowButtonAction,
        },
        createButton: {
            extend: 'unityWorkflow',
            init: createButtonInit,
            action: createButtonAction,
        },
    });

    $.fn.dataTable.Api.register('row().selectWithParams()', function (params) {
        this.params = params;
        return this.select();
    });

    const actionArray = getActionArray();

    let assessmentButtonsGroup = {
        name: 'assessmentActionButtons',
        buttons: getButtonArray(actionArray),
    };

    let assessmentCreateButtonGroup = {
        name: 'assessmentCreateButtonsGroup',
        buttons: Array(renderUnityWorkflowButton('Create')),
    };

    const reviewListDiv = 'ReviewListTable';

    let reviewListTable = $('#' + reviewListDiv).DataTable(
        abp.libs.datatables.normalizeConfiguration({
            dom: 'Bfrtip',
            serverSide: false,
            order: [[3, 'asc']],
            searching: false,
            paging: false,
            select: {
                style: 'single',
            },
            info: false,
            scrollX: true,
            lengthChange: false,
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.assessments.assessment.getDisplayList,
                inputAction,
                responseCallback
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
                    render: function (data) {
                        return data ?? nullPlaceholder;
                    },
                },
                {
                    title: l('ReviewerList:StartDate'),
                    data: 'startDate',
                    className: 'data-table-header',
                    render: $.fn.unityPlugin.formatDate,
                },
                {
                    title: l('ReviewerList:EndDate'),
                    data: 'endDate',
                    className: 'data-table-header',
                    render: $.fn.unityPlugin.formatDate,
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
                    width: '60px',
                    data: 'financialAnalysis',
                    className: 'data-table-header',
                },
                {
                    title: l('ReviewerList:EconomicImpact'),
                    width: '60px',
                    data: 'economicImpact',
                    className: 'data-table-header',
                },
                {
                    title: l('ReviewerList:InclusiveGrowth'),
                    width: '60px',
                    data: 'inclusiveGrowth',
                    className: 'data-table-header',
                },
                {
                    title: l('ReviewerList:CleanGrowth'),
                    width: '60px',
                    data: 'cleanGrowth',
                    className: 'data-table-header',
                },
                {
                    title: l('ReviewerList:Subtotal'),
                    width: '60px',
                    className: 'data-table-header',
                    data: 'subTotal',
                },
            ],
        })
    );

    $('#' + reviewListDiv).on('xhr.dt', function (e, settings, json, xhr) {
        if (!json.isUsingDefaultScoresheet) {
            reviewListTable.column(7).visible(false); // 'FinancialAnalysis' column
            reviewListTable.column(8).visible(false); // 'EconomicImpact' column
            reviewListTable.column(9).visible(false); // 'InclusiveGrowth' column
            reviewListTable.column(10).visible(false); // 'CleanGrowth' column
        } else {
            reviewListTable.column(7).visible(true);
            reviewListTable.column(8).visible(true);
            reviewListTable.column(9).visible(true);
            reviewListTable.column(10).visible(true);
        }
    });

    if (
        abp.auth.isGranted(
            'Unity.GrantManager.ApplicationManagement.Review.AssessmentReviewList.Create'
        )
    ) {
        CreateAssessmentButton();
    }

    async function CreateAssessmentButton() {
        let createButtons = new $.fn.dataTable.Buttons(
            reviewListTable,
            assessmentCreateButtonGroup
        );
        createButtons.container().prependTo('#AdjudicationTeamLeadActionBar');
        let isPermitted = await CheckAssessmentCreateButton();
        if (!isPermitted) {
            reviewListTable.buttons('Create:name').disable();
        }
    }
    async function CheckAssessmentCreateButton() {
        let applicationStatus = await getActionButtonConfigMap();
        return !finalApplicationStates.includes(applicationStatus.statusCode);
    }

    reviewListTable
        .buttons(0, null)
        .container()
        .appendTo('#AdjudicationTeamLeadActionBar');
    $('#AdjudicationTeamLeadActionBar .dt-buttons').contents().unwrap();

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

    PubSub.subscribe('assessment_action_completed', (msg, data) => {
        $('#detailsTab a[href="#nav-review-and-assessment"]').tab('show');
    });
    PubSub.subscribe('application_status_changed', async (msg, data) => {
        let isPermitted = await CheckAssessmentCreateButton();
        if (!isPermitted) {
            reviewListTable.buttons('Create:name').disable();
        }
    });

    $('#nav-review-and-assessment-tab').one('click', function () {
        reviewListTable.columns.adjust();
    });
});

function handleRowSelection(e, dt, type, indexes, reviewListTable) {
    let refreshSidePanel = dt?.params?.refreshSidePanel ?? true;
    if (type === 'row') {
        let selectedData = reviewListTable.row(indexes).data();
        document.getElementById('AssessmentId').value = selectedData.id;
        if (refreshSidePanel) {
            PubSub.publish('select_application_review', selectedData);
            PubSub.publish(
                'refresh_assessment_attachment_list',
                selectedData.id
            );
        }
        e.currentTarget.classList.toggle('selected');
        refreshActionButtons(dt, selectedData.id);
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
            let indexes = reviewListTable
                .rows()
                .eq(0)
                .filter(function (rowIdx) {
                    return reviewListTable.cell(rowIdx, 0).data() === data;
                });

            reviewListTable
                .row(indexes)
                .selectWithParams({ refreshSidePanel: refreshSidePanel });
        }
    });
}

function getActionArray() {
    let actionArray = [];
    unity.grantManager.assessments.assessment.getAllActions({
        async: false,
        success: function (data) {
            actionArray.push(...data);
        },
    });
    return actionArray;
}

function getButtonArray(actionArray) {
    return Array.from(actionArray, (item) =>
        renderUnityWorkflowButton(item)
    ).sort((a, b) => a.sortOrder - b.sortOrder);
}

function refreshActionButtons(dataTableContext, assessmentId) {
    dataTableContext.buttons(0, null).disable();

    if (assessmentId) {
        unity.grantManager.assessments.assessment
            .getPermittedActions(assessmentId, {})
            .then(function (actionListResult) {
                // Check permissions
                let enabledButtons = actionListResult.map((x) => x + ':name');
                dataTableContext.buttons(enabledButtons).enable();
            });
    }

    if (typeof CheckAssessmentCreateButton === 'function') {
        let isPermitted = CheckAssessmentCreateButton();
        if (!isPermitted) {
            dataTableContext.buttons('Create:name').disable();
        }
    }
}

function renderApproval(data) {
    if (data !== null) {
        return data === true
            ? 'Recommended for Approval'
            : 'Recommended for Denial';
    } else {
        return nullPlaceholder;
    }
}
async function getActionButtonConfigMap() {
    let applicationId = document.getElementById(
        'DetailsViewApplicationId'
    ).value;
    let applicationStatus =
        await unity.grantManager.grantApplications.grantApplication
            .getApplicationStatus(applicationId)
            .then((data) => {
                return data;
            });
    return applicationStatus;
}
function renderUnityWorkflowButton(actionValue) {
    let buttonConfig =
        actionButtonConfigMap[actionValue] ??
        actionButtonConfigMap['_Fallback'];

    return {
        extend: buttonConfig.buttonType,
        name: actionValue,
        sortOrder: buttonConfig.order ?? 100,
        attr: { id: `${actionValue}Button` },
    };
}

/* Cutom Unity Workflow Buttons */
function unityWorkflowButtonText(dt, button, config) {
    let buttonText = l(`Enum:AssessmentAction.${config.name}`);
    return '<span>' + buttonText + '</span>';
}

function unityWorkflowButtonAction(e, dt, button, config) {
    let selectedRow = dt.rows({ selected: true }).data()[0];
    if (typeof selectedRow === 'object') {
        executeAssessmentAction(selectedRow.id, config.name);
    }
}

function executeAssessmentAction(assessmentId, triggerAction) {
    unity.grantManager.assessments.assessment
        .executeAssessmentAction(assessmentId, triggerAction, {})
        .then(function (result) {
            PubSub.publish('assessment_action_completed');
            PubSub.publish('refresh_review_list', assessmentId);
            abp.notify.success(
                'Completed Successfully',
                l(`Enum:AssessmentAction.${triggerAction}`)
            );
        });
}

function createButtonInit(dt, button, config) {
    let that = this;
    unity.grantManager.assessments.assessment
        .getCurrentUserAssessmentId(pageApplicationId, {})
        .done(function (data) {
            if (data == null) {
                that.enable();
            } else {
                that.disable();
            }
        });
}

function createButtonAction(e, dt, button, config) {
    unity.grantManager.assessments.assessment
        .create({ applicationId: pageApplicationId }, {})
        .done(function (data) {
            PubSub.publish('assessment_action_completed');
            PubSub.publish('refresh_review_list', data.id);
            PubSub.publish('application_status_changed');
            PubSub.publish('refresh_detail_panel_summary');
            PubSub.publish('init_date_pickers');
        });
    this.disable();
}

///For lazy loading start here///////
window.initializeReviewListTable = function () {
    console.log('Re-initializing ReviewList table for lazy loading');

    // Check if table element exists
    if ($('#ReviewListTable').length === 0) {
        console.error('ReviewListTable not found');
        return;
    }

    // If table already exists, destroy it first
    if ($.fn.DataTable.isDataTable('#ReviewListTable')) {
        $('#ReviewListTable').DataTable().destroy();
    }

    // Clear any existing buttons
    $('#AdjudicationTeamLeadActionBar').empty();

    // Re-run the initialization code from the $(function() {}) block
    setTimeout(() => {
        const nullPlaceholder = '—';
        const reviewListDiv = 'ReviewListTable';

        let inputAction = function (requestData, dataTableSettings) {
            const applicationId = pageApplicationId;
            return applicationId;
        };

        let responseCallback = function (result) {
            return {
                data: result.data,
                isUsingDefaultScoresheet:
                    result.isApplicationUsingDefaultScoresheet,
            };
        };

        // Set up DataTable plugins
        $.fn.unityPlugin = {
            formatDate: function (data) {
                if (data === null) return nullPlaceholder;
                return luxon.DateTime.fromISO(data, {
                    locale: abp.localization.currentCulture.name,
                }).toLocaleString();
            },
            renderEnum: (data) => l('Enum:AssessmentState.' + data),
        };

        const actionArray = getActionArray();
        let assessmentButtonsGroup = {
            name: 'assessmentActionButtons',
            buttons: getButtonArray(actionArray),
        };

        let assessmentCreateButtonGroup = {
            name: 'assessmentCreateButtonsGroup',
            buttons: Array(renderUnityWorkflowButton('Create')),
        };

        // Create the DataTable
        let reviewListTable = $('#' + reviewListDiv).DataTable(
            abp.libs.datatables.normalizeConfiguration({
                dom: 'Bfrtip',
                serverSide: false,
                order: [[3, 'asc']],
                searching: false,
                paging: false,
                select: { style: 'single' },
                info: false,
                scrollX: true,
                lengthChange: false,
                ajax: abp.libs.datatables.createAjax(
                    unity.grantManager.assessments.assessment.getDisplayList,
                    inputAction,
                    responseCallback
                ),
                buttons: assessmentButtonsGroup,
                columnDefs: [
                    { title: '', data: 'id', visible: false },
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
                        render: function (data) {
                            return data ?? nullPlaceholder;
                        },
                    },
                    {
                        title: l('ReviewerList:StartDate'),
                        data: 'startDate',
                        className: 'data-table-header',
                        render: $.fn.unityPlugin.formatDate,
                    },
                    {
                        title: l('ReviewerList:EndDate'),
                        data: 'endDate',
                        className: 'data-table-header',
                        render: $.fn.unityPlugin.formatDate,
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
                        width: '60px',
                        data: 'financialAnalysis',
                        className: 'data-table-header',
                    },
                    {
                        title: l('ReviewerList:EconomicImpact'),
                        width: '60px',
                        data: 'economicImpact',
                        className: 'data-table-header',
                    },
                    {
                        title: l('ReviewerList:InclusiveGrowth'),
                        width: '60px',
                        data: 'inclusiveGrowth',
                        className: 'data-table-header',
                    },
                    {
                        title: l('ReviewerList:CleanGrowth'),
                        width: '60px',
                        data: 'cleanGrowth',
                        className: 'data-table-header',
                    },
                    {
                        title: l('ReviewerList:Subtotal'),
                        width: '60px',
                        className: 'data-table-header',
                        data: 'subTotal',
                    },
                ],
            })
        );

        // Set up events and handlers
        $('#' + reviewListDiv).on('xhr.dt', function (e, settings, json, xhr) {
            if (!json.isUsingDefaultScoresheet) {
                reviewListTable.column(7).visible(false);
                reviewListTable.column(8).visible(false);
                reviewListTable.column(9).visible(false);
                reviewListTable.column(10).visible(false);
            } else {
                reviewListTable.column(7).visible(true);
                reviewListTable.column(8).visible(true);
                reviewListTable.column(9).visible(true);
                reviewListTable.column(10).visible(true);
            }
        });

        if (
            abp.auth.isGranted(
                'Unity.GrantManager.ApplicationManagement.Review.AssessmentReviewList.Create'
            )
        ) {
            CreateAssessmentButton();
        }

        async function CreateAssessmentButton() {
            let createButtons = new $.fn.dataTable.Buttons(
                reviewListTable,
                assessmentCreateButtonGroup
            );
            createButtons
                .container()
                .prependTo('#AdjudicationTeamLeadActionBar');
            let isPermitted = await CheckAssessmentCreateButton();
            if (!isPermitted) {
                reviewListTable.buttons('Create:name').disable();
            }
        }

        async function CheckAssessmentCreateButton() {
            let applicationStatus = await getActionButtonConfigMap();
            return !finalApplicationStates.includes(
                applicationStatus.statusCode
            );
        }

        // Main buttons setup
        reviewListTable
            .buttons(0, null)
            .container()
            .appendTo('#AdjudicationTeamLeadActionBar');
        $('#AdjudicationTeamLeadActionBar .dt-buttons').contents().unwrap();

        // Event handlers
        reviewListTable.on('select', function (e, dt, type, indexes) {
            handleRowSelection(e, dt, type, indexes, reviewListTable);
        });

        reviewListTable.on('deselect', function (e, dt, type, indexes) {
            handleRowDeselection(e, dt, type, indexes, reviewListTable);
        });

        // PubSub subscriptions - ADD THESE MISSING SUBSCRIPTIONS
        PubSub.subscribe('refresh_review_list', (msg, data) => {
            refreshReviewList(data, reviewListTable);
        });

        PubSub.subscribe(
            'refresh_review_list_without_sidepanel',
            (msg, data) => {
                refreshReviewList(data, reviewListTable, false);
            }
        );

        PubSub.subscribe('assessment_action_completed', (msg, data) => {
            $('#detailsTab a[href="#nav-review-and-assessment"]').tab('show');
        });

        PubSub.subscribe('application_status_changed', async (msg, data) => {
            let isPermitted = await CheckAssessmentCreateButton();
            if (!isPermitted) {
                reviewListTable.buttons('Create:name').disable();
            }
        });

        $('#nav-review-and-assessment-tab').one('click', function () {
            if (reviewListTable) {
                reviewListTable.columns.adjust();
            }
        });

        console.log(
            'ReviewList table re-initialized successfully with buttons'
        );
    }, 100);
};
///For lazy loading end here///////
