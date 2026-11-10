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

    /// <summary>是否启用检索后重排（lexical）。</summary>
    public bool RerankEnabled { get; set; } = true;

    /// <summary>
    /// Agent 工具模式：native（MAF/MEAI function-calling）| orchestrated（固定编排 tool 事件）。
    /// </summary>
    public string AgentToolMode { get; set; } = "native";

    /// <summary>Agent 工具调用前是否走 HITL 审批事件。</summary>
    public bool HitlEnabled { get; set; }

    /// <summary>HITL 等待秒数；0 表示发出审批事件后立即 auto-approve（便于冒烟）。</summary>
    public int HitlTimeoutSeconds { get; set; }
}

public static class AgentToolModes
{
    public const string Native = "native";
    public const string Orchestrated = "orchestrated";
}
