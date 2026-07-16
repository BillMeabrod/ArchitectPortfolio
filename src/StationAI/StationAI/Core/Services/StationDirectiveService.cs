using Station.Logging;
using StationAI.Core.Interfaces;

namespace StationAI.Core.Services
{
    public class StationDirectiveService : IStationDirectiveService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IStationLogger<StationDirectiveService> _log;

        public StationDirectiveService(
            IServiceScopeFactory scopeFactory,
            IStationLogger<StationDirectiveService> log)
        {
            _scopeFactory = scopeFactory;
            _log = log;
        }

        public async Task ProcessDirectiveAsync(string directive)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var parsingService = scope.ServiceProvider.GetRequiredService<IDirectiveParsingService>();
            var targetRepository = scope.ServiceProvider.GetRequiredService<IDirectiveTargetRepository>();
            var moderationService = scope.ServiceProvider.GetRequiredService<IModerationService>();
            var stationDirectiveRepository = scope.ServiceProvider.GetRequiredService<IStationDirectiveRepository>();

            _log.InfoPublic("Station directive received — running content moderation", null);

            bool moderationRejected = await moderationService.IsRejectedByModerationAsync(directive);
            if (moderationRejected)
            {
                _log.WarnPublic("Directive failed moderation — reverting to fallback", null);

                try
                {
                    await stationDirectiveRepository.SaveRules(AriaIdentity.NoStationDirectiveFallback);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Failed to revert directive to fallback after moderation rejection.");
                }

                try
                {
                    await targetRepository.SaveTargetsAsync([]);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Failed to clear directive targets after moderation rejection.");
                }

                return;
            }

            _log.InfoPublic("Directive passed moderation — parsing targets", null);

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

                _log.InfoPublic(
                    "Directive targets extracted — {Count} target(s): {Targets}",
                    null,
                    targets.Count,
                    string.Join(", ", targets.Select(t => $"{t.Target} ({t.Type})")));
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to parse or store directive targets; previously stored targets left in place.");
            }
        }
    }
}