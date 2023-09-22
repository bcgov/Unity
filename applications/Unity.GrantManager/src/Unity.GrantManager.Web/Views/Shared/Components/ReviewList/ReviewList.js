$(document).ready(function () {
    console.log('Script loaded');
    const l = abp.localization.getResource('GrantManager');
    let inputAction = function (requestData, dataTableSettings) {
        const urlParams = new URL(window.location.toLocaleString()).searchParams;
        const applicationId = urlParams.get('ApplicationId');
        return applicationId;
    }

    let responseCallback = function (result) {

        // your custom code.
        console.log(result)

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
            text: function (dt, jqNode, config) {
                let buttonIcon = `<i class="fl ${config.buttonIcon}"></i>`;
                let buttonText = l(`Enum:AssessmentAction.${config.name}`);
                return buttonIcon + '<span>' + buttonText + '</span>'
            },
            action: function (e, dt, node, config) {
                let selectedRow = dt.rows({ selected: true }).data()[0];
                if (typeof(selectedRow) === 'object') {
                    unity.grantManager.assessments.assessments.executeAssessmentAction(selectedRow.id, config.name, {})
                        .then(function (result) {
                            PubSub.publish('refresh_review_list', selectedRow.id);
                            abp.notify.success(
                                String(result),
                                l(`Enum:AssessmentAction.${config.name}`)
                            );
                        });
                }
            }
        }
    });

    let actionArray = ['Create'];
    let additionalActions;

    // NOTE: FIND A BETTER WAY OF DOING THIS USING PROMISES
    unity.grantManager.assessments.assessments.getAllActions({
        async: false,
        success: function (data) {
            additionalActions = data;
        }
    });

    actionArray.push(...additionalActions);

    let renderButtons = function (actionValue) {
        return {
            extend: 'unityWorkflow',
            name: actionValue,
            // TODO: Get configured icons
            buttonIcon: 'fl-endpoint'
        };
    }
    
    let buttonArray = Array.from(actionArray, (item) => renderButtons(item));
    
    let assessmentButtonsGroup = {
        name: 'assessmentActionButtons',
        buttons: buttonArray
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
                unity.grantManager.assessments.assessments.getList, inputAction, responseCallback
            ),
            buttons: assessmentButtonsGroup,
            columnDefs: [
                {
                    title: '',
                    data: 'id',
                    visible: false,
                },
                {
                    title: l('ReviewerList:ReviewerName'),
                    data: 'assignedUserId',
                    className: 'data-table-header',
                    render: function (data) {
                        if (abp.currentUser.id === data) {
                            return 'Patrick Lavoie';
                        }
                        return data;
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
                        return data === null ? '' : (data === true ? 'Recommended for Approval' : 'Recommended for Denial');
                    },
                }
            ],
        })
    );

    reviewListTable.on('buttons-action', function (e, buttonApi, dataTable, node, config) {
        console.log('Button ' + buttonApi.name + ' was activated');
    });

    reviewListTable.buttons(0, null).container().appendTo("#DetailsActionBarStart");
    $("#DetailsActionBarStart .dt-buttons").contents().unwrap();

    let refreshActionButtons = function (dataTableContext, assessmentId) {
        dataTableContext.buttons().disable();

        if (assessmentId) 
        {
            unity.grantManager.assessments.assessments.getAvailableActions(assessmentId, {})
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
            console.log('Selected Data:', selectedData);
            PubSub.publish('select_application_review', selectedData);
            e.currentTarget.classList.toggle('selected');
            refreshActionButtons(dt, selectedData.id);
        }
    });

    reviewListTable.on('deselect', function (e, dt, type, indexes) {
        if (type === 'row') {
            let deselectedData = reviewListTable.row(indexes).data();
            PubSub.publish('select_application_review', null);
            e.currentTarget.classList.toggle('selected');
            refreshActionButtons(dt, null);
        }
    });

    const refresh_review_list_subscription = PubSub.subscribe(
        'refresh_review_list',
         (msg, data) => {
             reviewListTable.ajax.reload(function (json) {
                 if (data) {
                     let indexes = reviewListTable.rows().eq(0).filter(function (rowIdx) {
                         return reviewListTable.cell(rowIdx, 0).data() === data ? true : false;
                     });

                     reviewListTable.row(indexes).select();
                 }
             });
        
        }
    );
});
