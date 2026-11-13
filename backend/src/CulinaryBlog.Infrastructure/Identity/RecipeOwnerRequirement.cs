// Culinary Blog. Authorship is recorded in the repository history.

using System.Security.Claims;
using CulinaryBlog.Domain.Recipes;
using Microsoft.AspNetCore.Authorization;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Represents the recipe owner requirement.</summary>
public sealed class RecipeOwnerRequirement : IAuthorizationRequirement;
