using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using InfluxDB.Client;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace Suparco.Api.Services
{
    public class AlertMonitorService : BackgroundService
    {
        private readonly ILogger<AlertMonitorService> _logger;
        private readonly IConfiguration _config;
        private readonly AlertConfigState _alertState;
        private DateTime _lastEmailSent = DateTime.MinValue;

        // public AlertMonitorService(ILogger<AlertMonitorService> logger, IConfiguration config)
        // {
        //     _logger = logger;
        //     _config = config;
        // }

        // Change the constructor to inject AlertConfigState instead of just IConfiguration
    public AlertMonitorService(ILogger<AlertMonitorService> logger, IConfiguration config, AlertConfigState alertState)
    {
        _logger = logger;
        _config = config;
        _alertState = alertState;
    }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Custom Alert Monitor Started.");

            string url = _config.GetValue<string>("AppConfig:InfluxUrl");
            string token = _config.GetValue<string>("AppConfig:InfluxToken");
            string org = _config.GetValue<string>("AppConfig:InfluxOrg");
            double threshold = _config.GetValue<double>("Alerting:TankLevelThreshold");

            using var client = new InfluxDBClient(url, token);
            var queryApi = client.GetQueryApi();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Flux query to get the latest Tank 1 Level from the last 15 seconds
                    var fluxQuery = $@"
                        from(bucket: ""Data_Sim"")
                          |> range(start: -15s)
                          |> filter(fn: (r) => r[""_measurement""] == ""PLC_LOX"")
                          |> filter(fn: (r) => r[""_field""] == ""Program:MainProgram.Tank1_Level"")
                          |> last()
                    ";

                    var tables = await queryApi.QueryAsync(fluxQuery, org);

                    foreach (var table in tables)
                    {
                        foreach (var record in table.Records)
                        {
                            double currentLevel = Convert.ToDouble(record.GetValue());

                            if (currentLevel > _alertState.TankLevelThreshold)
                            {
                                _logger.LogWarning($"CRITICAL: Tank 1 Level at {currentLevel:0.0}%. Exceeds threshold of {threshold}%.");
                                SendAlertEmail(currentLevel);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error querying InfluxDB for alerts: {ex.Message}");
                }

                // Wait 10 seconds before polling again
                await Task.Delay(10000, stoppingToken);
            }
        }

        private void SendAlertEmail(double level)
        {
            // Prevent spamming: Only send one email every 5 minutes
            if ((DateTime.Now - _lastEmailSent).TotalMinutes < 5) return;

            try
            {
                var smtpClient = new SmtpClient(_config["Alerting:SmtpServer"])
                {
                    Port = _config.GetValue<int>("Alerting:SmtpPort"),
                    Credentials = new NetworkCredential(_config["Alerting:SenderEmail"], _config["Alerting:SenderPassword"]),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_config["Alerting:SenderEmail"], "SUPARCO Console System"),
                    Subject = "🚨 CRITICAL ALERT: LOX Tank Level Exceeded",
                    Body = $"<h2>System Alert</h2><p>Tank 1 level has reached <strong>{level:0.0}%</strong>, exceeding the safe threshold of {_alertState.TankLevelThreshold}%.</p>",
                    IsBodyHtml = true,
                };

                // mailMessage.To.Add(_config["Alerting:RecipientEmail"]);
                foreach (var email in _alertState.RecipientEmails)
                {
                    mailMessage.To.Add(email);
                }
                smtpClient.Send(mailMessage);

                _lastEmailSent = DateTime.Now;
                _logger.LogInformation("Alert email dispatched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email: {ex.Message}");
            }
        }
    }
}