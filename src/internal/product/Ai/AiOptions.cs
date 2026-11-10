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
}
