using System.Security.Cryptography;
using System.Text;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Chat.Dtos;
using SailNora.Product.Chat.Service;
using SailNora.Product.Im.DBLayer;
using SailNora.Product.Im.Dtos;

namespace SailNora.Product.Im.Service;

public sealed class ImChannelService : IScoped
{
    private readonly MooRepository<KnImChannel> _channels;
    private readonly MooRepository<KnImInboundEvent> _events;
    private readonly SessionService _sessions;

    public ImChannelService(
        MooRepository<KnImChannel> channels,
        MooRepository<KnImInboundEvent> events,
        SessionService sessions)
    {
        _channels = channels;
        _events = events;
        _sessions = sessions;
    }

    public ImChannelDto Create(Guid spaceId, CreateImChannelInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new InvalidOperationException("名称不能为空");

        var secret = "im_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var entity = new KnImChannel
        {
            Id = Guid.NewGuid().ToString(),
            SpaceId = spaceId,
            Name = input.Name.Trim(),
            Provider = string.IsNullOrWhiteSpace(input.Provider) ? "generic" : input.Provider.Trim(),
            Secret = secret,
            KnowledgeBaseId = string.IsNullOrWhiteSpace(input.KnowledgeBaseId) ? null : input.KnowledgeBaseId.Trim(),
            Enabled = true,
            CreateTime = DateTime.Now
        };
        _channels.Repo.Insert(entity);
        var dto = ToDto(entity);
        dto.Secret = secret;
        return dto;
    }

    public List<ImChannelDto> List(Guid spaceId) =>
        _channels.Repo.GetList(c => c.SpaceId == spaceId)
            .OrderByDescending(c => c.CreateTime)
            .Select(ToDto)
            .ToList();

    public ImChannelDto? Get(Guid spaceId, string id)
    {
        var c = _channels.Repo.GetById(id);
        if (c is null || c.SpaceId != spaceId)
            return null;
        return ToDto(c);
    }

    public void Delete(Guid spaceId, string id)
    {
        var c = _channels.Repo.GetById(id)
                ?? throw new InvalidOperationException("渠道不存在");
        if (c.SpaceId != spaceId)
            throw new UnauthorizedAccessException("无权访问");
        _channels.Repo.Delete(c);
    }

    public KnImChannel? GetEnabled(string channelId)
    {
        var c = _channels.Repo.GetById(channelId);
        if (c is null || !c.Enabled)
            return null;
        return c;
    }

    public static bool VerifySignature(string secret, string rawBody, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
            return false;

        var provided = signatureHeader.Trim();
        if (provided.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            provided = provided["sha256=".Length..];

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        try
        {
            var providedBytes = Convert.FromHexString(provided);
            return CryptographicOperations.FixedTimeEquals(hash, providedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public ImWebhookResultDto HandleInbound(KnImChannel channel, ImWebhookBody body)
    {
        if (!string.IsNullOrWhiteSpace(body.Challenge))
            return new ImWebhookResultDto { Ok = true, Challenge = body.Challenge };

        var text = (body.Text ?? string.Empty).Trim();
        if (text.Length == 0)
            throw new InvalidOperationException("缺少 text");

        var guest = GuestUserId(channel.Id, body.UserId);
        var session = _sessions.Create(channel.SpaceId, guest, new CreateSessionInput
        {
            Title = $"im:{channel.Name}"
        });
        _sessions.AddMessage(channel.SpaceId, session.Id, "user", text);

        var evt = new KnImInboundEvent
        {
            Id = Guid.NewGuid().ToString(),
            SpaceId = channel.SpaceId,
            ChannelId = channel.Id,
            ExternalUserId = body.UserId,
            Text = text,
            SessionId = session.Id,
            CreateTime = DateTime.Now
        };
        _events.Repo.Insert(evt);

        return new ImWebhookResultDto
        {
            Ok = true,
            SessionId = session.Id,
            EventId = evt.Id
        };
    }

    public static Guid GuestUserId(string channelId, string? externalUserId)
    {
        var key = "im|" + channelId + "|" + (externalUserId ?? "anon");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static ImChannelDto ToDto(KnImChannel c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Provider = c.Provider,
        KnowledgeBaseId = c.KnowledgeBaseId,
        Enabled = c.Enabled,
        CreateTime = c.CreateTime
    };
}
