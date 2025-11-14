using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Suparco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SampleDataController : ControllerBase
    {
        [Authorize(Roles = "Admin")]
        [HttpPost("configure-alerts")]
        public IActionResult ConfigureAlerts()
        {
            return Ok("✅ Only Admin can configure alerts.");
        }

        [Authorize(Roles = "Operator,Admin")]
        [HttpGet("view-dashboard")]
        public IActionResult ViewDashboard()
        {
            return Ok("📊 Both Admin and Operator can view dashboards.");
        }
    }
}
