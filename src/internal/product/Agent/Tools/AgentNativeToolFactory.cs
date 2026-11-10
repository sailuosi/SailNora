using Microsoft.Extensions.AI;
using SailNora.Foundation.DI;
using SailNora.Product.Sandbox.Service;
using SailNora.Product.Sandbox.Tools;

namespace SailNora.Product.Agent.Tools;

/// <summary>为单次 Agent 请求构造带闭包上下文的 MAF/MEAI 工具。</summary>
public sealed class AgentNativeToolFactory : IScoped
{
    private readonly KnowledgeAgentTools _tools;
    private readonly SandboxAgentTools _sandboxTools;

    public AgentNativeToolFactory(KnowledgeAgentTools tools, SandboxAgentTools sandboxTools)
    {
        _tools = tools;
        _sandboxTools = sandboxTools;
    }

    public IList<AITool> Create(
        Guid spaceId,
        IReadOnlyList<string>? knowledgeBaseIds,
        IReadOnlyList<string>? knowledgeIds,
        string? sandboxConfigId = null)
    {
        var kbIds = knowledgeBaseIds;
        var kidIds = knowledgeIds;
        var sandboxId = sandboxConfigId;

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

        if (string.IsNullOrWhiteSpace(sandboxId))
            return [search, list, read];

        AIFunction shell = AIFunctionFactory.Create(
            async (string command, string? work_dir = null, int timeout_sec = HostSandboxRunner.DefaultTimeoutSec, int max_output_bytes = HostSandboxRunner.DefaultMaxOutputBytes, CancellationToken cancellationToken = default) =>
                await _sandboxTools.ShellExecAsync(
                    spaceId,
                    sandboxId,
                    command,
                    work_dir,
                    timeout_sec <= 0 ? HostSandboxRunner.DefaultTimeoutSec : timeout_sec,
                    max_output_bytes <= 0 ? HostSandboxRunner.DefaultMaxOutputBytes : max_output_bytes,
                    cancellationToken),
            name: "shell_exec",
            description: "Execute a shell command in the configured host sandbox (run_command). Requires sandbox_config_id.");

        return [search, list, read, shell];
    }
}
