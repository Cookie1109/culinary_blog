// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Auth;

/// <summary>Represents the logout command handler.</summary>
/// <param name="service">The service supplied to this operation.</param>
internal sealed class LogoutCommandHandler(IAuthService service) : IRequestHandler<LogoutCommand>
{
    /// <summary>Handles the logout command handler.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var owner = await service.FindRefreshTokenOwnerAsync(request.Body.RefreshToken, cancellationToken).ConfigureAwait(false);
        if (owner is not null && request.AuthenticatedUserId is not null && owner != request.AuthenticatedUserId)
        {
            throw new ContentProblemException("FORBIDDEN", "The refresh token belongs to another session owner.", ContentProblemKind.Forbidden);
        }

        await service.LogoutAsync(request.Body.RefreshToken, request.IpAddress, cancellationToken).ConfigureAwait(false);
    }
}
