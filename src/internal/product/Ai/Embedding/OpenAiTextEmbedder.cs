using System.ClientModel;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Embeddings;
using SailNora.Product.Ai;

namespace SailNora.Product.Ai.Embedding;

public sealed class OpenAiTextEmbedder : ITextEmbedder
{
    private readonly EmbeddingClient _client;
    private readonly ILogger<OpenAiTextEmbedder> _logger;
    private readonly int _expectedDimensions;

    public OpenAiTextEmbedder(AiOptions options, ILogger<OpenAiTextEmbedder> logger)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("Ai:ApiKey is required for OpenAI embedding.");

        _logger = logger;
        _expectedDimensions = options.EmbeddingDimensions;
        var model = string.IsNullOrWhiteSpace(options.EmbeddingModel)
            ? "text-embedding-3-small"
            : options.EmbeddingModel.Trim();

        var openAi = string.IsNullOrWhiteSpace(options.Endpoint)
            ? new OpenAIClient(options.ApiKey)
            : new OpenAIClient(
                new ApiKeyCredential(options.ApiKey),
                new OpenAIClientOptions { Endpoint = new Uri(options.Endpoint) });

        _client = openAi.GetEmbeddingClient(model);
        ProviderName = "openai:" + model;
    }

    public string ProviderName { get; }

    public int Dimensions => _expectedDimensions;

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var batch = await EmbedBatchAsync([text ?? string.Empty], cancellationToken);
        return batch[0];
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
            return [];

        try
        {
            var inputs = texts.Select(t => string.IsNullOrWhiteSpace(t) ? " " : t).ToList();
            var result = await _client.GenerateEmbeddingsAsync(inputs, cancellationToken: cancellationToken);
            var vectors = new List<float[]>(texts.Count);
            foreach (var item in result.Value)
            {
                var floats = item.ToFloats().ToArray();
                vectors.Add(HashingEmbeddingGenerator.Normalize(floats));
            }

            // API 可能一次返回不足时兜底
            while (vectors.Count < texts.Count)
                vectors.Add(new float[Math.Max(1, _expectedDimensions)]);

            return vectors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI embedding failed; returning zero vectors");
            return texts.Select(_ => new float[Math.Max(1, _expectedDimensions)]).ToList();
        }
    }
}
