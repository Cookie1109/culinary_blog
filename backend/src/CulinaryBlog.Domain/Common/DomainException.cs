// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Domain.Common;

/// <summary>Represents the domain exception.</summary>
/// <param name="code">The code supplied to this operation.</param>
/// <param name="message">The message supplied to this operation.</param>
public sealed class DomainException(string code, string message) : Exception(message)
{
    /// <summary>Gets the code.</summary>
    public string Code { get; } = code;
}
