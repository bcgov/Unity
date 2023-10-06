const l = abp.localization.getResource('GrantManager');
const pageApplicationId = decodeURIComponent(document.querySelector("#DetailsViewApplicationId").value);
const nullPlaceholder = '—';

const actionButtonConfigMap = {
    Create: { buttonType: 'createButton', order: 1, icon: 'fl-review' },
    SendToTeamLead: { buttonType: 'unityWorkflow', order: 2, icon: 'fl-send' },
    SendBack: { buttonType: 'unityWorkflow', order: 3, icon: 'fl-send-mirrored' },
    Confirm: { buttonType: 'unityWorkflow', order: 4, icon: 'fl-checkbox-checked' },
    _Fallback: { buttonType: 'unityWorkflow', order: 100, icon: 'fl-endpoint' }
}

$(function () {
    
    let inputAction = function (requestData, dataTableSettings) {
        const applicationId = pageApplicationId
        return applicationId;
    }

    let responseCallback = function (result) {
        return {
            data: result
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
            buttonIcon: 'fl-review',
            enabled: false,
            text: unityWorkflowButtonText,
            action: unityWorkflowButtonAction
        },
        createButton: {
            extend: 'unityWorkflow',
            init: createButtonInit,
            action: createButtonAction
        }
    });

    const actionArray = getActionArray();

    let assessmentButtonsGroup = {
        name: 'assessmentActionButtons',
        buttons: getButtonArray(actionArray)
    };

    let assessmentCreateButtonGroup = {
        name: 'assessmentCreateButtonsGroup',
        buttons: Array(renderUnityWorkflowButton('Create'))
    };

    let reviewListTable = $('#ReviewListTable').DataTable(
        abp.libs.datatables.normalizeConfiguration({
            dom: 'Bfrtip',
            serverSide: true,
            order: [],
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
                    data: 'assessorDisplayName',
                    className: 'data-table-header',
                    render: function (data) {
                        return data ?? nullPlaceholder;
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
                }
            ],
        })
    );

    if (abp.auth.isGranted('GrantApplicationManagement.Assessments.Create')) {
        let createButtons = new $.fn.dataTable.Buttons(reviewListTable, assessmentCreateButtonGroup);
        createButtons.container().appendTo("#DetailsActionBarStart");
    }

    reviewListTable.buttons(0, null).container().appendTo("#DetailsActionBarStart");
    $("#DetailsActionBarStart .dt-buttons").contents().unwrap();

    reviewListTable.on('select', function (e, dt, type, indexes) {
        handleRowSelection(e, dt, type, indexes, reviewListTable);
    });

    reviewListTable.on('deselect', function (e, dt, type, indexes) {
        handleRowDeselection(e, dt, type, indexes, reviewListTable);
    });

    PubSub.subscribe('refresh_review_list', (msg, data) => {
        refreshReviewList(data, reviewListTable);
    });

    PubSub.subscribe(
        'assessment_action_completed',
        (msg, data) => {
            $('#detailsTab a[href="#nav-review-and-assessment"]').tab('show');
        }
    );
});

function handleRowSelection(e, dt, type, indexes, reviewListTable) {
    if (type === 'row') {
        let selectedData = reviewListTable.row(indexes).data();
            console.log('Selected Data:', selectedData);
            document.getElementById("AssessmentId").value = selectedData.id;
        PubSub.publish('select_application_review', selectedData);
            PubSub.publish('refresh_assessment_attachment_list', selectedData.id);
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

function refreshReviewList(data, reviewListTable) {
    reviewListTable.ajax.reload(function (json) {
        if (data) {
            let indexes = reviewListTable.rows().eq(0).filter(function (rowIdx) {
                return reviewListTable.cell(rowIdx, 0).data() === data;
            });

            reviewListTable.row(indexes).select();
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

function refreshActionButtons(dataTableContext, assessmentId) {
    dataTableContext.buttons(0, null).disable();

    if (assessmentId) {
        unity.grantManager.assessments.assessment.getPermittedActions(assessmentId, {})
            .then(function (actionListResult) {
                // Check permissions
                let enabledButtons = actionListResult.map((x) => x + ':name');
                dataTableContext.buttons(enabledButtons).enable();
            });
    }
}

function renderApproval(data) {
    if (data !== null) {
        return data === true ? 'Recommended for Approval' : 'Recommended for Denial'
    } else {
        return nullPlaceholder;
    }
}

function renderUnityWorkflowButton(actionValue) {
    let buttonConfig = actionButtonConfigMap[actionValue] ?? actionButtonConfigMap['_Fallback']

    return {
        extend: buttonConfig.buttonType,
        name: actionValue,
        sortOrder: buttonConfig.order ?? 100,
        buttonIcon: buttonConfig.icon,
        attr: { id: `${actionValue}Button` }
    };
}

/* Cutom Unity Workflow Buttons */
function unityWorkflowButtonText(dt, button, config) {
    let buttonIcon = `<i class="fl ${config.buttonIcon}"></i>`;
    let buttonText = l(`Enum:AssessmentAction.${config.name}`);
    return buttonIcon + '<span>' + buttonText + '</span>';
}

function unityWorkflowButtonAction(e, dt, button, config) {
    let selectedRow = dt.rows({ selected: true }).data()[0];
    if (typeof (selectedRow) === 'object') {
        executeAssessmentAction(selectedRow.id, config.name);
    }
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
                PubSub.publish('refresh_review_list', data);
            }
        });
}

function createButtonAction(e, dt, button, config) {
    unity.grantManager.assessments.assessment.create({ "applicationId": pageApplicationId }, {})
        .done(function (data) {
            PubSub.publish('assessment_action_completed');
            PubSub.publish('refresh_review_list', data.id);
        });
    this.disable();
}