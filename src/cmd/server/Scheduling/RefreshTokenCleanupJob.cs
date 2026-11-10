using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Platform.Auth.DBLayer;

namespace SailNora.App.Scheduling;

public sealed class RefreshTokenCleanupJob : IScoped
{
    private readonly MooRepository<SysRefreshToken> _tokens;

    public RefreshTokenCleanupJob(MooRepository<SysRefreshToken> tokens) => _tokens = tokens;

    public Task ExecuteAsync()
    {
        var expired = _tokens.Repo.GetList(t => t.ExpireTime < DateTime.Now || t.IsRevoked);
        foreach (var token in expired)
            _tokens.Repo.Delete(token);
        return Task.CompletedTask;
    }
}
