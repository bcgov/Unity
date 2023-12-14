$(function () {
    unity.grantManager.grantApplications.grantApplication.getEconomicRegionCount().then(economicRegion => {

        // setup 
        const data = {
            labels: economicRegion.map(obj => obj.economicRegion),
            datasets: [{
                label: 'Submission Breakdown By Economic Region',
                data: economicRegion.map(obj => obj.count),
                hoverOffset: 4
            }]
        };

        const sum = economicRegion.map(obj => obj.count).reduce((partialSum, a) => partialSum + a, 0);

        const centerText = {
            id: 'centerText',
            beforeDatasetsDraw(chart, args, pluginOptions) {
                const { ctx, data } = chart;
                const text = 'Total Submissions: ';
                ctx.save();
                const x = chart.getDatasetMeta(0).data[0].x;
                const y = chart.getDatasetMeta(0).data[0].y;
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.font = '20px sans-serif';
                ctx.fillText(text, x, y - 10);
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.font = '20px sans-serif';
                ctx.fillText(sum, x, y + 15);
            }
        }

        // config 
        const config = {
            type: 'doughnut',
            data: data,
            options: {
                plugins: {
                    title: {
                        display: true,
                        text: 'SUBMISSION BREAKDOWN BY ECONOMIC REGION'
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                let currentValue = context.raw;
                                let total = context.chart._metasets[context.datasetIndex].total;
                                let percentage = parseFloat((currentValue / total * 100).toFixed(1));
                                return "Number of Submissions : " + currentValue + ' (' + percentage + '%)';
                            }
                        }
                    }
                }
            },
            plugins: [centerText]
        };

        // render init block
        const myChart = new Chart(
            document.getElementById('economicRegionChart'),
            config
        );
    });

    unity.grantManager.grantApplications.grantApplication.getSectorCount().then(sector => {

        // setup 
        const data = {
            labels: sector.map(obj => obj.sector),
            datasets: [{
                label: 'Submission Breakdown By Sector',
                data: sector.map(obj => obj.count),
                hoverOffset: 4
            }]
        };

        const sum = sector.map(obj => obj.count).reduce((partialSum, a) => partialSum + a, 0);

        const centerText = {
            id: 'centerText',
            beforeDatasetsDraw(chart, args, pluginOptions) {
                const { ctx, data } = chart;
                const text = 'Total Submissions: ';
                ctx.save();
                const x = chart.getDatasetMeta(0).data[0].x;
                const y = chart.getDatasetMeta(0).data[0].y;
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.font = '20px sans-serif';
                ctx.fillText(text, x, y - 10);
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.font = '20px sans-serif';
                ctx.fillText(sum, x, y+15);
            }
        }

        // config 
        const config = {
            type: 'doughnut',
            data: data,
            options: {
                plugins: {
                    title: {
                        display: true,
                        text: 'SUBMISSION BREAKDOWN BY SECTOR'
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                let currentValue = context.raw;
                                let total = context.chart._metasets[context.datasetIndex].total;
                                let percentage = parseFloat((currentValue / total * 100).toFixed(1));
                                return "Number of Submissions : " + currentValue + ' (' + percentage + '%)';
                            }
                        }
                    }
                }
            },
            plugins: [centerText]
        };

        // render init block
        const myChart = new Chart(
            document.getElementById('sectorChart'),
            config
        );
    });


    unity.grantManager.grantApplications.grantApplication.getApplicationStatusCount().then(applicationStatus => {

        // setup 
        const data = {
            labels: applicationStatus.map(obj => obj.applicationStatus),
            datasets: [{
                label: 'Application Status Overview',
                data: applicationStatus.map(obj => obj.count),
                hoverOffset: 4
            }]
        };

        const sum = applicationStatus.map(obj => obj.count).reduce((partialSum, a) => partialSum + a, 0);

        const centerText = {
            id: 'centerText',
            beforeDatasetsDraw(chart, args, pluginOptions) {
                const { ctx, data } = chart;
                const text = 'Total Submissions: ';
                ctx.save();
                const x = chart.getDatasetMeta(0).data[0].x;
                const y = chart.getDatasetMeta(0).data[0].y;
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.font = '20px sans-serif';
                ctx.fillText(text, x, y - 10);
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.font = '20px sans-serif';
                ctx.fillText(sum, x, y + 15);
            }
        }

        // config 
        const config = {
            type: 'doughnut',
            data: data,
            options: {
                plugins: {
                    title: {
                        display: true,
                        text: 'APPLICATION STATUS OVERVIEW'
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                let currentValue = context.raw;
                                let total = context.chart._metasets[context.datasetIndex].total;
                                let percentage = parseFloat((currentValue / total * 100).toFixed(1));
                                return "Count : " + currentValue + ' (' + percentage + '%)';
                            }
                        }
                    }
                }
            },
            plugins: [centerText]
        };

        // render init block
        const myChart = new Chart(
            document.getElementById('applicationStatusChart'),
            config
        );
    });
});