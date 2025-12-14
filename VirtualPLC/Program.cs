using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NModbus;
using NModbus.Data; // <--- REQUIRED for SlaveDataStore

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🏭 Starting 3 Virtual PLCs...");

        // Start 3 simulators on different ports
        var plc1 = StartPlc(1, 5021, "PLC_01 (Oven)");
        var plc2 = StartPlc(1, 5022, "PLC_02 (Press)");
        var plc3 = StartPlc(1, 5023, "PLC_03 (Vibro)");

        await Task.WhenAll(plc1, plc2, plc3);
    }

    static async Task StartPlc(byte slaveId, int port, string name)
    {
        // 1. Start TCP Listener
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        
        // 2. Create Modbus Factory
        IModbusFactory factory = new ModbusFactory();
        IModbusSlaveNetwork network = factory.CreateSlaveNetwork(listener);
        
        // 3. FIX: Create Data Store directly (No Factory needed in NModbus v3)
        var dataStore = new SlaveDataStore(); 
        
        // 4. Add Slave to Network
        IModbusSlave slave = factory.CreateSlave(slaveId, dataStore);
        network.AddSlave(slave);

        // 5. Start Listening
        var networkTask = network.ListenAsync();
        Console.WriteLine($"✅ {name} listening on Port {port}");

        // 6. Simulation Loop (Physics Engine)
        double time = 0;
        while (true)
        {
            time += 0.1;

            // Generate values (Temperature, Pressure, Vibration)
            // Multiply by 10 or 100 to simulate "Integer-only" PLC registers
            ushort temp = (ushort)((30 + 10 * Math.Sin(time)) * 10); // 20.0 to 40.0
            ushort pres = (ushort)(1000 + 50 * Math.Cos(time * 0.5)); // 950 to 1050
            ushort vib = (ushort)(Math.Abs(Math.Sin(time * 2)) * 500); // 0.00 to 5.00

            // Write to Holding Registers (Address 1, 2, 3)

            // dataStore.HoldingRegisters[1] = temp;
            // dataStore.HoldingRegisters[2] = pres;
            // dataStore.HoldingRegisters[3] = vib;
            dataStore.HoldingRegisters.WritePoints(
                0,
                new ushort[] { temp, pres, vib }
            );
            // Log output every ~2 seconds just to show it's alive
            if (port == 5021 && ((int)(time * 10)) % 20 == 0)
                Console.WriteLine($"[Sim] {name} -> Temp: {temp / 10.0:0.0} | Pres: {pres} | Vib: {vib / 100.0:0.00}");

            await Task.Delay(200); // Update speed
        }
    }
}