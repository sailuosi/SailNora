using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SailNora.Foundation.Auth;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Foundation.Options;
using SailNora.Foundation.Security;
using SailNora.Foundation.Tenant;
using SailNora.Platform.Auth.DBLayer;
using SailNora.Platform.Auth.Dtos;
using SailNora.Platform.Space.Service;
using SailNora.Platform.User.DBLayer;

namespace SailNora.Platform.Auth.Service;

public sealed class AuthService : IScoped
{
    private readonly MooRepository<SysUser> _users;
    private readonly MooRepository<SysRefreshToken> _refreshTokens;
    private readonly SpaceService _spaces;
    private readonly JwtOptions _jwt;

    public AuthService(
        MooRepository<SysUser> users,
        MooRepository<SysRefreshToken> refreshTokens,
        SpaceService spaces,
        IOptions<JwtOptions> jwt)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _spaces = spaces;
        _jwt = jwt.Value;
    }

    public Task<LoginResult?> LoginAsync(LoginInput input, CancellationToken cancellationToken = default)
    {
        var user = _users.Repo.GetList(u => u.Account == input.UserName).FirstOrDefault();
        if (user is null || user.Status != UserStatus.Enabled)
            return Task.FromResult<LoginResult?>(null);

        if (!PasswordHasher.Verify(input.Password, user.Password))
            return Task.FromResult<LoginResult?>(null);

        return Task.FromResult<LoginResult?>(CreateTokens(user));
    }

    public LoginResult Refresh(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new InvalidOperationException("RefreshToken 不能为空");

        var hash = RefreshTokenHasher.Hash(refreshToken.Trim());
        var stored = _refreshTokens.Repo.GetFirst(t =>
            t.TokenHash == hash && !t.IsRevoked && t.ExpireTime > DateTime.Now);

        if (stored is null)
            throw new InvalidOperationException("RefreshToken 无效或已过期");

        var user = _users.Repo.GetById(stored.UserId)
                   ?? throw new InvalidOperationException("用户不存在");

        stored.IsRevoked = true;
        _refreshTokens.Repo.Update(stored);
        return CreateTokens(user);
    }

    public Task<UserInfoResult?> GetUserInfoAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = _users.Repo.GetById(userId);
        if (user is null)
            return Task.FromResult<UserInfoResult?>(null);

        var spaces = _spaces.ListForUser(userId);
        return Task.FromResult<UserInfoResult?>(new UserInfoResult
        {
            UserId = user.Id.ToString(),
            Account = user.Account,
            UserName = user.RealName ?? user.Account,
            Permissions = ["skeleton:probe", "space:read"],
            Spaces = spaces,
            DefaultSpaceId = spaces.FirstOrDefault()?.Id
        });
    }

    private LoginResult CreateTokens(SysUser user)
    {
        var spaces = _spaces.ListForUser(user.Id);
        var primary = spaces.FirstOrDefault();

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_jwt.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new("uid", user.Id.ToString()),
            new(ClaimTypes.Name, user.Account),
            new(JwtRegisteredClaimNames.Sub, user.Account),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(PermissionClaimTypes.Permission, "skeleton:probe"),
            new(PermissionClaimTypes.Permission, "space:read")
        };

        if (primary is not null)
        {
            claims.Add(new Claim(SpaceClaimTypes.SpaceId, primary.Id));
            claims.Add(new Claim(SpaceClaimTypes.SpaceRole, primary.Role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        var access = new JwtSecurityTokenHandler().WriteToken(jwt);
        var refreshRaw = RefreshTokenHasher.CreateRawToken();
        var refreshEntity = new SysRefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = RefreshTokenHasher.Hash(refreshRaw),
            ExpireTime = DateTime.Now.AddDays(_jwt.RefreshTokenDays),
            IsRevoked = false,
            CreateTime = DateTime.Now
        };
        _refreshTokens.Repo.Insert(refreshEntity);

        return new LoginResult
        {
            AccessToken = access,
            RefreshToken = refreshRaw,
            ExpiresIn = _jwt.AccessTokenMinutes * 60,
            SpaceId = primary?.Id,
            SpaceRole = primary?.Role
        };
    }
}
