using System.Text.Json.Serialization;

namespace SailNora.Product.Agent.Dtos;

public sealed class CreateAgentInput
{
    public string Name { get; set; } = string.Empty;
    public string? Instructions { get; set; }
}

public sealed class AgentDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreateTime { get; set; }
}

public sealed class AgentChatInput
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("knowledge_base_ids")]
    public List<string>? KnowledgeBaseIds { get; set; }

    [JsonPropertyName("knowledge_ids")]
    public List<string>? KnowledgeIds { get; set; }

    [JsonPropertyName("agent_id")]
    public string? AgentId { get; set; }

    [JsonPropertyName("sandbox_config_id")]
    public string? SandboxConfigId { get; set; }
}
