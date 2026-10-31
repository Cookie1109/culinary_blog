// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries page envelope data.</summary>
/// <param name="Data">The data supplied to this operation.</param>
/// <param name="Meta">The meta supplied to this operation.</param>
/// <param name="Message">The message supplied to this operation.</param>
/// <typeparam name="T">The  type.</typeparam>
public sealed record PageEnvelope<T>(
    IReadOnlyCollection<T> Data,
    PageMeta Meta,
    [property: System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    string? Message = null);
