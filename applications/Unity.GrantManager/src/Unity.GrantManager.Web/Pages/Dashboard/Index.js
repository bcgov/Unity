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

        // innerBarText plugin block
        const innerBarText = {
            id: 'innerBarText',
            afterDatasetDraw(chart, args, pluginOption) {
                const { ctx, data, chartArea: { left }, scales: { x, y } } = chart;

                console.log('dataset:'+data.datasets);

                ctx.save();
                data.datasets[0].data.forEach((dataPoint, index) => {
                    ctx.font = 'bolder 12px sans-serif';
                    ctx.fillStyle = data.datasets[0].borderColor[index];
                    ctx.fillText(`${data.labels[index]}: ${dataPoint}`, left + 10, y.getPixelForValue(index))
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