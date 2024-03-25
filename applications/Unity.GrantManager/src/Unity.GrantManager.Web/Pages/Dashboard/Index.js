$(function () {

    unity.grantManager.dashboard.dashboard.getEconomicRegionCount().then(economicRegion => {
        initializeChart(economicRegion.map(obj => obj.economicRegion), economicRegion.map(obj => obj.count),
            'Submission Breakdown By Economic Region', 'Total Submissions', 'SUBMISSION BREAKDOWN BY ECONOMIC REGION',
            'Number of Submissions', 'economicRegionChart');
    });

    unity.grantManager.dashboard.dashboard.getSectorCount().then(sector => {
        initializeChart(sector.map(obj => obj.sector), sector.map(obj => obj.count), 'Submission Breakdown By Sector',
            'Total Submissions', 'SUBMISSION BREAKDOWN BY SECTOR', "Number of Submissions", 'sectorChart');
    });


    unity.grantManager.dashboard.dashboard.getApplicationStatusCount().then(applicationStatus => {
        initializeChart(applicationStatus.map(obj => obj.applicationStatus), applicationStatus.map(obj => obj.count),
            'Application Status Overview', 'Total Submissions', 'APPLICATION STATUS OVERVIEW', "Count", 'applicationStatusChart')
    });

    unity.grantManager.dashboard.dashboard.getApplicationTagsCount().then(applicationTags => {
        initializeChart(applicationTags.map(obj => obj.applicationTag), applicationTags.map(obj => obj.count),
            'Application Tags Overview', 'Total Number of Tags', 'APPLICATION TAGS OVERVIEW', "Count", 'applicationTagsChart')
    });

    function initializeChart(labelsArray, dataArray, labelDesc, centerTextLabel, titleText, mouseOverText, chartId) {

        var myChart = echarts.init(document.getElementById(chartId), null, {
            width: 450,
            height: 250
        });

        var sum = 0;
        if (chartId === 'applicationTagsChart') {
            sum = labelsArray.length;
        } else {
            sum = dataArray.reduce((partialSum, a) => partialSum + a, 0);
        }

        var data = [];
        dataArray.forEach((value, index) => data.push({ 'value': value, 'name': labelsArray[index] }));

        var option = {
            title: {
                text: labelDesc,
                left: 'center',
                fontFamily: "BCSans",
            },
            graphic: [
                {
                    type: 'text',
                    left: 'center',
                    bottom: '18%',
                    fontFamily: "BCSans",
                    style: {
                        text: sum,
                        fill: '#000',
                        fontWeight: 700,
                        fontSize: 32,
                    }
                }
            ],
            series: [
                {
                    type: 'pie',
                    radius: ['80%', '90%'],
                    center: ['50%', '90%'],
                    padAngle: 3,
                    startAngle: 180,
                    endAngle: 360,
                    labelLine: {
                        length: 30
                    },
                    label: {
                        formatter: '{a|{c}}\n {b}',
                        fontFamily: "BCSans",
                        rich: {
                            a: {
                                color: '#4C5058',
                                fontWeight: 700,
                                lineHeight: 30.61,
                                fontSize: 18,
                            },
                        }
                    },
                    data: data,
                }
            ],
        };

        if (option && typeof option === 'object') {
            myChart.setOption(option);
        }
    }
});
