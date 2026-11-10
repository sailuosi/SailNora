using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Sandbox.DBLayer;
using SailNora.Product.Sandbox.Dtos;

namespace SailNora.Product.Sandbox.Service;

public sealed class SandboxConfigService : IScoped
{
    private static readonly HashSet<string> AllowedBackends = new(StringComparer.OrdinalIgnoreCase)
    {
        "host", "docker", "remote"
    };

    private readonly MooRepository<KnSandboxConfig> _configs;
    private readonly SandboxRuntimeService _runtime;

    public SandboxConfigService(MooRepository<KnSandboxConfig> configs, SandboxRuntimeService runtime)
    {
        _configs = configs;
        _runtime = runtime;
    }

    public SandboxConfigDto Create(Guid spaceId, CreateSandboxConfigInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new InvalidOperationException("名称不能为空");
        var backend = string.IsNullOrWhiteSpace(input.Backend) ? "host" : input.Backend.Trim();
        if (!AllowedBackends.Contains(backend))
            throw new InvalidOperationException("backend 仅支持 host|docker|remote");

        var now = DateTime.Now;
        var entity = new KnSandboxConfig
        {
            Id = Guid.NewGuid().ToString(),
            SpaceId = spaceId,
            Name = input.Name.Trim(),
            Description = input.Description?.Trim(),
            Backend = backend.ToLowerInvariant(),
            Image = string.IsNullOrWhiteSpace(input.Image) ? null : input.Image.Trim(),
            WorkDir = string.IsNullOrWhiteSpace(input.WorkDir) ? null : input.WorkDir.Trim(),
            Enabled = true,
            CreateTime = now,
            UpdateTime = now
        };
        _configs.Repo.Insert(entity);
        return ToDto(entity);
    }

    public List<SandboxConfigDto> List(Guid spaceId) =>
        _configs.Repo.GetList(c => c.SpaceId == spaceId)
            .OrderByDescending(c => c.UpdateTime)
            .Select(ToDto)
            .ToList();

    public SandboxConfigDto? Get(Guid spaceId, string id)
    {
        var c = _configs.Repo.GetById(id);
        if (c is null || c.SpaceId != spaceId)
            return null;
        return ToDto(c);
    }

    public SandboxConfigDto Update(Guid spaceId, string id, UpdateSandboxConfigInput input)
    {
        var c = RequireOwned(spaceId, id);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new InvalidOperationException("名称不能为空");
        c.Name = input.Name.Trim();
        if (input.Description is not null)
            c.Description = input.Description.Trim();
        if (!string.IsNullOrWhiteSpace(input.Backend))
        {
            if (!AllowedBackends.Contains(input.Backend))
                throw new InvalidOperationException("backend 仅支持 host|docker|remote");
            c.Backend = input.Backend.Trim().ToLowerInvariant();
        }
        if (input.Image is not null)
            c.Image = string.IsNullOrWhiteSpace(input.Image) ? null : input.Image.Trim();
        if (input.WorkDir is not null)
            c.WorkDir = string.IsNullOrWhiteSpace(input.WorkDir) ? null : input.WorkDir.Trim();
        if (input.Enabled is not null)
            c.Enabled = input.Enabled.Value;
        c.UpdateTime = DateTime.Now;
        _configs.Repo.Update(c);
        return ToDto(c);
    }

    public void Delete(Guid spaceId, string id)
    {
        var c = RequireOwned(spaceId, id);
        _configs.Repo.Delete(c);
    }

    public SandboxInventoryDto Inventory(Guid spaceId, string id)
    {
        _ = RequireOwned(spaceId, id);
        return new SandboxInventoryDto
        {
            ConfigId = id,
            ActiveCount = 0,
            Instances = [],
            Note = "F5d stub: no live sandbox runtime"
        };
    }

    public Task<SandboxProbeResultDto> ProbeAsync(Guid spaceId, string id, CancellationToken ct = default)
    {
        var c = RequireOwned(spaceId, id);
        return _runtime.ProbeHostAsync(spaceId, c, ct);
    }

    public Task<SandboxCommandResultDto> ExecAsync(
        Guid spaceId,
        string id,
        RunSandboxCommandInput input,
        CancellationToken ct = default) =>
        _runtime.ExecAsync(spaceId, id, input, ct);

    private KnSandboxConfig RequireOwned(Guid spaceId, string id)
    {
        var c = _configs.Repo.GetById(id)
                ?? throw new InvalidOperationException("沙箱配置不存在");
        if (c.SpaceId != spaceId)
            throw new UnauthorizedAccessException("无权访问");
        return c;
    }

    private static SandboxConfigDto ToDto(KnSandboxConfig c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Description = c.Description,
        Backend = c.Backend,
        Image = c.Image,
        WorkDir = c.WorkDir,
        Enabled = c.Enabled,
        CreateTime = c.CreateTime,
        UpdateTime = c.UpdateTime
    };
}
