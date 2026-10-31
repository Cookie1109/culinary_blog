// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Auth;

/// <summary>Carries data envelope data.</summary>
/// <param name="Data">The data supplied to this operation.</param>
/// <typeparam name="T">The  type.</typeparam>
public sealed record DataEnvelope<T>(T Data);
