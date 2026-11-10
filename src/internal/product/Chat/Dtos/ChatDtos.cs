using System.Text.Json.Serialization;

namespace SailNora.Product.Chat.Dtos;

public sealed class CreateSessionInput
{
    public string? Title { get; set; }
}

public sealed class SessionDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}

public sealed class MessageDto
{
    public string Id { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
}

public sealed class KnowledgeSearchInput
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("knowledge_base_ids")]
    public List<string>? KnowledgeBaseIds { get; set; }

    [JsonPropertyName("knowledge_ids")]
    public List<string>? KnowledgeIds { get; set; }

    [JsonPropertyName("match_count")]
    public int? MatchCount { get; set; }
}

public sealed class KnowledgeChatInput
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("knowledge_base_ids")]
    public List<string>? KnowledgeBaseIds { get; set; }

    [JsonPropertyName("knowledge_ids")]
    public List<string>? KnowledgeIds { get; set; }
}

public sealed class StreamResponseDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ResponseType { get; set; } = "answer";
    public object? Data { get; set; }
    public bool Done { get; set; }
}
