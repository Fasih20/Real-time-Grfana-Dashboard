using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace Suparco.DataSimulator
{
    public class DataSimulatorService : BackgroundService
    {
        private readonly ILogger<DataSimulatorService> _logger;
        private readonly Random _random = new();
        private InfluxDBClient _influxClient;
        private WriteApiAsync _writeApi;

        // Your InfluxDB settings
        private const string influxUrl = "https://us-east-1-1.aws.cloud2.influxdata.com";
        private const string token = "uMoMNAN6HNeT0y1TEMDmqRIQ7MpBnIXwPK6WoUYgnRaHSzf6l8jIVAx6IWn8ZBI9zNLziEG_UWI1UpDWdZ5Zfg==";
        private const string org = "SHU";
        private const string bucket = "Data_Sim";

        public DataSimulatorService(ILogger<DataSimulatorService> logger)
        {
            _logger = logger;
            _influxClient = InfluxDBClientFactory.Create(influxUrl, token.ToCharArray());
            _writeApi = _influxClient.GetWriteApiAsync();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Data Simulator Service started at: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                for (int i = 1; i <= 3; i++)
                {
                    var temperature = 20 + _random.NextDouble() * 15; // 20–35°C
                    var pressure = 1000 + _random.NextDouble() * 50;  // 1000–1050 hPa
                    var vibration = _random.NextDouble() * 10;        // 0–10 m/s²

                    var point = PointData
                        .Measurement("telemetry")
                        .Tag("sensor_id", $"S{i}")
                        .Field("temperature", temperature)
                        .Field("pressure", pressure)
                        .Field("vibration", vibration)
                        .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

                    await _writeApi.WritePointAsync(point, bucket, org);

                    _logger.LogInformation("[{time}] Sensor {sensor}: T={temp:F2}°C, P={pres:F1}hPa, V={vib:F2}m/s²",
                        DateTime.Now, i, temperature, pressure, vibration);
                }

                await Task.Delay(5000, stoppingToken); // Send every 5 seconds
            }

            _logger.LogInformation("🛑 Data Simulator Service stopped.");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            _influxClient.Dispose();
        }
    }
}
