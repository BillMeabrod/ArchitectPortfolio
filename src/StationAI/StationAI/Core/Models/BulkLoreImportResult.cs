namespace StationAI.Core.Models;

public record BulkLoreImportFailure(string Title, string Reason);

public class BulkLoreImportResult
{
    public int Succeeded { get; set; }
    public List<BulkLoreImportFailure> Failures { get; set; } = [];
}