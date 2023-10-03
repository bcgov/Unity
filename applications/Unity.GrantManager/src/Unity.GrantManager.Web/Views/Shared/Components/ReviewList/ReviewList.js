$(function () {
    const l = abp.localization.getResource('GrantManager');
    let inputAction = function (requestData, dataTableSettings) {
        const urlParams = new URL(window.location.toLocaleString()).searchParams;
        const applicationId = urlParams.get('ApplicationId');
        return applicationId;
    }

    let responseCallback = function (result) {
        return {
            data: result
        };
    };

    $.fn.unityPlugin = {};
    $.fn.unityPlugin.formatDate = function (data) {
        if (data === null) return '—';

        return luxon.DateTime.fromISO(data, {
            locale: abp.localization.currentCulture.name,
        }).toLocaleString();
    }

    $.fn.unityPlugin.renderEnum = (data) => l('Enum:AssessmentState.' + data);

    $.fn.dataTable.Buttons.defaults.dom.button.className = 'btn btn-light';
    $.fn.dataTable.Buttons.defaults.dom.button.liner.tag = false;

    $.extend(DataTable.ext.buttons, {
        unityWorkflow: {
            className: 'btn btn-light',
            buttonIcon: 'fl-review',
            enabled: false,
            text: function (dt, button, config) {
                let buttonIcon = `<i class="fl ${config.buttonIcon}"></i>`;
                let buttonText = l(`Enum:AssessmentAction.${config.name}`);
                return buttonIcon + '<span>' + buttonText + '</span>'
            },
            action: function (e, dt, button, config) {
                let selectedRow = dt.rows({ selected: true }).data()[0];
                if (typeof (selectedRow) === 'object') {
                    unity.grantManager.assessments.assessment.executeAssessmentAction(selectedRow.id, config.name, {})
                        .then(function (result) {
                            PubSub.publish('refresh_review_list', selectedRow.id);
                            abp.notify.success(
                                String(result),
                                l(`Enum:AssessmentAction.${config.name}`)
                            );
                        });
                }
            }
        },
        createButton: {
            extend: 'unityWorkflow',
            init: function (dt, button, config) {
                var that = this;
                unity.grantManager.assessments.assessment.getCurrentUserAssessmentId($("#PageApplicationId").val(), {})
                    .done(function (data) {
                        if (data == null) {
                            that.enable();
                        } else {
                            that.disable();
                            PubSub.publish('refresh_review_list', data);
                        }
                    });
            },
            action: function (e, dt, button, config) {
                let applicationId = decodeURIComponent($("#PageApplicationId").val());
                unity.grantManager.assessments.assessment.createAsync({ "applicationId": applicationId }, {})
                    .done(function (data) {
                        PubSub.publish('add_review');
                        PubSub.publish('refresh_review_list', data.id);
                    });
                this.disable();
            }
        }
    });

    let actionArray = [];
    // NOTE: FIND A BETTER WAY OF DOING THIS USING PROMISES
    unity.grantManager.assessments.assessment.getAllActions({
        async: false,
        success: function (data) {
            actionArray.push(...data);
        }
    });

    const actionButtonConfigMap = {
        Create:         { buttonType: 'createButton', order: 1, icon: 'fl-review' },
        SendToTeamLead: { buttonType: 'unityWorkflow', order: 2, icon: 'fl-send' },
        SendBack:       { buttonType: 'unityWorkflow', order: 3, icon: 'fl-send-mirrored' },
        Confirm:        { buttonType: 'unityWorkflow', order: 4, icon: 'fl-checkbox-checked' },
        _Fallback:      { buttonType: 'unityWorkflow', order: 100, icon: 'fl-endpoint'}
    }

    let renderUnityWorkflowButton = function (actionValue) {
        let buttonConfig = actionButtonConfigMap[actionValue] ?? actionButtonConfigMap['_Fallback']

        return {
            extend: buttonConfig.buttonType,
            name: actionValue,
            sortOrder: buttonConfig.order ?? 100,
            buttonIcon: buttonConfig.icon,
            attr: { id: `${actionValue}Button`}
        };
    }
    
    let buttonArray = Array.from(actionArray, (item) => renderUnityWorkflowButton(item))
        .sort((a, b) => a.sortOrder - b.sortOrder);

    let assessmentButtonsGroup = {
        name: 'assessmentActionButtons',
        buttons: buttonArray
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
                unity.grantManager.assessments.assessment.getList, inputAction, responseCallback
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
                    title: l('ReviewerList:ReviewerName'),
                    data: 'assessorName',
                    className: 'data-table-header',
                    render: function (data) {
                        return data || '';
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
                    render: function (data) {
                        if (data !== null) {
                            return data === true ? 'Recommended for Approval' : 'Recommended for Denial'
                        } else {
                            return '';
                        }                        
                    },
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

    let refreshActionButtons = function (dataTableContext, assessmentId) {
        dataTableContext.buttons(0, null).disable();

        if (assessmentId)
        {
            unity.grantManager.assessments.assessment.getAvailableActions(assessmentId, {})
                .then(function (actionListResult) {
                    // Check permissions
                    let enabledButtons = actionListResult.map((x) => x + ':name');
                    dataTableContext.buttons(enabledButtons).enable();
                });
        }
    }

    reviewListTable.on('select', function (e, dt, type, indexes) {
        if (type === 'row') {
            let selectedData = reviewListTable.row(indexes).data();
            PubSub.publish('select_application_review', selectedData);
            e.currentTarget.classList.toggle('selected');
            refreshActionButtons(dt, selectedData.id);
        }
    });

    reviewListTable.on('deselect', function (e, dt, type, indexes) {
        if (type === 'row') {
            let deselectedData = reviewListTable.row(indexes).data();
            PubSub.publish('deselect_application_review', deselectedData);
            e.currentTarget.classList.toggle('selected');
            refreshActionButtons(dt, null);
        }
    });

    PubSub.subscribe(
        'refresh_review_list',
         (msg, data) => {
             reviewListTable.ajax.reload(function (json) {
                 if (data) {
                     let indexes = reviewListTable.rows().eq(0).filter(function (rowIdx) {
                         return reviewListTable.cell(rowIdx, 0).data() === data;
                     });

                     reviewListTable.row(indexes).select();
                 }
             });
        
        }
    );
});
