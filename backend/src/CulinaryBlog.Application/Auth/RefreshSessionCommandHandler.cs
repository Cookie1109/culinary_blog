// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Auth;

/// <summary>Represents the refresh session command handler.</summary>
/// <param name="service">The service supplied to this operation.</param>
internal sealed class RefreshSessionCommandHandler(IAuthService service) : IRequestHandler<RefreshSessionCommand, AuthSessionDto>
{
    /// <summary>Handles the refresh session command handler.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<AuthSessionDto> Handle(RefreshSessionCommand request, CancellationToken cancellationToken) =>
        service.RefreshAsync(request.Body.RefreshToken, request.IpAddress, request.UserAgent, cancellationToken);
}
