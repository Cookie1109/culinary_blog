// Culinary Blog. Authorship is recorded in the repository history.

using MediatR;

namespace CulinaryBlog.Application.Media;

/// <summary>Carries get sitemap query data.</summary>
public sealed record GetSitemapQuery : IRequest<Stream?>;
