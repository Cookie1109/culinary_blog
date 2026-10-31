// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the content problem exception.</summary>
/// <param name="code">The code supplied to this operation.</param>
/// <param name="message">The message supplied to this operation.</param>
/// <param name="kind">The kind supplied to this operation.</param>
/// <param name="currentVersion">The current version supplied to this operation.</param>
public sealed class ContentProblemException(
    string code,
    string message,
    ContentProblemKind kind,
    long? currentVersion = null) : Exception(message)
{
    /// <summary>Gets the code.</summary>
    public string Code { get; } = code;

    /// <summary>Gets the kind.</summary>
    public ContentProblemKind Kind { get; } = kind;

    /// <summary>Gets the current version.</summary>
    public long? CurrentVersion { get; } = currentVersion;
}
