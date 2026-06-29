using System;
using System.Diagnostics; // Required for Stopwatch
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
using libplctag;
using libplctag.DataTypes;

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

                // Restore original 200ms delay to match PLC speed
                await Task.Delay(200, stoppingToken);
            }
        }

        // private async Task RunVirtualPhysicsEngineAsync()
        // {
        //     _simulationTime += 0.1;

        //     for (int i = 0; i < _plcConfigs.Count; i++)
        //     {
        //         var plc = _plcConfigs[i];
        //         var stopwatch = Stopwatch.StartNew();
                
        //         // Add a slight phase shift 'i' so all 3 PLCs don't have identical graphs
        //         double phaseOffset = i * 0.5;

        //         // Physics Engine (Sine/Cosine simulation)
        //         double temp = 30 + 10 * Math.Sin(_simulationTime + phaseOffset); 
        //         double pres = 1000 + 50 * Math.Cos((_simulationTime + phaseOffset) * 0.5); 
        //         double vib = Math.Abs(Math.Sin((_simulationTime + phaseOffset) * 2)) * 5.0; 

        //         stopwatch.Stop();
        //         // Add a mock baseline latency (10-25ms) so the demo dashboard looks realistic
        //         double latency = stopwatch.Elapsed.TotalMilliseconds + new Random().Next(10, 25); 

        //         await WriteToInfluxAsync(plc.Name, temp, pres, vib, latency);
        //         _logger.LogInformation($"[VIRTUAL] {plc.Name}: T={temp:0.0} | P={pres:0.0} | V={vib:0.00} | Latency={latency:0.00}ms");
        //     }
        // }

        // private async Task PollRealPlcsAsync()
        // {
        //     foreach (var plc in _activeConnections)
        //     {
        //         var stopwatch = Stopwatch.StartNew();
        //         try
        //         {
        //             // 1. Connect if disconnected
        //             if (!plc.Client.IsConnected)
        //             {
        //                 plc.Client.Connect(new IPEndPoint(IPAddress.Parse(plc.IpAddress), plc.Port), ModbusEndianness.BigEndian);
        //             }

        //             // 2. Read Registers
        //             var memory = await plc.Client.ReadHoldingRegistersAsync<short>(unitIdentifier: 1, startingAddress: 0, count: 4);
        //             short[] data = memory.ToArray(); 

        //             // 3. Extract Values
        //             double temp = data[0] / 10.0;
        //             double pres = data[1];
        //             double vib  = data[2] / 100.0;

        //             // Stop the watch to capture processing time
        //             stopwatch.Stop();
        //             double latency = stopwatch.Elapsed.TotalMilliseconds;

        //             // 4. Send to InfluxDB
        //             await WriteToInfluxAsync(plc.Name, temp, pres, vib, latency);
        //             _logger.LogInformation($"[REAL] {plc.Name}: T={temp:0.0} | P={pres} | V={vib:0.00} | Latency={latency:0.00}ms");
        //         }
        //         catch (Exception ex)
        //         {
        //             _logger.LogWarning($"⚠️ {plc.Name} ({plc.IpAddress}) Connection Error: {ex.Message}. Retrying...");
        //             plc.Client.Disconnect(); 
        //         }
        //     }
        // }

        private async Task RunVirtualPhysicsEngineAsync()
{
    _simulationTime += 0.1;

    for (int i = 0; i < _plcConfigs.Count; i++)
    {
        var plc = _plcConfigs[i];
        var stopwatch = Stopwatch.StartNew();
        
        // Add a slight phase shift so all PLCs don't have identical graphs
        double phaseOffset = i * 0.5;

        // Physics Engine: Simulate Tank Levels (0-100%)
        double tank1Level = 50 + 40 * Math.Sin(_simulationTime + phaseOffset);
        double tank2Level = 50 + 40 * Math.Sin((_simulationTime + phaseOffset) * 0.8);
        double tank3Level = 50 + 40 * Math.Sin((_simulationTime + phaseOffset) * 1.2);

        // Physics Engine: Simulate Pump Temperatures (SUPARCO noted 25 to -164 range)
        double hsPump1Temp = -70 + 90 * Math.Cos(_simulationTime + phaseOffset);
        double hsPump2Temp = -70 + 90 * Math.Cos((_simulationTime + phaseOffset) * 1.1);

        stopwatch.Stop();
        
        // Mock baseline latency (10-25ms)
        double latency = stopwatch.Elapsed.TotalMilliseconds + new Random().Next(10, 25);

        // Build the batched point using the exact SUPARCO schema
        var point = PointData.Measurement("PLC_LOX")
                             .Tag("sensor_id", plc.Name)
                             .Field("Program:MainProgram.Tank1_Level", tank1Level)
                             .Field("Program:MainProgram.Tank2_Level2", tank2Level) // Keeping the typo they noted
                             .Field("Program:MainProgram.Tank3_Level", tank3Level)
                             .Field("Program:MainProgram.HS_Pump1_Temperature", hsPump1Temp)
                             .Field("Program:MainProgram.HS_Pump2_Temperature", hsPump2Temp)
                             .Field("latency_ms", latency)
                             .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

        // Write to InfluxDB
        await _writeApi.WritePointAsync(point, _bucket, _org);

        _logger.LogInformation($"[VIRTUAL] {plc.Name}: T1={tank1Level:0.0} | T2={tank2Level:0.0} | Pump1Temp={hsPump1Temp:0.0} | Latency={latency:0.00}ms");
    }
}

        private async Task PollRealPlcsAsync()
{
    foreach (var plc in _plcConfigs)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Define all tags from the SUPARCO Python script
            var tagsToRead = new Dictionary<string, string>
            {
                // Tank Levels
                { "Tank1_Level", "Program:MainProgram.Tank1_Level" },
                { "Tank2_Level", "Program:MainProgram.Tank2_Level2" }, // keeping the typo as requested
                { "Tank3_Level", "Program:MainProgram.Tank3_Level" },
                
                // Pump Temperatures
                { "HS_Pump1_Temperature", "Program:MainProgram.HS_Pump1_Temperature" },
                { "HS_Pump2_Temperature", "Program:MainProgram.HS_Pump2_Temperature" },
                { "MS_Pump1_Temperature", "Program:MainProgram.MS_Pump1_Temperature" },
                { "MS_Pump2_Temperature", "Program:MainProgram.MS_Pump2_Temperature" },
                { "LS_Pump1_Temperature", "Program:MainProgram.LS_Pump1_Temperature" },
                { "LS_Pump2_Temperature", "Program:MainProgram.LS_Pump2_Temperature" },

                // Tank LOX Temperatures
                { "Tank1_LOX_Temperature", "Program:MainProgram.Tank1_LOX_Temperature" },
                { "Tank2_LOX_Temperature", "Program:MainProgram.Tank2_LOX_Temperature" }
            };

            // Start an Influx Point under the "PLC_LOX" measurement 
            var point = PointData.Measurement("PLC_LOX")
                                 .Tag("sensor_id", plc.Name);

            // Read tags via libplctag (using REAL/Float mapper as an example for analog values)
            foreach (var kvp in tagsToRead)
            {
                var tag = new Tag<RealPlcMapper, float>()
                {
                    Name = kvp.Value,
                    Gateway = plc.IpAddress,
                    Path = "1,0", // Standard ControlLogix routing
                    PlcType = PlcType.ControlLogix,
                    Protocol = Protocol.ab_eip,
                    Timeout = TimeSpan.FromMilliseconds(1000)
                };

                tag.Initialize();
                tag.Read();
                
                // Add as a field to our Influx point
                point = point.Field(kvp.Value, tag.Value); 
            }

            // Capture latency
            stopwatch.Stop();
            double latency = stopwatch.Elapsed.TotalMilliseconds;
            point = point.Field("latency_ms", latency).Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            // Write batched point to InfluxDB
            await _writeApi.WritePointAsync(point, _bucket, _org);

            _logger.LogInformation($"[REAL] {plc.Name} Polled {tagsToRead.Count} tags | Latency={latency:0.00}ms");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"{plc.Name} ({plc.IpAddress}) Connection Error: {ex.Message}. Retrying...");
        }
    }
}
        private async Task WriteToInfluxAsync(string sensorName, double temp, double pres, double vib, double latency)
        {
            try
            {
                var point = PointData
                    .Measurement("telemetry")
                    .Tag("sensor_id", sensorName)
                    .Field("temperature", temp)
                    .Field("pressure", pres)
                    .Field("vibration", vib)
                    .Field("latency_ms", latency) // Restored latency field
                    .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

                await _writeApi.WritePointAsync(point, _bucket, _org);
            }
            catch (Exception ex)
            {
                // Prevents network blips from crashing the service
                _logger.LogError($"❌ Failed to write to InfluxDB for {sensorName}: {ex.Message}");
            }
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