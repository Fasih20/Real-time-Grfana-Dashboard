using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration; // <-- Add this
using libplctag;
using libplctag.DataTypes;
using System;
using System.Threading.Tasks; // <-- Add this for Task.Delay

namespace Suparco.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProcessController : ControllerBase
    {
        private readonly string _plcIp = "192.168.1.100"; 
        private readonly string _cpuPath = "1,0"; 
        private readonly string _commandTag = "Program:MainProgram.Test_Article_Filling_Command";
        
        private readonly bool _isDemoMode; // <-- Add this

        // Inject IConfiguration
        public ProcessController(IConfiguration config)
        {
            _isDemoMode = config.GetValue<bool>("AppConfig:IsDemoMode");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("StartProcess")]
        public async Task<IActionResult> StartProcess()
        {
            if (_isDemoMode)
            {
                await Task.Delay(500); // Simulate network latency
                return Ok(new { message = "[DEMO] Process Started Alpha: Success" });
            }

            try
            {
                var tag = new Tag<BoolPlcMapper, bool>()
                {
                    Name = _commandTag, Gateway = _plcIp, Path = _cpuPath,
                    PlcType = PlcType.ControlLogix, Protocol = Protocol.ab_eip
                };
                tag.Initialize();
                tag.Value = true; 
                tag.Write();
                return Ok(new { message = "Process Started Alpha: Success" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error starting process: {ex.Message}" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("StopProcess")]
        public async Task<IActionResult> StopProcess()
        {
            if (_isDemoMode)
            {
                await Task.Delay(500);
                return Ok(new { message = "[DEMO] Process Stopped Alpha: Success" });
            }

            try
            {
                var tag = new Tag<BoolPlcMapper, bool>()
                {
                    Name = _commandTag, Gateway = _plcIp, Path = _cpuPath,
                    PlcType = PlcType.ControlLogix, Protocol = Protocol.ab_eip
                };
                tag.Initialize();
                tag.Value = false; 
                tag.Write();
                return Ok(new { message = "Process Stopped Alpha: Success" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error stopping process: {ex.Message}" });
            }
        }

        [Authorize(Roles = "Admin,Operator")]
        [HttpGet("GetProcessStatus")]
        public IActionResult GetProcessStatus()
        {
            if (_isDemoMode)
            {
                // In demo mode, randomly toggle status for UI testing, or just return True
                return Ok(new { status = "Process Status is: True (Demo)" });
            }

            try
            {
                var tag = new Tag<BoolPlcMapper, bool>()
                {
                    Name = _commandTag, Gateway = _plcIp, Path = _cpuPath,
                    PlcType = PlcType.ControlLogix, Protocol = Protocol.ab_eip
                };
                tag.Initialize();
                tag.Read();
                return Ok(new { status = $"Process Status is: {tag.Value}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error reading status: {ex.Message}" });
            }
        }
    }
}