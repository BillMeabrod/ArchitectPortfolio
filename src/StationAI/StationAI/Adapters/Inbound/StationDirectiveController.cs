using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Station.Logging;
using StationAI.Core;
using StationAI.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace StationAI.Adapters.Inbound
{
    [ApiController]
    [Route("api/[controller]")]
    public class StationDirectiveController : ControllerBase
    {
        private readonly IStationDirectiveService _stationDirectiveService;
        private readonly IStationDirectiveRepository _stationDirectiveRepository;
        private readonly IStationLogger<StationDirectiveController> _log;

        public StationDirectiveController(
            IStationDirectiveRepository stationDirectiveRepository,
            IStationDirectiveService stationDirectiveService,
            IStationLogger<StationDirectiveController> log)
        {
            _stationDirectiveRepository = stationDirectiveRepository;
            _stationDirectiveService = stationDirectiveService;
            _log = log;
        }

        [HttpGet]
        public async Task<IActionResult> GetDirective()
        {
            var directive = await _stationDirectiveRepository.GetRules();
            return Ok(new
            {
                coreDirective = AriaIdentity.CoreDirective,
                stationDirective = directive ?? AriaIdentity.NoStationDirectiveFallback
            });
        }

        [HttpPut]
        [EnableRateLimiting("StationDirectiveSave")]
        public async Task<IActionResult> SaveRules([FromBody] SaveDirectiveRequest request)
        {
            await _stationDirectiveRepository.SaveRules(request.Directive);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _stationDirectiveService.ProcessDirectiveAsync(request.Directive);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Background directive processing failed.");
                }
            });

            return Ok();
        }
    }

    public record SaveDirectiveRequest(
        [Required][MaxLength(500)] string Directive
    );
}