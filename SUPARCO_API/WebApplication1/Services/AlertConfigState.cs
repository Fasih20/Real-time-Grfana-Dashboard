using System.Collections.Generic;

namespace Suparco.Api.Services
{
    public class AlertConfigState
    {
        // Default values that will be overwritten by appsettings on startup
        public double TankLevelThreshold { get; set; } = 85.0;
        public List<string> RecipientEmails { get; set; } = new List<string>();
    }
}