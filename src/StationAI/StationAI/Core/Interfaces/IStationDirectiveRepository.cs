namespace StationAI.Core.Interfaces
{
    public interface IStationDirectiveRepository
    {
        public Task<string?> GetRules();
        public Task SaveRules(string rules);
    }
}
