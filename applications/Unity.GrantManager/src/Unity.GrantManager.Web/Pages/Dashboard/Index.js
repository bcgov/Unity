
function reloadDashboard() {
    const intakeIds = $('#dashboardIntakeId').val();
    const categories = $('#dashboardCategoryName').val();
    const statusCodes = $('#dashboardStatuses').val();
    const substatus = $('#dashboardSubStatus').val();
    const tags = $('#dashboardTags').val();
    const assignees = $('#dashboardAssignees').val();
    const dateFrom = $('#dateFrom').val();
    const dateTo = $('#dateTo').val();
    const params = {};
    if (intakeIds.length > 0) {
        params.intakeIds = intakeIds;
    }
    if (categories.length > 0) {
        params.categories = categories;
    }
    if (statusCodes.length > 0) {
        params.statusCodes = statusCodes;
    }
    if (substatus.length > 0) {
        params.substatus = substatus;
    }
    if (tags.length > 0) {
        params.tags = tags;
    }
    if (assignees.length > 0) {
        params.assignees = assignees;
    }
    if (dateFrom.length > 0) {
        params.dateFrom = dateFrom;
    }
    if (dateTo.length > 0) {
        params.dateTo = dateTo;
    }

    const chartConfigs = [
        {
            fetchFunction: unity.grantManager.dashboard.dashboard.getEconomicRegionCount,
            label: 'economicRegion',
            count: 'count',
            title: 'Submissions by Economic Region',
            chartId: 'economicRegionChart',
            chartOption: 'pie',
            width: 465,
            height: 300
        },
        {
            fetchFunction: unity.grantManager.dashboard.dashboard.getApplicationStatusCount,
            label: 'applicationStatus',
            count: 'count',
            title: 'Submissions by Status',
            chartId: 'applicationStatusChart',
            chartOption: 'pie',
            width: 465,
            height: 300
        },
        {
            fetchFunction: unity.grantManager.dashboard.dashboard.getApplicationTagsCount,
            label: 'applicationTag',
            count: 'count',
            title: 'Application Tags Overview',
            chartId: 'applicationTagsChart',
            chartOption: 'pie',
            width: 465,
            height: 300
        },
        {
            fetchFunction: unity.grantManager.dashboard.dashboard.getApplicationAssigneeCount,
            label: 'applicationAssignee',
            count: 'count',
            title: 'Application Assignee Overview',
            chartId: 'applicationAssigneeChart',
            chartOption: 'pie',
            width: 465,
            height: 300
        },
        {
            fetchFunction: unity.grantManager.dashboard.dashboard.getRequestedAmountPerSubsector,
            label: 'subsector',
            count: 'totalRequestedAmount',
            title: 'Total Funding Requested Per Sub-Sector',
            chartId: 'subsectorRequestedAmountChart',
            chartOption: 'pie',
            width: 465,
            height: 300
        },
        {
            fetchFunction: unity.grantManager.dashboard.dashboard.getRequestApprovedCount,
            label: 'description',
            count: 'amount',
            title: 'Requested Vs. Approved Funding',
            chartId: 'requestVsApprovedChart',
            chartOption: 'bar',
            width: 465,
            height: 300
        }
    ];

    chartConfigs.forEach(config => {
        config.fetchFunction(params).then(data => {
            initializeChart(
                config,
                data.map(obj => obj[config.label]),
                data.map(obj => obj[config.count])
            );
        });
    });
}

let colorPalette;

fetch('./colorsPalette.json')
    .then(response => response.json())
    .then(data => {
        colorPalette = data.colors;
    });

reloadDashboard();

function initializeChart(config, labelsArray, dataArray) {

    let myChart = echarts.init(document.getElementById(config.chartId), null, {
        width: config.width,
        height: config.height,
        renderer: 'svg',
        useDirtyRect: false,
    });

    let option;

    switch (config.chartOption) {
        case "bar":
            option = initializeBarChart(config, dataArray, labelsArray);
            break;
        case "pie":
            option = initializePieChart(config, dataArray, labelsArray);
            break;
    }

    if (option && typeof option === 'object') {
        myChart.setOption(option);
    }

    window.addEventListener('resize', myChart.resize);
}

