// Culinary Blog. Authorship is recorded in the repository history.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CulinaryBlog.Application.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Carries access token result data.</summary>
/// <param name="Token">The token supplied to this operation.</param>
/// <param name="ExpiresAt">The UTC expiration time.</param>
public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);
