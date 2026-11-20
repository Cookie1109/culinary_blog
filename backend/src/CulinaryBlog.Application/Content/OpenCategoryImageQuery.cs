// Culinary Blog. Authorship is recorded in the repository history.

using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries open category image query data.</summary>
/// <param name="FileName">The stored file name including its extension.</param>
public sealed record OpenCategoryImageQuery(string FileName) : IRequest<MediaFileDto>;
