using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using FluentModbus; 

namespace Suparco.DataSimulator
{
    public class DataSimulatorService : BackgroundService
    {
        private readonly ILogger<DataSimulatorService> _logger;
        private readonly IConfiguration _config;
        private InfluxDBClient _influxClient;
        private WriteApiAsync _writeApi;

        // InfluxDB Settings
        private const string influxUrl = "https://us-east-1-1.aws.cloud2.influxdata.com";
        private const string token = "uMoMNAN6HNeT0y1TEMDmqRIQ7MpBnIXwPK6WoUYgnRaHSzf6l8jIVAx6IWn8ZBI9zNLziEG_UWI1UpDWdZ5Zfg==";
        private const string org = "SHU";
        private const string bucket = "Data_Sim";

        // List to hold our 3 PLC connections
        private List<PlcConnection> _plcs = new();

        public DataSimulatorService(ILogger<DataSimulatorService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;

            _influxClient = new InfluxDBClient(influxUrl, token);
            _writeApi = _influxClient.GetWriteApiAsync();

            // Initialize our 3 PLC Managers
            _plcs.Add(new PlcConnection("PLC_01", 5021));
            _plcs.Add(new PlcConnection("PLC_02", 5022));
            _plcs.Add(new PlcConnection("PLC_03", 5023));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Multi-PLC Modbus Service Started!");

            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var plc in _plcs)
                {
                    try
                    {
                        // 1. Connect if needed
                        if (!plc.Client.IsConnected)
                        {
                            plc.Client.Connect(new IPEndPoint(IPAddress.Loopback, plc.Port), ModbusEndianness.BigEndian);
                        }

                        // 2. Read Registers (Address 0 to match your simulator)
                        // Reading 4 registers just to be safe, we use 0, 1, 2
                        var memory = await plc.Client.ReadHoldingRegistersAsync<short>(unitIdentifier: 1, startingAddress: 0, count: 4);
                        
                        // Fix for Span error: Convert to Array immediately
                        short[] data = memory.ToArray(); 

                        // 3. Extract Values (Using simulated logic: 255 = 25.5 deg)
                        // Note: NModbus Simulator writes to registers 1, 2, 3. 
                        // FluentModbus reads 0-indexed. So Register 1 is at index 0.
                        double temp = data[0] / 10.0;
                        double pres = data[1];
                        double vib  = data[2] / 100.0;

                        // 4. Send to InfluxDB
                        var point = PointData
                            .Measurement("telemetry")
                            .Tag("sensor_id", plc.Name)
                            .Field("temperature", temp)
                            .Field("pressure", pres)
                            .Field("vibration", vib)
                            .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

                        await _writeApi.WritePointAsync(point, bucket, org);

                        _logger.LogInformation($"✅ {plc.Name}: T={temp:0.0} | P={pres} | V={vib:0.00}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"⚠️ {plc.Name} Connection Error: {ex.Message}. Reconnecting...");
                        plc.Client.Disconnect(); // Reset connection for next try
                    }
                }

                // Wait 1 second before next cycle
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach(var plc in _plcs) plc.Client.Disconnect();
            _influxClient.Dispose();
            await base.StopAsync(cancellationToken);
        }
    }

    // Helper class to manage multiple clients neatly
    public class PlcConnection
    {
        public string Name { get; }
        public int Port { get; }
        public ModbusTcpClient Client { get; }

        public PlcConnection(string name, int port)
        {
            Name = name;
            Port = port;
            Client = new ModbusTcpClient();
        }
    }
}