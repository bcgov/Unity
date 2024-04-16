$(function () {

    unity.grantManager.dashboard.dashboard.getEconomicRegionCount().then(economicRegion => {
        initializeChart(economicRegion.map(obj => obj.economicRegion), economicRegion.map(obj => obj.count),
            'Submissions by Economic Region', 'Total Submissions', 'SUBMISSION BREAKDOWN BY ECONOMIC REGION',
            'Number of Submissions', 'economicRegionChart');
    });

    unity.grantManager.dashboard.dashboard.getSectorCount().then(sector => {
        initializeChart(sector.map(obj => obj.sector), sector.map(obj => obj.count), 'Submissions by Sector',
            'Total Submissions', 'SUBMISSION BREAKDOWN BY SECTOR', "Number of Submissions", 'sectorChart');
    });


    unity.grantManager.dashboard.dashboard.getApplicationStatusCount().then(applicationStatus => {
        initializeChart(applicationStatus.map(obj => obj.applicationStatus), applicationStatus.map(obj => obj.count),
            'Submissions by Status', 'Total Submissions', 'APPLICATION STATUS OVERVIEW', "Count", 'applicationStatusChart')
    });

    unity.grantManager.dashboard.dashboard.getApplicationTagsCount().then(applicationTags => {
        initializeChart(applicationTags.map(obj => obj.applicationTag), applicationTags.map(obj => obj.count),
            'Application Tags Overview', 'Total Number of Tags', 'APPLICATION TAGS OVERVIEW', "Count", 'applicationTagsChart')
    });

    let colorPalette;

    fetch('./colorsPalette.json')
        .then(response => response.json())
        .then(data => {
            colorPalette = data.colors;
        });

    function initializeChart(labelsArray, dataArray, labelDesc, centerTextLabel, titleText, mouseOverText, chartId) {

        let myChart = echarts.init(document.getElementById(chartId), null, {
            width: 465,
            height: 250,
            renderer: 'svg',
            useDirtyRect: false,
        });

        let sum = 0;
        if (chartId === 'applicationTagsChart') {
            sum = labelsArray.length;
        } else {
            sum = dataArray.reduce((partialSum, a) => partialSum + a, 0);
        }

        let data = [];
        dataArray.forEach((value, index) => data.push({
            'value': value, 'name': labelsArray[index]
        }));

        let option = {
            textStyle: {
                fontFamily: 'BCSans'
            },
            responsive: true,
            title: {
                text: labelDesc,
                left: 'left',
                top: '16px',

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
                        formatter: '{a| {c}}\n {b| {b}}',
                        overflow: 'break',
                        rich: {
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
                        }
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
});