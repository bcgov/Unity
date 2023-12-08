$(function () {
    unity.grantManager.grantApplications.grantApplication.getEconomicRegionCount().then(economicRegion => {

        // setup 
        const data = {
            labels: economicRegion.map(obj => obj.economicRegion),
            datasets: [{
                label: 'SUBMISSION BREAKDOWN BY ECONOMIC REGION',
                data: economicRegion.map(obj => obj.count),
                borderWidth: 1
            }]
        };

        const sum = economicRegion.map(obj => obj.count).reduce((partialSum, a) => partialSum + a, 0);

        // innerBarText plugin block
        const innerBarText = {
            id: 'innerBarText',
            afterDatasetDraw(chart, args, pluginOption) {
                const { ctx, data, chartArea: { left }, scales: { x, y } } = chart;
                ctx.save();
                data.datasets[0].data.forEach((dataPoint, index) => {
                    const percent = (dataPoint / sum) * 100;
                    ctx.fillText(`${percent.toFixed(2)}%`, left + 10, y.getPixelForValue(index));
                });
            }
        }

        // config 
        const config = {
            type: 'bar',
            data,
            options: {
                indexAxis: 'y',
                scales: {
                    x: {
                        beginAtZero: true,
                        suggestedMin: 0,
                        ticks: {
                            precision:0
                        },
                        title: {
                            display: true,
                            text:'Number of Submissions'
                        }
                    },
                    y: {
                        title: {
                            display: true,
                            text: 'Economic  Regions'
                        }
                    }
                }
            },
            plugins: [innerBarText]
        };

        // render init block
        const myChart = new Chart(
            document.getElementById('economicRegionChart'),
            config
        );
    });
   

});