using System.Globalization;
using System.Text.Json;

namespace SailNora.Product.Ai.Embedding;

public static class EmbeddingCodec
{
    public static string? ToJson(float[]? vector)
    {
        if (vector is null || vector.Length == 0)
            return null;
        return JsonSerializer.Serialize(vector);
    }

    public static float[]? FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<float[]>(json);
        }
        catch
        {
            // 兼容逗号分隔兜底
            var parts = json.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                return null;
            var v = new float[parts.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out v[i]))
                    return null;
            }

            return v;
        }
    }
}
