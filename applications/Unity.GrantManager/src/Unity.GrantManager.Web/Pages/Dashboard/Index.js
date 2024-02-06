$(function () {

    unity.grantManager.dashboard.dashboard.getEconomicRegionCount().then(economicRegion => {
        initializeChart(economicRegion.map(obj => obj.economicRegion), economicRegion.map(obj => obj.count), true,
            'Submission Breakdown By Economic Region', 'Total Submissions', 'SUBMISSION BREAKDOWN BY ECONOMIC REGION',
            'Number of Submissions', 'economicRegionChart');
    });

    unity.grantManager.dashboard.dashboard.getSectorCount().then(sector => {
        initializeChart(sector.map(obj => obj.sector), sector.map(obj => obj.count), true, 'Submission Breakdown By Sector',
            'Total Submissions', 'SUBMISSION BREAKDOWN BY SECTOR', "Number of Submissions", 'sectorChart');
    });


    unity.grantManager.dashboard.dashboard.getApplicationStatusCount().then(applicationStatus => {
        initializeChart(applicationStatus.map(obj => obj.applicationStatus), applicationStatus.map(obj => obj.count), true,
            'Application Status Overview', 'Total Submissions', 'APPLICATION STATUS OVERVIEW', "Count", 'applicationStatusChart')
    });

    unity.grantManager.dashboard.dashboard.getApplicationTagsCount().then(applicationTags => {
        initializeChart(applicationTags.map(obj => obj.applicationTag), applicationTags.map(obj => obj.count), false,
            'Application Tags Overview', 'Total Number of Tags', 'APPLICATION TAGS OVERVIEW', "Count", 'applicationTagsChart')
    });

    function initializeChart(labelsArray, dataArray, displaySumOfData, labelDesc, centerTextLabel, titleText, mouseOverText, chartId) {
        // setup 
        const data = {
            labels: labelsArray,
            datasets: [{
                label: labelDesc,
                data: dataArray,
                hoverOffset: 4
            }]
        };

        let sum = 0;
        if (displaySumOfData) {
            sum = dataArray.reduce((partialSum, a) => partialSum + a, 0);
        } else {
            sum = labelsArray.length;
        }

        const centerText = {
            id: 'centerText',
            beforeDatasetsDraw(chart, args, pluginOptions) {
                const { ctx } = chart;
                const text = centerTextLabel + ': ';
                ctx.save();
                const x = chart.getDatasetMeta(0).data[0].x;
                const y = chart.getDatasetMeta(0).data[0].y;
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.font = '16px sans-serif';
                ctx.fillText(text, x, y - 10);
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.font = '16px sans-serif';
                ctx.fillText(sum, x, y + 15);
            }
        }

        // config 
        const config = {
            type: 'doughnut',
            data: data,
            options: {
                maintainAspectRatio: false,
                responsive: true,
                plugins: {
                    title: {
                        display: true,
                        text: titleText
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                let currentValue = context.raw;
                                let total = context.chart._metasets[context.datasetIndex].total;
                                let percentage = parseFloat((currentValue / total * 100).toFixed(1));
                                return mouseOverText + " : " + currentValue + ' (' + percentage + '%)';
                            }
                        }
                    }
                }
            },
            plugins: [centerText]
        };

        // render init block
        new Chart(document.getElementById(chartId), config); //NOSONAR
    }
});