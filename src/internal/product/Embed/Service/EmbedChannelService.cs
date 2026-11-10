using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Embed.DBLayer;
using SailNora.Product.Embed.Dtos;

namespace SailNora.Product.Embed.Service;

[Scoped]
public sealed class EmbedChannelService
{
    private readonly MooRepository<KnEmbedChannel> _channels;

    public EmbedChannelService(MooRepository<KnEmbedChannel> channels) => _channels = channels;

    public EmbedChannelDto Create(Guid spaceId, CreateEmbedChannelInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new InvalidOperationException("名称不能为空");

        var token = "emb_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var entity = new KnEmbedChannel
        {
            Id = Guid.NewGuid().ToString(),
            SpaceId = spaceId,
            Name = input.Name.Trim(),
            TokenHash = HashToken(token),
            TokenPrefix = token[..8],
            KnowledgeBaseIdsJson = JsonSerializer.Serialize(input.KnowledgeBaseIds ?? []),
            Enabled = true,
            CreateTime = DateTime.Now
        };
        _channels.Repo.Insert(entity);
        var dto = ToDto(entity);
        dto.Token = token;
        return dto;
    }

    public List<EmbedChannelDto> List(Guid spaceId) =>
        _channels.Repo.GetList(c => c.SpaceId == spaceId)
            .OrderByDescending(c => c.CreateTime)
            .Select(ToDto)
            .ToList();

    public EmbedChannelDto? Get(Guid spaceId, string id)
    {
        var c = _channels.Repo.GetById(id);
        if (c is null || c.SpaceId != spaceId)
            return null;
        return ToDto(c);
    }

    public EmbedChannelDto RotateToken(Guid spaceId, string id)
    {
        var c = _channels.Repo.GetById(id)
                ?? throw new InvalidOperationException("渠道不存在");
        if (c.SpaceId != spaceId)
            throw new UnauthorizedAccessException("无权访问");

        var token = "emb_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        c.TokenHash = HashToken(token);
        c.TokenPrefix = token[..8];
        _channels.Repo.Update(c);
        var dto = ToDto(c);
        dto.Token = token;
        return dto;
    }

    public void Delete(Guid spaceId, string id)
    {
        var c = _channels.Repo.GetById(id)
                ?? throw new InvalidOperationException("渠道不存在");
        if (c.SpaceId != spaceId)
            throw new UnauthorizedAccessException("无权访问");
        _channels.Repo.Delete(c);
    }

    public KnEmbedChannel? Authenticate(string channelId, string rawToken)
    {
        var c = _channels.Repo.GetById(channelId);
        if (c is null || !c.Enabled)
            return null;
        if (!string.Equals(c.TokenHash, HashToken(rawToken), StringComparison.Ordinal))
            return null;
        return c;
    }

    public static Guid GuestUserId(string channelId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("embed|" + channelId));
        return new Guid(hash.AsSpan(0, 16));
    }

    public static List<string> ParseKbIds(KnEmbedChannel channel)
    {
        if (string.IsNullOrWhiteSpace(channel.KnowledgeBaseIdsJson))
            return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(channel.KnowledgeBaseIdsJson) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static EmbedChannelDto ToDto(KnEmbedChannel c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        TokenPrefix = c.TokenPrefix,
        KnowledgeBaseIds = ParseKbIds(c),
        Enabled = c.Enabled,
        CreateTime = c.CreateTime
    };
}
