using Microsoft.Extensions.AI;
using SailNora.Foundation.DI;

namespace SailNora.Product.Agent.Tools;

/// <summary>为单次 Agent 请求构造带闭包上下文的 MAF/MEAI 工具。</summary>
public sealed class AgentNativeToolFactory : IScoped
{
    private readonly KnowledgeAgentTools _tools;

    public AgentNativeToolFactory(KnowledgeAgentTools tools) => _tools = tools;

    public IList<AITool> Create(
        Guid spaceId,
        IReadOnlyList<string>? knowledgeBaseIds,
        IReadOnlyList<string>? knowledgeIds)
    {
        var kbIds = knowledgeBaseIds;
        var kidIds = knowledgeIds;

        AIFunction search = AIFunctionFactory.Create(
            async (string query, CancellationToken cancellationToken) =>
                await _tools.SearchKnowledgeAsync(spaceId, query, kbIds, kidIds, topK: 5, cancellationToken),
            name: "search_knowledge",
            description: "Search knowledge bases for relevant chunks.");

        AIFunction list = AIFunctionFactory.Create(
            (string? knowledge_base_id) =>
                _tools.ListDocuments(spaceId, knowledge_base_id ?? kbIds?.FirstOrDefault()),
            name: "list_documents",
            description: "List documents in a knowledge base.");

        AIFunction read = AIFunctionFactory.Create(
            (string knowledge_id, int max_chunks = 8) =>
                _tools.ReadDocument(spaceId, knowledge_id, max_chunks <= 0 ? 8 : max_chunks),
            name: "read_document",
            description: "Read chunk contents of a knowledge document.");

        return [search, list, read];
    }
}
