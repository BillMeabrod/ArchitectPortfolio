using StationAI.Core.Interfaces;

namespace StationAI.Adapters.Outbound
{
    public class RulesBlobStorageAdapter : IRulesRepository
    {
        public Task<string> GetRules()
        {
            throw new NotImplementedException();
        }

        public Task SaveRules(string rules)
        {
            throw new NotImplementedException();
        }
    }
}
