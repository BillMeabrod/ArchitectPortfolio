namespace StationAI.Core.Interfaces
{
    public interface IStationDirectiveService
    {
        public Task ProcessDirectiveAsync(string directive);
    }
}
