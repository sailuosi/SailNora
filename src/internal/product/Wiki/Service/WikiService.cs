using System.Text.RegularExpressions;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Wiki.DBLayer;
using SailNora.Product.Wiki.Dtos;

namespace SailNora.Product.Wiki.Service;

public sealed class WikiService : IScoped
{
    private readonly MooRepository<KnWikiPage> _pages;

    public WikiService(MooRepository<KnWikiPage> pages) => _pages = pages;

    public WikiPageDto Create(Guid spaceId, CreateWikiPageInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
            throw new InvalidOperationException("标题不能为空");

        var slug = string.IsNullOrWhiteSpace(input.Slug)
            ? Slugify(input.Title)
            : Slugify(input.Slug);
        if (_pages.Repo.GetFirst(p => p.SpaceId == spaceId && p.Slug == slug) is not null)
            throw new InvalidOperationException("slug 已存在");

        var now = DateTime.Now;
        var entity = new KnWikiPage
        {
            Id = Guid.NewGuid().ToString(),
            SpaceId = spaceId,
            KnowledgeBaseId = string.IsNullOrWhiteSpace(input.KnowledgeBaseId) ? null : input.KnowledgeBaseId.Trim(),
            Slug = slug,
            Title = input.Title.Trim(),
            Content = input.Content ?? string.Empty,
            Status = WikiPageStatuses.Draft,
            CreateTime = now,
            UpdateTime = now
        };
        _pages.Repo.Insert(entity);
        return ToDto(entity);
    }

    public List<WikiPageSummaryDto> List(Guid spaceId) =>
        _pages.Repo.GetList(p => p.SpaceId == spaceId)
            .OrderByDescending(p => p.UpdateTime)
            .Select(p => new WikiPageSummaryDto
            {
                Id = p.Id,
                Slug = p.Slug,
                Title = p.Title,
                Status = string.IsNullOrWhiteSpace(p.Status) ? WikiPageStatuses.Draft : p.Status,
                UpdateTime = p.UpdateTime
            })
            .ToList();

    public WikiPageDto? GetBySlug(Guid spaceId, string slug)
    {
        var page = _pages.Repo.GetFirst(p => p.SpaceId == spaceId && p.Slug == slug);
        return page is null ? null : ToDto(page);
    }

    public WikiPageDto? GetById(Guid spaceId, string id)
    {
        var page = _pages.Repo.GetById(id);
        if (page is null || page.SpaceId != spaceId)
            return null;
        return ToDto(page);
    }

    public WikiPageDto Update(Guid spaceId, string id, UpdateWikiPageInput input)
    {
        var page = _pages.Repo.GetById(id)
                   ?? throw new InvalidOperationException("页面不存在");
        if (page.SpaceId != spaceId)
            throw new UnauthorizedAccessException("无权访问");

        if (!string.IsNullOrWhiteSpace(input.Title))
            page.Title = input.Title.Trim();
        if (input.Content is not null)
            page.Content = input.Content;
        page.UpdateTime = DateTime.Now;
        _pages.Repo.Update(page);
        return ToDto(page);
    }

    private static string Slugify(string raw)
    {
        var s = raw.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, @"[^a-z0-9\u4e00-\u9fff\-]+", "");
        return string.IsNullOrWhiteSpace(s) ? Guid.NewGuid().ToString("N")[..8] : s;
    }

    private static WikiPageDto ToDto(KnWikiPage p) => new()
    {
        Id = p.Id,
        Slug = p.Slug,
        Title = p.Title,
        Content = p.Content,
        KnowledgeBaseId = p.KnowledgeBaseId,
        Status = string.IsNullOrWhiteSpace(p.Status) ? WikiPageStatuses.Draft : p.Status,
        CreateTime = p.CreateTime,
        UpdateTime = p.UpdateTime
    };
}
