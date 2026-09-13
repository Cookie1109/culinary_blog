namespace CulinaryBlog.Application.Content;

public enum ContentProblemKind
{
    BadRequest,
    Forbidden,
    NotFound,
    Conflict,
}

public sealed class ContentProblemException(
    string code,
    string message,
    ContentProblemKind kind,
    long? currentVersion = null) : Exception(message)
{
    public string Code { get; } = code;

    public ContentProblemKind Kind { get; } = kind;

    public long? CurrentVersion { get; } = currentVersion;
}
