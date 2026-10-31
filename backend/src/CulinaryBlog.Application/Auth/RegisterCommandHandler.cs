// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Auth;

/// <summary>Represents the register command handler.</summary>
/// <param name="service">The service supplied to this operation.</param>
internal sealed class RegisterCommandHandler(IAuthService service) : IRequestHandler<RegisterCommand, AuthSessionDto>
{
    /// <summary>Handles the register command handler.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<AuthSessionDto> Handle(RegisterCommand request, CancellationToken cancellationToken) =>
        service.RegisterAsync(request.Body, request.IpAddress, request.UserAgent, cancellationToken);
}
