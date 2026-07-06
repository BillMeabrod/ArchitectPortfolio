using StationAI.Core.Models;

namespace StationAI.Core.Interfaces;

public interface IDirectiveTargetRepository
{
    Task<IReadOnlyList<DirectiveTarget>> GetTargetsAsync();

    Task SaveTargetsAsync(IReadOnlyList<DirectiveTarget> targets);
}