using mooSQL.data;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Foundation.Security;
using SailNora.Platform.Space.DBLayer;
using SailNora.Platform.Space.Dtos;

namespace SailNora.Platform.Space.Service;

public sealed class SpaceService : IScoped
{
    private readonly MooRepository<SysSpace> _spaces;
    private readonly MooRepository<SysSpaceMember> _members;
    private readonly MooRepository<SysApiKey> _apiKeys;

    public SpaceService(
        MooRepository<SysSpace> spaces,
        MooRepository<SysSpaceMember> members,
        MooRepository<SysApiKey> apiKeys)
    {
        _spaces = spaces;
        _members = members;
        _apiKeys = apiKeys;
    }

    public List<SpaceDto> ListForUser(Guid userId)
    {
        var memberships = _members.Repo.GetList(m => m.UserId == userId);
        if (memberships.Count == 0)
            return [];

        var result = new List<SpaceDto>();
        foreach (var m in memberships)
        {
            var space = _spaces.Repo.GetById(m.SpaceId);
            if (space is null || space.Status != SpaceStatus.Enabled)
                continue;
            result.Add(new SpaceDto
            {
                Id = space.Id.ToString(),
                Name = space.Name,
                Slug = space.Slug,
                Role = m.Role
            });
        }

        return result;
    }

    public SpaceDto Create(Guid userId, CreateSpaceInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new InvalidOperationException("空间名称不能为空");

        var now = DateTime.Now;
        var space = new SysSpace
        {
            Id = Guid.NewGuid(),
            Name = input.Name.Trim(),
            Slug = string.IsNullOrWhiteSpace(input.Slug) ? null : input.Slug.Trim(),
            Status = SpaceStatus.Enabled,
            CreateTime = now
        };
        var member = new SysSpaceMember
        {
            Id = Guid.NewGuid(),
            SpaceId = space.Id,
            UserId = userId,
            Role = SpaceRoles.Owner,
            CreateTime = now
        };

        var work = _spaces.Db.useWork();
        work.Insert(space);
        work.Insert(member);
        work.Commit();

        return new SpaceDto
        {
            Id = space.Id.ToString(),
            Name = space.Name,
            Slug = space.Slug,
            Role = SpaceRoles.Owner
        };
    }

    public SysSpaceMember? GetMembership(Guid userId, Guid spaceId) =>
        _members.Repo.GetFirst(m => m.UserId == userId && m.SpaceId == spaceId);

    public CreateApiKeyResult CreateApiKey(Guid spaceId, CreateApiKeyInput input)
    {
        var raw = "sn_" + Convert.ToHexString(global::System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        var prefix = raw[..8];
        var entity = new SysApiKey
        {
            Id = Guid.NewGuid(),
            SpaceId = spaceId,
            Name = string.IsNullOrWhiteSpace(input.Name) ? "default" : input.Name.Trim(),
            KeyHash = RefreshTokenHasher.Hash(raw),
            Prefix = prefix,
            Enabled = true,
            CreateTime = DateTime.Now
        };
        _apiKeys.Repo.Insert(entity);
        return new CreateApiKeyResult
        {
            Id = entity.Id.ToString(),
            Name = entity.Name,
            Prefix = prefix,
            ApiKey = raw
        };
    }

    public SysApiKey? FindApiKeyByRaw(string rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
            return null;
        var hash = RefreshTokenHasher.Hash(rawKey.Trim());
        return _apiKeys.Repo.GetFirst(k => k.KeyHash == hash && k.Enabled);
    }
}
