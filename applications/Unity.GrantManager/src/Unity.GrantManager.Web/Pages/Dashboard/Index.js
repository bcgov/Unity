
function reloadDashboard() {
    const intakeIds = $('#dashboardIntakeId').val();
    const categories = $('#dashboardCategoryName').val();
    const statusCodes = $('#dashboardStatuses').val();
    const substatus = $('#dashboardSubStatus').val();
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
    unity.grantManager.dashboard.dashboard.getEconomicRegionCount(params.intakeIds, params.categories, params.statusCodes, params.substatus).then(economicRegion => {
        initializeChart(economicRegion.map(obj => obj.economicRegion), economicRegion.map(obj => obj.count),
            'Submissions by Economic Region', 'economicRegionChart', 465, 280);
    });

    unity.grantManager.dashboard.dashboard.getApplicationStatusCount(params.intakeIds, params.categories, params.statusCodes, params.substatus).then(applicationStatus => {
        initializeChart(applicationStatus.map(obj => obj.applicationStatus), applicationStatus.map(obj => obj.count),
            'Submissions by Status', 'applicationStatusChart', 465, 280)
    });

    unity.grantManager.dashboard.dashboard.getApplicationTagsCount(params.intakeIds, params.categories, params.statusCodes, params.substatus).then(applicationTags => {
        initializeChart(applicationTags.map(obj => obj.applicationTag), applicationTags.map(obj => obj.count),
            'Application Tags Overview', 'applicationTagsChart', 465, 280)
    });

    unity.grantManager.dashboard.dashboard.getRequestedAmountPerSubsector(params.intakeIds, params.categories, params.statusCodes, params.substatus).then(subSector => {
        initializeChart(subSector.map(obj => obj.subsector), subSector.map(obj => obj.totalRequestedAmount),
            'Total Funding Requested Per Sub-Sector', 'subsectorRequestedAmountChart', 698, 420)
    });

    unity.grantManager.dashboard.dashboard.getSectorCount(params.intakeIds, params.categories, params.statusCodes, params.substatus).then(sector => {
        initializeChart(sector.map(obj => obj.sector), sector.map(obj => obj.count), 'Submissions by Sector',
            'sectorChart', 698, 420);
    });
}

let colorPalette;

fetch('./colorsPalette.json')
    .then(response => response.json())
    .then(data => {
        colorPalette = data.colors;
    });

reloadDashboard();

function initializeChart(labelsArray, dataArray, labelDesc, chartId, width, height) {

    let myChart = echarts.init(document.getElementById(chartId), null, {
        width: width,
        height: height,
        renderer: 'svg',
        useDirtyRect: false,
    });

    let sum = 0;
    if (chartId === 'applicationTagsChart' || chartId === 'subsectorRequestedAmountChart') {
        sum = labelsArray.length;
    } else {
        sum = dataArray.reduce((partialSum, a) => partialSum + a, 0);
    }

    let data = [];
    dataArray.forEach((value, index) => data.push({
        'value': value, 'name': labelsArray[index]
    }));

    let formatter = '{a| {c}}\n {b| {b}}';
    if (chartId === 'subsectorRequestedAmountChart') {
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

    if (chartId === 'subsectorRequestedAmountChart') {
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
            text: labelDesc,
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

    if (option && typeof option === 'object') {
        myChart.setOption(option);
    }

    window.addEventListener('resize', myChart.resize);
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
    childDropdown.multiselect('rebuild');
    highlightSelected(); 
    reloadDashboard();
});

function highlightSelected() {
    $('.multiselect-container li.active a').addClass('dt-button-active');
    $('.multiselect-container li:not(.active) a').removeClass('dt-button-active');
}

$(function () {

    let intakeOptions = generateMultiSelectOptions('INTAKES');
    $('#dashboardIntakeId').multiselect(intakeOptions);

    let categoryOptions = generateMultiSelectOptions('CATEGORIES');
    $('#dashboardCategoryName').multiselect(categoryOptions);

    let statusOptions = generateMultiSelectOptions('STATUS');
    $('#dashboardStatuses').multiselect(statusOptions);

    let subStatusOptions = generateMultiSelectOptions('SUB-STATUS');
    $('#dashboardSubStatus').multiselect(subStatusOptions);

    $('#dashboardIntakeId, #dashboardCategoryName, #dashboardStatuses, #dashboardSubStatus').on('change', function () {
        highlightSelected();
    });

    function generateMultiSelectOptions(buttonLabel) {
        return {
            includeSelectAllOption: false,
            buttonClass: 'btn btn-group dt-buttons btn-secondary buttons-collection dropdown-toggle custom-table-btn flex-none',
            buttonWidth: '100%',
            templates: {
                button: `<button type="button" style="font-weight:700;font-size:18px;text-align:center;display:flex;justify-content:center;align-items:center;" data-toggle="dropdown"><span>${buttonLabel}</span></button>`,
                ul: '<ul class="multiselect-container dropdown-menu"></ul>', // Right-aligned dropdown menu

                li: '<li><a class="dt-button dropdown-item buttons-columnVisibility" href="#"><label></label></a></li>'
            }
        };
    }

    

    highlightSelected();
});
