// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Auth;

/// <summary>Carries application actor data.</summary>
/// <param name="Id">The identifier of the requested resource.</param>
/// <param name="IsActive">The is active supplied to this operation.</param>
/// <param name="Roles">The roles supplied to this operation.</param>
public sealed record ApplicationActor(string Id, bool IsActive, IReadOnlyCollection<string> Roles);
