using StationAI.Core.Interfaces;

namespace StationAI.Core.Services
{
    public class StationDirectiveService : IStationDirectiveService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StationDirectiveService> _logger;

        public StationDirectiveService(IServiceScopeFactory scopeFactory, ILogger<StationDirectiveService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task ProcessDirectiveAsync(string directive)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var parsingService = scope.ServiceProvider.GetRequiredService<IDirectiveParsingService>();
            var targetRepository = scope.ServiceProvider.GetRequiredService<IDirectiveTargetRepository>();
            var moderationService = scope.ServiceProvider.GetRequiredService<IModerationService>();
            var stationDirectiveRepository = scope.ServiceProvider.GetRequiredService<IStationDirectiveRepository>();

            bool moderationRejected = await moderationService.IsRejectedByModerationAsync(directive);
            if (moderationRejected)
            {
                _logger.LogWarning("Directive moderation rejected submitted content; reverting to fallback.");

                try
                {
                    await stationDirectiveRepository.SaveRules(AriaIdentity.NoStationDirectiveFallback);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to revert directive to fallback after moderation rejection.");
                }

                try
                {
                    await targetRepository.SaveTargetsAsync([]);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clear directive targets after moderation rejection.");
                }
                return;
            }

            await ParseAndStoreTargetsAsync(directive, parsingService, targetRepository);
        }

        private async Task ParseAndStoreTargetsAsync(
            string directive,
            IDirectiveParsingService parsingService,
            IDirectiveTargetRepository targetRepository)
        {
            try
            {
                var targets = await parsingService.Parse(directive);
                await targetRepository.SaveTargetsAsync(targets);

                _logger.LogInformation(
                    "Parsed and stored {Count} directive target(s) from saved directive.", targets.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to parse or store directive targets; previously stored targets left in place.");
            }
        }
    }
}
