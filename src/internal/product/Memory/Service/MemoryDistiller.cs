using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SailNora.Foundation.DI;
using SailNora.Product.Ai;
using SailNora.Product.Chat.DBLayer;

namespace SailNora.Product.Memory.Service;

/// <summary>F6：将会话消息蒸馏为简短记忆条目。</summary>
[Scoped]
public sealed partial class MemoryDistiller
{
    private readonly AiOptions _options;
    private readonly ILogger<MemoryDistiller> _logger;

    public MemoryDistiller(IOptions<AiOptions> options, ILogger<MemoryDistiller> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<string> Facts, string ModeUsed)> DistillAsync(
        IReadOnlyList<KnMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var userTexts = messages
            .Where(m => string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase))
            .Select(m => Normalize(m.Content))
            .Where(t => t.Length >= 8)
            .Take(20)
            .ToList();

        if (userTexts.Count == 0)
            return ([], MemoryExtractModes.Heuristic);

        if (!string.Equals(_options.MemoryExtractMode, MemoryExtractModes.Llm, StringComparison.OrdinalIgnoreCase))
            return (userTexts.Select(Clip).ToList(), MemoryExtractModes.Heuristic);

        if (string.Equals(_options.Provider, AiProviders.OpenAI, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            try
            {
                var facts = await DistillWithOpenAiAsync(userTexts, cancellationToken);
                if (facts.Count > 0)
                    return (facts, MemoryExtractModes.Llm);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "memory llm distill failed; falling back to lite rules");
            }
        }

        // Placeholder / 失败：轻量规则蒸馏（区别于原文直存）
        return (LiteDistill(userTexts), MemoryExtractModes.Llm);
    }

    private async Task<List<string>> DistillWithOpenAiAsync(
        IReadOnlyList<string> userTexts,
        CancellationToken cancellationToken)
    {
        IChatClient client = SailNoraAiClient.CreateOpenAiChatClient(_options).AsIChatClient();
        var transcript = string.Join("\n", userTexts.Select((t, i) => $"{i + 1}. {t}"));
        var prompt =
            "从下列用户发言中提取不超过 5 条长期记忆（偏好、身份、约束）。" +
            "只输出 JSON 数组字符串，例如 [\"喜欢中文回答\",\"偏好简洁\"]。不要解释。\n\n" +
            transcript;

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, prompt)],
            cancellationToken: cancellationToken);
        var text = response.Text?.Trim() ?? "";
        return ParseFacts(text);
    }

    internal static List<string> LiteDistill(IReadOnlyList<string> userTexts)
    {
        var facts = new List<string>();
        foreach (var text in userTexts)
        {
            foreach (Match m in PreferenceRegex().Matches(text))
            {
                var fact = Clip(m.Value.Trim());
                if (fact.Length >= 4 && facts.All(f => !string.Equals(f, fact, StringComparison.Ordinal)))
                    facts.Add(fact);
            }

            if (facts.Count >= 5)
                break;
        }

        if (facts.Count == 0 && userTexts.Count > 0)
            facts.Add("用户提及：" + Clip(userTexts[0]));

        return facts.Take(5).ToList();
    }

    internal static List<string> ParseFacts(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var json = text.Trim();
        var start = json.IndexOf('[');
        var end = json.LastIndexOf(']');
        if (start >= 0 && end > start)
            json = json[start..(end + 1)];

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return [];
            return doc.RootElement.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => Clip(e.GetString() ?? ""))
                .Where(s => s.Length >= 4)
                .Distinct(StringComparer.Ordinal)
                .Take(5)
                .ToList();
        }
        catch
        {
            return text.Split(['\n', '；', ';'], StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Clip(s.Trim().TrimStart('-', '*', '•', ' ')))
                .Where(s => s.Length >= 4)
                .Take(5)
                .ToList();
        }
    }

    private static string Normalize(string raw) =>
        string.Join(' ', raw.Replace('\r', ' ').Replace('\n', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string Clip(string s) =>
        s.Length <= 240 ? s : s[..240].Trim();

    [GeneratedRegex(@"(?:我(?:喜欢|偏好|是|叫|需要|希望)|偏好|喜欢)[^。！？.!?]{2,40}", RegexOptions.Compiled)]
    private static partial Regex PreferenceRegex();
}
