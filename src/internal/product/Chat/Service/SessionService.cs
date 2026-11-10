using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Chat.DBLayer;
using SailNora.Product.Chat.Dtos;

namespace SailNora.Product.Chat.Service;

[Scoped]
public sealed class SessionService
{
    private readonly MooRepository<KnSession> _sessions;
    private readonly MooRepository<KnMessage> _messages;

    public SessionService(MooRepository<KnSession> sessions, MooRepository<KnMessage> messages)
    {
        _sessions = sessions;
        _messages = messages;
    }

    public SessionDto Create(Guid spaceId, Guid userId, CreateSessionInput input)
    {
        var now = DateTime.Now;
        var entity = new KnSession
        {
            Id = Guid.NewGuid().ToString(),
            SpaceId = spaceId,
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(input.Title) ? "新会话" : input.Title.Trim(),
            CreateTime = now,
            UpdateTime = now
        };
        _sessions.Repo.Insert(entity);
        return ToDto(entity);
    }

    public List<SessionDto> List(Guid spaceId, Guid userId) =>
        _sessions.Repo.GetList(s => s.SpaceId == spaceId && s.UserId == userId)
            .OrderByDescending(s => s.UpdateTime)
            .Select(ToDto)
            .ToList();

    public KnSession? GetOwned(Guid spaceId, Guid userId, string sessionId)
    {
        var s = _sessions.Repo.GetById(sessionId);
        if (s is null || s.SpaceId != spaceId || s.UserId != userId)
            return null;
        return s;
    }

    public List<KnMessage> ListMessages(string sessionId) =>
        _messages.Repo.GetList(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreateTime)
            .ToList();

    public KnMessage AddMessage(Guid spaceId, string sessionId, string role, string content)
    {
        var msg = new KnMessage
        {
            Id = Guid.NewGuid().ToString(),
            SessionId = sessionId,
            SpaceId = spaceId,
            Role = role,
            Content = content,
            CreateTime = DateTime.Now
        };
        _messages.Repo.Insert(msg);

        var session = _sessions.Repo.GetById(sessionId);
        if (session is not null)
        {
            session.UpdateTime = DateTime.Now;
            if (session.Title == "新会话" && role == "user")
            {
                var title = content.Trim();
                session.Title = title.Length > 40 ? title[..40] : title;
            }

            _sessions.Repo.Update(session);
        }

        return msg;
    }

    private static SessionDto ToDto(KnSession s) => new()
    {
        Id = s.Id,
        Title = s.Title,
        CreateTime = s.CreateTime,
        UpdateTime = s.UpdateTime
    };
}
