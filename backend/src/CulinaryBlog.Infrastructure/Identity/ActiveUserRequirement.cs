// Culinary Blog. Authorship is recorded in the repository history.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Represents the active user requirement.</summary>
public sealed class ActiveUserRequirement : IAuthorizationRequirement;
