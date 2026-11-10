using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Agent.DBLayer;
using SailNora.Product.Agent.Dtos;

namespace SailNora.Product.Agent.Service;

public sealed class AgentService : IScoped
{
    private readonly MooRepository<KnAgent> _agents;

    public AgentService(MooRepository<KnAgent> agents) => _agents = agents;

    public AgentDto Create(Guid spaceId, CreateAgentInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new InvalidOperationException("Agent 名称不能为空");

        var entity = new KnAgent
        {
            Id = Guid.NewGuid().ToString(),
            SpaceId = spaceId,
            Name = input.Name.Trim(),
            Instructions = string.IsNullOrWhiteSpace(input.Instructions)
                ? "你是知识库 Agent，优先使用工具检索再回答。"
                : input.Instructions.Trim(),
            Enabled = true,
            CreateTime = DateTime.Now
        };
        _agents.Repo.Insert(entity);
        return ToDto(entity);
    }

    public List<AgentDto> List(Guid spaceId) =>
        _agents.Repo.GetList(a => a.SpaceId == spaceId)
            .OrderByDescending(a => a.CreateTime)
            .Select(ToDto)
            .ToList();

    public KnAgent? GetOwned(Guid spaceId, string agentId)
    {
        var a = _agents.Repo.GetById(agentId);
        if (a is null || a.SpaceId != spaceId || !a.Enabled)
            return null;
        return a;
    }

    private static AgentDto ToDto(KnAgent a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        Instructions = a.Instructions,
        Enabled = a.Enabled,
        CreateTime = a.CreateTime
    };
}