function initializePieChart(config, dataArray, labelsArray) {
    let sum = dataArray?.reduce((partialSum, a) => partialSum + a, 0) ?? 0;
    if (config.chartId === 'subsectorRequestedAmountChart') {
        sum = formatCurrency(sum);
    }

    let data = [];
    dataArray.forEach((value, index) => data.push({
        'value': value, 'name': labelsArray[index]
    }));

    let formatter = '{a| {c}}\n {b| {b}}';
    if (config.chartId === 'subsectorRequestedAmountChart') {
        formatter = '{a| ${c} ({d}%)}\n {b| {b}}';
    }

    let rich = {
        a: {
            color: '#474543',
            fontWeight: 700,
            fontSize: 18,
            align: 'left',
            padding: 5,
        },
        b: {
            color: '#2D2D2D',
            fontWeight: 400,
            fontSize: 14,
            align: 'left',
        }
    };

    if (config.chartId === 'subsectorRequestedAmountChart') {
        rich = {
            a: {
                color: '#474543',
                fontWeight: 700,
                fontSize: 14,
                align: 'left',
            },
            b: {
                color: '#2D2D2D',
                fontWeight: 400,
                fontSize: 14,
                align: 'left',
            }
        };
    }

    let option = {
        textStyle: {
            fontFamily: 'BCSans'
        },
        responsive: true,
        title: {
            text: config.title,
            left: 'left',
            top: '0%',
        },
        graphic: [
            {
                type: 'text',
                left: 'center',
                bottom: '18%',
                cursor: "auto",
                style: {
                    text: sum,
                    color: '#474543',
                    fontWeight: 700,
                    fontSize: 32,
                    fontFamily: 'BCSans'
                }
            }
        ],
        series: [
            {
                type: 'pie',
                radius: ['65%', '71%'],
                center: ['50%', '90%'],
                padAngle: 3,
                itemStyle: {
                    borderRadius: 10
                },
                startAngle: 180,
                endAngle: 360,
                labelLine: {
                    length: 30,
                },
                label: {
                    formatter: formatter,
                    overflow: 'break',
                    rich: rich
                },
                data: data,
                colorBy: "data",
                color: colorPalette,
                silent: true,
                avoidLabelOverlap: true,
            }
        ],
    };
    return option;
}

function initializeBarChart(config, dataArray, labelsArray) {
    let x_axisLabel = {
        color: '#2D2D2D',
        fontWeight: 400,
        fontSize: 14
    }

    let y_axisLabel = {
        color: '#474543',
        fontWeight: 700,
        fontSize: 14,
        formatter: function (value) {
            return formatToCADCurrency(value);
        }
    }

    option = {
        textStyle: {
            fontFamily: 'BCSans'
        },
        title: {
            text: config.title,
            left: 'left',
            top: '0%',
        },
        tooltip: {
            trigger: 'axis',
            axisPointer: {
                type: 'shadow'
            },
            formatter: function (params) {
                let tooltipText = params[0].name + '<br/>';
                params.forEach(function (item) {
                    tooltipText += item.marker + item.seriesName + ': ' + formatToCADCurrency(item.value) + '<br/>';
                });
                return tooltipText;
            }
        },
        grid: {
            left: '3%',
            right: '4%',
            bottom: '3%',
            containLabel: true
        },
        xAxis: [
            {
                type: 'category',
                data: labelsArray,
                axisTick: {
                    alignWithLabel: true
                },
                axisLabel: x_axisLabel
            }
        ],
        yAxis: [
            {
                type: 'value',
                axisLabel: y_axisLabel
            }
        ],
        series: [
            {
                name: 'Amount',
                type: 'bar',
                barWidth: '50%',
                data: dataArray,
                itemStyle: {
                    color: ({ name }) => {
                        const colors = {
                            'Requested Amount': '#F8BA47',
                            'Approved Amount': '#0288D1',
                        };
                        return colors[name] || '#0288D1';
                    }
                }
            }
        ]
    };

    return option;
}

