using StationAI.Core.Models;

namespace StationAI.Core.Interfaces
{
    public interface IDirectiveParsingService
    {
        Task<IReadOnlyList<DirectiveTarget>> Parse(string stationDirective);
    }
}
