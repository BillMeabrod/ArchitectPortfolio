namespace StationAI.Core.Interfaces
{
    public interface IRulesRepository
    {
        public Task<string> GetRules();
        public Task SaveRules(string rules);
    }
}