function formatToCADCurrency(amount) {
    return new Intl.NumberFormat('en-CA', {
        style: 'currency',
        currency: 'CAD',
        minimumFractionDigits: 0
    }).format(amount);
}

function formatCurrency(num) {
    const units = [
        { value: 1e9, suffix: 'B' },
        { value: 1e6, suffix: 'M' }
    ];

    for (const { value, suffix } of units) {
        if (num >= value) {
            return `$${(num / value).toFixed(1).replace(/\.0$/, '')}${suffix}`;
        }
    }

    return `$${num.toFixed(2)}`;
}

$('#dashboardIntakeId').change(function () {
    const selectedValue = $(this).val();
    let intakeList = JSON.parse($('#dashboardIntakeList').text());
    let childDropdown = $('#dashboardCategoryName');
    childDropdown.empty();
    const filteredIntakes = intakeList.filter(intake => selectedValue.includes(intake.intakeId));
    const categories = Array.from(new Set(filteredIntakes.flatMap(intake => intake.categories)));
    $.each(categories, function (index, item) {
        childDropdown.append($('<option>', {
            value: item,
            text: item,
            selected: 'selected'
        }));
    });
    highlightSelected('dashboardCategoryName', 'CATEGORIES');
    reloadDashboard();
});

function highlightSelected(dropdownId,title) {
    $('#' + dropdownId + ' option:selected').addClass('dt-button-active');
    $('#' + dropdownId + ' option:not(:selected)').removeClass('dt-button-active');
    $('#' + dropdownId).selectpicker('refresh');
    $('#' + dropdownId).closest('.bootstrap-select').find('.btn .filter-option-inner-inner').html(title);
    $('#' + dropdownId).closest('.bootstrap-select').find('.btn').removeClass('bs-placeholder');
}

function initDropdown(dropdownId, title) {
    $('#' + dropdownId).selectpicker();
    $('#' + dropdownId).closest('.bootstrap-select').find('.btn .filter-option-inner-inner').html(title);
    $('#' + dropdownId).closest('.bootstrap-select').find('.btn .filter-option').addClass('button-align-center');
}

$(function () {

    initDropdown('dashboardIntakeId', 'INTAKES');
    initDropdown('dashboardCategoryName', 'CATEGORIES');
    initDropdown('dashboardStatuses', 'STATUS');
    initDropdown('dashboardSubStatus', 'SUB-STATUS');
    initDropdown('dashboardTags', 'TAGS');
    initDropdown('dashboardAssignees', 'ASSIGNEES');

    highlightSelected('dashboardIntakeId', 'INTAKES');
    highlightSelected('dashboardCategoryName', 'CATEGORIES');
    highlightSelected('dashboardStatuses', 'STATUS');
    highlightSelected('dashboardSubStatus', 'SUB-STATUS');
    highlightSelected('dashboardTags', 'TAG(S)');
    highlightSelected('dashboardAssignees', 'ASSIGNEE(S)');
    
    $('#dashboardIntakeId').change(function () {
        highlightSelected('dashboardIntakeId', 'INTAKES');
    });
    $('#dashboardCategoryName').change(function () {
        highlightSelected('dashboardCategoryName', 'CATEGORIES');
    });
    $('#dashboardStatuses').change(function () {
        highlightSelected('dashboardStatuses', 'STATUS');
    });
    $('#dashboardSubStatus').change(function () {
        highlightSelected('dashboardSubStatus', 'SUB-STATUS');
    });
    $('#dashboardTags').change(function () {
        highlightSelected('dashboardTags', 'TAGS');
    });
    $('#dashboardAssignees').change(function () {
        highlightSelected('dashboardAssignees', 'ASSIGNEES');
    });
});
