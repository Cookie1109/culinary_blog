// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Content;

/// <summary>The content problem kind.</summary>
public enum ContentProblemKind
{
    /// <summary>The bad request value.</summary>
    BadRequest,

    /// <summary>The forbidden value.</summary>
    Forbidden,

    /// <summary>The not found value.</summary>
    NotFound,

    /// <summary>The conflict value.</summary>
    Conflict,

    /// <summary>The service unavailable value.</summary>
    ServiceUnavailable,

    /// <summary>The validation value.</summary>
    Validation,
}
