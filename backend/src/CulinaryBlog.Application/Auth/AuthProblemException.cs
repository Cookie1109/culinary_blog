namespace CulinaryBlog.Application.Auth;

public sealed class AuthProblemException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
