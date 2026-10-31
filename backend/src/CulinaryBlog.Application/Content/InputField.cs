// Culinary Blog. Authorship is recorded in the repository history.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries input field data.</summary>
/// <param name="Value">The value supplied to this operation.</param>
/// <typeparam name="T">The  type.</typeparam>
[JsonConverter(typeof(InputFieldJsonConverterFactory))]
public readonly record struct InputField<T>(T Value)
{
    /// <summary>Gets or initializes a value indicating whether is specified.</summary>
    public bool IsSpecified { get; init; } = true;

    /// <summary>Performs the or operation.</summary>
    /// <param name="existing">The existing supplied to this operation.</param>
    /// <returns>The or result.</returns>
    public T Or(T existing) => this.IsSpecified ? this.Value : existing;
}
