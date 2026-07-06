using StationAI.Core.Interfaces;
using StationAI.Core.Models;

namespace StationAI.Core.Services
{
    public class DirectiveParsingService : IDirectiveParsingService
    {
        private readonly ILargeLanguageModelService _llmService;
        private readonly ILogger<DirectiveParsingService> _logger;

        public DirectiveParsingService(ILargeLanguageModelService llmService, ILogger<DirectiveParsingService> logger)
        {
            _llmService = llmService;
            _logger = logger;
        }

        public Task<IReadOnlyList<DirectiveTarget>> Parse(string stationDirective)
        {
            throw new NotImplementedException();
        }
    }
}
