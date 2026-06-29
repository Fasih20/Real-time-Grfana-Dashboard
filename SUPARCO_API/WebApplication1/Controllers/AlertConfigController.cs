using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Suparco.Api.Services;
using System.Collections.Generic;

namespace Suparco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Strictly lock this to Admins
    public class AlertConfigController : ControllerBase
    {
        private readonly AlertConfigState _alertState;

        public AlertConfigController(AlertConfigState alertState)
        {
            _alertState = alertState;
        }

        [HttpGet]
        public IActionResult GetConfig()
        {
            return Ok(new 
            { 
                threshold = _alertState.TankLevelThreshold, 
                emails = _alertState.RecipientEmails 
            });
        }

        [HttpPost]
        public IActionResult UpdateConfig([FromBody] UpdateConfigRequest request)
        {
            if (request.Emails == null || request.Emails.Count == 0)
                return BadRequest(new { message = "At least one email is required." });

            _alertState.TankLevelThreshold = request.Threshold;
            _alertState.RecipientEmails = request.Emails;

            return Ok(new { message = "Alert configuration updated successfully." });
        }
    }

    public class UpdateConfigRequest
    {
        public double Threshold { get; set; }
        public List<string> Emails { get; set; }
    }
}