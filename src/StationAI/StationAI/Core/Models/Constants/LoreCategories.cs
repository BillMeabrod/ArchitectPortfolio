namespace StationAI.Core.Models.Constants
{
    public static class LoreCategories
    {
        public const string Person = "person";
        public const string Faction = "faction";
        public const string Planet = "planet";
        public const string Conflict = "conflict";

        public static readonly string[] All = [Person, Faction, Planet, Conflict];

        public static bool IsValid(string category) =>
            All.Contains(category, StringComparer.OrdinalIgnoreCase);
    }
}
