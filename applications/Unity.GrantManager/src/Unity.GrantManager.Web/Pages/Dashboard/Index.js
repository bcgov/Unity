
function reloadDashboard() {
    const intakeIds = $('#dashboardIntakeId').val();
    const categories = $('#dashboardCategoryName').val();
    unity.grantManager.dashboard.dashboard.getEconomicRegionCount(intakeIds, categories).then(economicRegion => {
        initializeChart(economicRegion.map(obj => obj.economicRegion), economicRegion.map(obj => obj.count),
            'Submissions by Economic Region', 'Total Submissions', 'SUBMISSION BREAKDOWN BY ECONOMIC REGION',
            'Number of Submissions', 'economicRegionChart');
    });

    unity.grantManager.dashboard.dashboard.getSectorCount(intakeIds, categories).then(sector => {
        initializeChart(sector.map(obj => obj.sector), sector.map(obj => obj.count), 'Submissions by Sector',
            'Total Submissions', 'SUBMISSION BREAKDOWN BY SECTOR', "Number of Submissions", 'sectorChart');
    });


    unity.grantManager.dashboard.dashboard.getApplicationStatusCount(intakeIds, categories).then(applicationStatus => {
        initializeChart(applicationStatus.map(obj => obj.applicationStatus), applicationStatus.map(obj => obj.count),
            'Submissions by Status', 'Total Submissions', 'APPLICATION STATUS OVERVIEW', "Count", 'applicationStatusChart')
    });

    unity.grantManager.dashboard.dashboard.getApplicationTagsCount(intakeIds, categories).then(applicationTags => {
        initializeChart(applicationTags.map(obj => obj.applicationTag), applicationTags.map(obj => obj.count),
            'Application Tags Overview', 'Total Number of Tags', 'APPLICATION TAGS OVERVIEW', "Count", 'applicationTagsChart')
    });

    unity.grantManager.dashboard.dashboard.getRequestedAmountPerSubsector(intakeIds, categories).then(subSector => {
        initializeChart(subSector.map(obj => obj.subsector), subSector.map(obj => obj.totalRequestedAmount),
            'Total Funding Requested Per Sub-Sector', 'Total Funding Requested', 'Total Funding Requested Per Sub-Sector', "Total Funding Requested", 'subsectorRequestedAmountChart')
    });

}

let colorPalette;

fetch('./colorsPalette.json')
    .then(response => response.json())
    .then(data => {
        colorPalette = data.colors;
    });

reloadDashboard();

function initializeChart(labelsArray, dataArray, labelDesc, centerTextLabel, titleText, mouseOverText, chartId) {

    let myChart = echarts.init(document.getElementById(chartId), null, {
        width: 465,
        height: 280,
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
    reloadDashboard();
});
