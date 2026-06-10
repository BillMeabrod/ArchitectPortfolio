using StationAI.Core.Interfaces;

namespace StationAI.Adapters.Outbound
{
    public class GeminiAdapter : ILargeLanguageModelService
    {
        public Task<string> SendPrompt(string prompt)
        {
            throw new NotImplementedException();
        }
    }
}
