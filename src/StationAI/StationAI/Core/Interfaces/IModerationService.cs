namespace StationAI.Core.Interfaces
{
    public interface IModerationService
    {
        Task<bool> IsRejectedByModerationAsync(string directive);
    }
}
