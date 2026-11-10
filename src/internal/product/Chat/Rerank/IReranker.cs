using SailNora.Product.Chat.Service;

namespace SailNora.Product.Chat.Rerank;

public interface IReranker
{
    string Name { get; }

    IReadOnlyList<RetrievalHitDto> Rerank(string query, IReadOnlyList<RetrievalHitDto> hits, int topK);
}
