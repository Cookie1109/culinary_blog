// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the create category command handler.</summary>
/// <param name="service">The service supplied to this operation.</param>
internal sealed class CreateCategoryCommandHandler(IContentService service) : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    /// <summary>Handles the create category command handler.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (!request.IsAdmin)
        {
            throw new ContentProblemException("FORBIDDEN", "Administrator access is required.", ContentProblemKind.Forbidden);
        }

        return service.CreateCategoryAsync(request.Body, cancellationToken);
    }
}
