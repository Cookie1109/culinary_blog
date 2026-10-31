// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Behaviors;

/// <summary>Defines the request with body contract.</summary>
/// <typeparam name="TBody">The body type.</typeparam>
public interface IRequestWithBody<out TBody>
{
    /// <summary>Gets the body.</summary>
    TBody Body { get; }
}
