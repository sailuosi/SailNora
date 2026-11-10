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
}
