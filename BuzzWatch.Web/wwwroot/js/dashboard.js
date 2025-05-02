// Dashboard.js - Real-time chart updates with SignalR

document.addEventListener('DOMContentLoaded', function () {
    // Store chart instances by device ID
    const charts = {};
    const maxDataPoints = 20; // Limit number of points to prevent performance issues
    
    // Get the JWT token from session storage or another source
    const getToken = function() {
        return sessionStorage.getItem('jwtToken');
    };
    
    // Initialize charts for each device
    function initializeCharts() {
        document.querySelectorAll('[id^="chart-"]').forEach(canvas => {
            const deviceId = canvas.id.replace('chart-', '');
            
            charts[deviceId] = new Chart(canvas, {
                type: 'line',
                data: {
                    labels: [],
                    datasets: [{
                        label: 'Temperature (°C)',
                        borderColor: 'rgb(75, 192, 192)',
                        tension: 0.1,
                        data: []
                    }]
                },
                options: {
                    responsive: true,
                    animation: false,
                    scales: {
                        y: {
                            beginAtZero: false
                        }
                    }
                }
            });
        });
    }
    
    // Add a data point to the chart
    function addDataPoint(deviceId, value, timestamp) {
        if (!charts[deviceId]) return;
        
        const chart = charts[deviceId];
        const time = new Date(timestamp).toLocaleTimeString();
        
        // Limit data points
        if (chart.data.labels.length >= maxDataPoints) {
            chart.data.labels.shift();
            chart.data.datasets[0].data.shift();
        }
        
        chart.data.labels.push(time);
        chart.data.datasets[0].data.push(value);
        chart.update();
        
        // Update the latest temperature display
        const tempElement = document.getElementById(`latestTemp-${deviceId}`);
        if (tempElement) {
            tempElement.textContent = value.toFixed(1);
        }
    }
    
    // Initialize SignalR connection
    function startSignalRConnection() {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/measurements', {
                accessTokenFactory: () => getToken()
            })
            .withAutomaticReconnect()
            .build();
        
        // Handle measurement updates
        connection.on('MeasurementAdded', function(measurement) {
            console.log('Received measurement:', measurement);
            
            if (measurement.tempInsideC) {
                addDataPoint(
                    measurement.deviceId, 
                    measurement.tempInsideC,
                    measurement.timestamp
                );
            }
        });
        
        // Start the connection
        connection.start()
            .then(() => {
                console.log('SignalR connected!');
            })
            .catch(err => {
                console.error('SignalR connection error:', err);
                setTimeout(startSignalRConnection, 5000); // Retry after 5 seconds
            });
            
        // Handle disconnection
        connection.onclose(err => {
            console.warn('SignalR disconnected:', err);
            setTimeout(startSignalRConnection, 5000);
        });
    }
    
    // Initialize everything
    initializeCharts();
    startSignalRConnection();
}); 