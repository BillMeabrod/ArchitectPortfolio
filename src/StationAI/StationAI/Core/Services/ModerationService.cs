using Station.Logging;
using StationAI.Core.Interfaces;
using System.Text.Json;

namespace StationAI.Core.Services
{
    public class ModerationService : IModerationService
    {
        private readonly ILargeLanguageModelService _llmService;
        private readonly IStationLogger<ModerationService> _log;

        public ModerationService(
            ILargeLanguageModelService llmService,
            IStationLogger<ModerationService> log)
        {
            _llmService = llmService;
            _log = log;
        }

        public async Task<bool> IsRejectedByModerationAsync(string directive)
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

                // Full moderation prompt logged to Azure Monitor only — not surfaced publicly
                _log.Info("Moderation prompt:\n{Prompt}", prompt);

                var response = await _llmService.SendPrompt(prompt, typeof(ModerationResponse));
                var result = JsonSerializer.Deserialize<ModerationResponse>(response,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result is null)
                {
                    _log.Warn("Directive moderation returned unparseable response; submitted content left in place.");
                    return false;
                }

                return result.Inappropriate;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Directive moderation check failed; submitted content left in place.");
                return false;
            }
        }

        private sealed class ModerationResponse
        {
            public bool Inappropriate { get; set; }
        }
    }
}