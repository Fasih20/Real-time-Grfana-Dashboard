using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Suparco.DataSimulator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    // Catch the custom --demo flag
                    bool isDemo = args.Contains("--demo") || args.Contains("-demo");
                    
                    // Override the appsettings.json value if the flag is present
                    if (isDemo)
                    {
                        var dict = new Dictionary<string, string>
                        {
                            {"AppConfig:IsDemoMode", "true"}
                        };
                        config.AddInMemoryCollection(dict);
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<DataSimulatorService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                });
    }
}