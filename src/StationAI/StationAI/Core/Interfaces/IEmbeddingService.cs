namespace StationAI.Core.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text);
}