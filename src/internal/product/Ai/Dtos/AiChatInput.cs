namespace SailNora.Product.Ai.Dtos;

public class AiChatInput
{
    public string? Prompt { get; set; }

    public List<AiChatMessageInput>? Messages { get; set; }
}

public sealed class AiChatMessageInput
{
    public string Role { get; set; } = "user";

    public string Content { get; set; } = string.Empty;
}
