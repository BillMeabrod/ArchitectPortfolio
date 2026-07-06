using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StationAI.Core;
using StationAI.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace StationAI.Adapters.Inbound
{
    [ApiController]
    [Route("api/[controller]")]
    public class StationDirectiveController : ControllerBase
    {
        private readonly IStationDirectiveRepository _stationDirectiveRepository;
        private readonly ILargeLanguageModelService _llmService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StationDirectiveController> _logger;

        public StationDirectiveController(
            IStationDirectiveRepository stationDirectiveRepository,
            ILargeLanguageModelService lmService,
            IServiceProvider serviceProvider,
            ILogger<StationDirectiveController> logger)
        {
            _stationDirectiveRepository = stationDirectiveRepository;
            _llmService = lmService;
            _serviceProvider = serviceProvider;
            _logger = logger;
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

            _ = Task.Run(() => ProcessDirectiveAsync(request.Directive));

            return Ok();
        }

        private async Task ProcessDirectiveAsync(string directive)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();

            bool moderationRejected = await IsRejectedByModerationAsync(directive);
            if (moderationRejected)
                return;

            await ParseAndStoreTargetsAsync(scope.ServiceProvider, directive);
        }

        private async Task<bool> IsRejectedByModerationAsync(string directive)
        {
            try
            {
                var prompt = $$"""
                    You are a content moderation system for a public portfolio project.
                    Your only job is to determine whether the submitted text violates the following guidelines.

                    {{AriaIdentity.ContentModerationGuidelines}}

                    Respond only with a valid JSON object in this exact format, no explanation, no preamble, no markdown:
                    { "inappropriate": true } or { "inappropriate": false }

                    Text to evaluate:
                    {{directive}}
                    """;

                var response = await _llmService.SendPrompt(prompt, typeof(ModerationResponse));
                var result = JsonSerializer.Deserialize<ModerationResponse>(response,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result is null)
                {
                    _logger.LogWarning(
                        "Directive moderation check returned an unparseable response; submitted content left in place.");
                    return false;
                }

                if (result.Inappropriate)
                {
                    await _stationDirectiveRepository.SaveRules(AriaIdentity.NoStationDirectiveFallback);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Directive moderation check failed; submitted content left in place.");
                return false;
            }
        }

        private async Task ParseAndStoreTargetsAsync(IServiceProvider provider, string directive)
        {
            try
            {
                var parser = provider.GetRequiredService<IDirectiveParsingService>();
                var targetRepo = provider.GetRequiredService<IDirectiveTargetRepository>();

                var targets = await parser.Parse(directive);
                await targetRepo.SaveTargetsAsync(targets);

                _logger.LogInformation(
                    "Parsed and stored {Count} directive target(s) from saved directive.", targets.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to parse or store directive targets; previously stored targets left in place.");
            }
        }

        private sealed class ModerationResponse
        {
            public bool Inappropriate { get; set; }
        }
    }

    public record SaveDirectiveRequest(
        [Required][MaxLength(500)] string Directive
    );
}