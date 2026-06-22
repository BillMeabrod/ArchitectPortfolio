using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StationAI.Core;
using StationAI.Core.Interfaces;
using System.Text.Json;

namespace StationAI.Adapters.Inbound
{

    [ApiController]
    [Route("api/[controller]")]
    public class UniverseRulesController : ControllerBase
    {
        private readonly IRulesRepository _rulesRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UniverseRulesController> _logger;

        public UniverseRulesController(
            IRulesRepository rulesRepository,
            IServiceProvider serviceProvider,
            ILogger<UniverseRulesController> logger)
        {
            _rulesRepository = rulesRepository;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetRules()
        {
            var universeIntel = await _rulesRepository.GetRules();
            return Ok(new
            {
                coreDirective = AriaIdentity.CoreDirective,
                universeIntel = universeIntel ?? AriaIdentity.NoUniverseIntelFallback
            });
        }

        [HttpPut]
        [EnableRateLimiting("UniverseRulesSave")]
        public async Task<IActionResult> SaveRules([FromBody] string rules)
        {
            await _rulesRepository.SaveRules(rules);

            _ = Task.Run(async () =>
            {
                try
                {
                    await using var scope = _serviceProvider.CreateAsyncScope();
                    var llm = scope.ServiceProvider.GetRequiredService<ILargeLanguageModelService>();
                    var repo = scope.ServiceProvider.GetRequiredService<IRulesRepository>();

                    var prompt = $$"""
                        You are a content moderation system for a public portfolio project.
                        Your only job is to determine whether the submitted text violates the following guidelines.

                        {{AriaIdentity.ContentModerationGuidelines}}

                        Respond only with a valid JSON object in this exact format, no explanation, no preamble, no markdown:
                        { "inappropriate": true } or { "inappropriate": false }

                        Text to evaluate:
                        {{rules}}
                        """;

                    var response = await llm.SendPrompt(prompt, typeof(ModerationResponse));
                    var result = JsonSerializer.Deserialize<ModerationResponse>(response,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result is null)
                    {
                        _logger.LogWarning("Universe rules moderation check returned an unparseable response; submitted content left in place.");
                        return;
                    }

                    if (result.Inappropriate)
                        await repo.SaveRules(AriaIdentity.NoUniverseIntelFallback);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Universe rules moderation check failed; submitted content left in place.");
                }
            });

            return Ok();
        }

        private sealed class ModerationResponse
        {
            public bool Inappropriate { get; set; }
        }
    }
}
