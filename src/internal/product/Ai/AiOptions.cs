namespace SailNora.Product.Ai;

public class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>Placeholder | OpenAI</summary>
    public string Provider { get; set; } = AiProviders.Placeholder;

    public string Model { get; set; } = "gpt-4o-mini";

    public string? ApiKey { get; set; }

    public string? Endpoint { get; set; }

    public string AgentName { get; set; } = "sail-nora助手";

    public string? Instructions { get; set; }

    /// <summary>hashing | openai（openai 需 ApiKey；失败或未配置时回退 hashing）</summary>
    public string EmbeddingProvider { get; set; } = "hashing";

    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    public int EmbeddingDimensions { get; set; } = 64;

    /// <summary>是否启用检索后重排。</summary>
    public bool RerankEnabled { get; set; } = true;

    /// <summary>lexical（默认）| http（外部 Rerank API；失败回退 lexical）</summary>
    public string RerankProvider { get; set; } = "lexical";

    /// <summary>外部 Rerank HTTP 端点（Cohere 兼容 POST）。</summary>
    public string? RerankEndpoint { get; set; }

    public string? RerankApiKey { get; set; }

    public string RerankModel { get; set; } = "rerank-multilingual-v3.0";

    /// <summary>
    /// Agent 工具模式：native（MAF/MEAI function-calling）| orchestrated（固定编排 tool 事件）。
    /// </summary>
    public string AgentToolMode { get; set; } = "native";

    /// <summary>Agent 工具调用前是否走 HITL 审批事件。</summary>
    public bool HitlEnabled { get; set; }

    /// <summary>HITL 等待秒数；0 表示发出审批事件后立即 auto-approve（便于冒烟）。</summary>
    public int HitlTimeoutSeconds { get; set; }

    /// <summary>json（EmbeddingJson 余弦，默认）| pgvector（未接线时回退 json）</summary>
    public string VectorStore { get; set; } = "json";

    /// <summary>heuristic（默认）| llm（OpenAI 蒸馏；Placeholder 时走轻量规则蒸馏）</summary>
    public string MemoryExtractMode { get; set; } = "heuristic";
}

public static class MemoryExtractModes
{
    public const string Heuristic = "heuristic";
    public const string Llm = "llm";
}

public static class AgentToolModes
{
    public const string Native = "native";
    public const string Orchestrated = "orchestrated";
}
