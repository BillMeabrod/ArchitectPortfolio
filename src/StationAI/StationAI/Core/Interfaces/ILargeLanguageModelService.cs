namespace StationAI.Core.Interfaces
{
    public interface ILargeLanguageModelService
    {
        public Task<string> SendPrompt(string prompt, Type targetSchemaType);
    }
}
