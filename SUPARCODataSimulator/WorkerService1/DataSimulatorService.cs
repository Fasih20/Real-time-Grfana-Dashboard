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
        private readonly bool _isDemoMode;
        
        private InfluxDBClient _influxClient;
        private WriteApiAsync _writeApi;

        // Configuration bindings
        private readonly string _bucket;
        private readonly string _org;
        private List<PlcConfig> _plcConfigs = new();

        // Active Modbus Connections (Only used in Production mode)
        private List<PlcConnection> _activeConnections = new();

        // Physics Engine State (Only used in Demo mode)
        private double _simulationTime = 0;

        public DataSimulatorService(ILogger<DataSimulatorService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;

            // 1. Load Configurations
            _isDemoMode = _config.GetValue<bool>("AppConfig:IsDemoMode");
            
            string influxUrl = _config.GetValue<string>("AppConfig:InfluxUrl");
            string token = _config.GetValue<string>("AppConfig:InfluxToken");
            _org = _config.GetValue<string>("AppConfig:InfluxOrg");
            _bucket = _config.GetValue<string>("AppConfig:InfluxBucket");

            _config.GetSection("Plcs").Bind(_plcConfigs);

            // 2. Init InfluxDB
            _influxClient = new InfluxDBClient(influxUrl, token);
            _writeApi = _influxClient.GetWriteApiAsync();

            // 3. Init Modbus Clients (If not in demo mode)
            if (!_isDemoMode)
            {
                foreach (var plc in _plcConfigs)
                {
                    _activeConnections.Add(new PlcConnection(plc.Name, plc.IpAddress, plc.Port));
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_isDemoMode)
                _logger.LogWarning("⚠️ RUNNING IN DEMO MODE: Bypassing Network. Generating Virtual PLC Physics Data...");
            else
                _logger.LogInformation("🚀 RUNNING IN PRODUCTION MODE: Connecting to Real Modbus PLCs...");

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_isDemoMode)
                {
                    await RunVirtualPhysicsEngineAsync();
                }
                else
                {
                    await PollRealPlcsAsync();
                }

                // Poll / Generate data every 1 second
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task RunVirtualPhysicsEngineAsync()
        {
            _simulationTime += 0.1;

            for (int i = 0; i < _plcConfigs.Count; i++)
            {
                var plc = _plcConfigs[i];
                
                // Add a slight phase shift 'i' so all 3 PLCs don't have identical graphs
                double phaseOffset = i * 0.5;

                // Physics Engine (Sine/Cosine simulation)
                double temp = 30 + 10 * Math.Sin(_simulationTime + phaseOffset); 
                double pres = 1000 + 50 * Math.Cos((_simulationTime + phaseOffset) * 0.5); 
                double vib = Math.Abs(Math.Sin((_simulationTime + phaseOffset) * 2)) * 5.0; 

                await WriteToInfluxAsync(plc.Name, temp, pres, vib);
                _logger.LogInformation($"[VIRTUAL] {plc.Name}: T={temp:0.0} | P={pres:0.0} | V={vib:0.00}");
            }
        }

        private async Task PollRealPlcsAsync()
        {
            foreach (var plc in _activeConnections)
            {
                try
                {
                    // 1. Connect if disconnected
                    if (!plc.Client.IsConnected)
                    {
                        plc.Client.Connect(new IPEndPoint(IPAddress.Parse(plc.IpAddress), plc.Port), ModbusEndianness.BigEndian);
                    }

                    // 2. Read Registers
                    var memory = await plc.Client.ReadHoldingRegistersAsync<short>(unitIdentifier: 1, startingAddress: 0, count: 4);
                    short[] data = memory.ToArray(); 

                    // 3. Extract Values
                    double temp = data[0] / 10.0;
                    double pres = data[1];
                    double vib  = data[2] / 100.0;

                    // 4. Send to InfluxDB
                    await WriteToInfluxAsync(plc.Name, temp, pres, vib);
                    _logger.LogInformation($"[REAL] {plc.Name}: T={temp:0.0} | P={pres} | V={vib:0.00}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"⚠️ {plc.Name} ({plc.IpAddress}) Connection Error: {ex.Message}. Retrying...");
                    plc.Client.Disconnect(); 
                }
            }
        }

        private async Task WriteToInfluxAsync(string sensorName, double temp, double pres, double vib)
        {
            var point = PointData
                .Measurement("telemetry")
                .Tag("sensor_id", sensorName)
                .Field("temperature", temp)
                .Field("pressure", pres)
                .Field("vibration", vib)
                .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            await _writeApi.WritePointAsync(point, _bucket, _org);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach(var plc in _activeConnections) plc.Client.Disconnect();
            _influxClient.Dispose();
            await base.StopAsync(cancellationToken);
        }
    }

    // --- Helper Classes ---
    
    public class PlcConfig
    {
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public int Port { get; set; }
    }

    public class PlcConnection
    {
        public string Name { get; }
        public string IpAddress { get; }
        public int Port { get; }
        public ModbusTcpClient Client { get; }

        public PlcConnection(string name, string ipAddress, int port)
        {
            Name = name;
            IpAddress = ipAddress;
            Port = port;
            Client = new ModbusTcpClient();
        }
    }
}