// Culinary Blog. Authorship is recorded in the repository history.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the input field json converter factory.</summary>
public sealed class InputFieldJsonConverterFactory : JsonConverterFactory
{
    /// <summary>Performs the can convert operation.</summary>
    /// <param name="typeToConvert">The type to convert supplied to this operation.</param>
    /// <returns>The can convert result.</returns>
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(InputField<>);

    /// <summary>Creates converter.</summary>
    /// <param name="typeToConvert">The type to convert supplied to this operation.</param>
    /// <param name="options">The options supplied to this operation.</param>
    /// <returns>The create converter result.</returns>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(InputFieldJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;

    private sealed class InputFieldJsonConverter<T> : JsonConverter<InputField<T>>
    {
        public override InputField<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(JsonSerializer.Deserialize<T>(ref reader, options)!);

        public override void Write(Utf8JsonWriter writer, InputField<T> value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value.Value, options);
    }
}
