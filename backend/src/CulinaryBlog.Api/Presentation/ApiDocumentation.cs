// Culinary Blog. Authorship is recorded in the repository history.

using System.Text.Json.Nodes;
using CulinaryBlog.Application.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

namespace CulinaryBlog.Api.Presentation;

/// <summary>Represents the api documentation.</summary>
internal static class ApiDocumentation
{
    private static readonly string[] QuotaHeaderNames = ["X-RateLimit-Limit", "X-RateLimit-Remaining", "X-RateLimit-Reset"];
    private static readonly string[] ImageContentTypes = ["image/jpeg", "image/png", "image/webp", "image/avif"];

    /// <summary>Adds api documentation.</summary>
    /// <param name="services">The services supplied to this operation.</param>
    /// <returns>The add api documentation result.</returns>
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.ShouldInclude = description => description.RelativePath?.StartsWith("api/v1", StringComparison.Ordinal) == true;
            options.AddSchemaTransformer(async (schema, context, cancellationToken) =>
            {
                var type = context.JsonTypeInfo.Type;
                if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(InputField<>))
                {
                    return;
                }

                var value = await context.GetOrCreateSchemaAsync(type.GetGenericArguments()[0], cancellationToken: cancellationToken).ConfigureAwait(false);
                schema.Type = value.Type;
                schema.Format = value.Format;
                schema.Properties = value.Properties;
                schema.Required = value.Required;
                schema.Items = value.Items;
                schema.Enum = value.Enum;
                schema.AnyOf = value.AnyOf;
                schema.AllOf = value.AllOf;
                schema.AdditionalProperties = value.AdditionalProperties;
                schema.Description = "Omitted fields retain their current value. Explicit null clears nullable content; null is invalid for required data.";
            });
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Culinary Blog API",
                    Version = "1.0.0",
                    Description = "REST API v1. Errors use RFC 7807 Problem Details. Authenticate with a JWT access token from /api/v1/auth/login.",
                };
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                    },
                };
                return Task.CompletedTask;
            });
            options.AddOperationTransformer(async (operation, context, cancellationToken) =>
            {
                var path = context.Description.RelativePath ?? string.Empty;
                if (context.Description.HttpMethod == "PUT" && path.StartsWith("api/v1/recipes/{id", StringComparison.Ordinal))
                {
                    var recipeOrIngredientType = path.Contains("/ingredients/", StringComparison.Ordinal) ? typeof(IngredientWriteRequest) : typeof(RecipeWriteRequest);
                    var bodyType = path.Contains("/steps/", StringComparison.Ordinal) ? typeof(StepWriteRequest) : recipeOrIngredientType;
                    var body = await context.GetOrCreateSchemaAsync(bodyType, cancellationToken: cancellationToken).ConfigureAwait(false);
                    var properties = body.Properties!.ToDictionary(pair => pair.Key, pair => pair.Value);
                    if (bodyType == typeof(StepWriteRequest))
                    {
                        properties["stepNumber"] = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" };
                    }

                    operation.RequestBody!.Content!["application/json"].Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = properties,
                        Required = new HashSet<string>(),
                        Description = "Omit a field to retain its value; null clears nullable content. Nutrition merges supplied fields. Empty object is a no-op. If-Match is still required.",
                    };
                }

                var problemSchema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                operation.Responses ??= new OpenApiResponses();
                operation.Responses["422"] = new OpenApiResponse
                {
                    Description = "Semantically invalid field/query data or incomplete recipe content. Field validation errors include errors; version conflicts use409, malformed requests and missing If-Match use400.",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/problem+json"] = new OpenApiMediaType { Schema = problemSchema },
                    },
                };
                static Dictionary<string, IOpenApiHeader> QuotaHeaders(bool includeRetry = false)
                {
                    var headers = new Dictionary<string, IOpenApiHeader>();
                    foreach (var name in QuotaHeaderNames)
                    {
                        headers[name] = new OpenApiHeader
                        {
                            Description = name == "X-RateLimit-Reset" ? "Unix timestamp in seconds when the limiting window next admits a request." : "The binding shared IP quota and its remaining requests.",
                            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int64" },
                        };
                    }

                    if (includeRetry)
                    {
                        headers["Retry-After"] = new OpenApiHeader
                        {
                            Description = "Seconds until retry (at least 1).",
                            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer },
                        };
                    }

                    return headers;
                }

                foreach (var response in operation.Responses.Where(pair => pair.Key.StartsWith('2')).Select(pair => pair.Value).OfType<OpenApiResponse>())
                {
                    response.Headers = QuotaHeaders();
                    if (!(context.Description.HttpMethod == "DELETE" && path.TrimEnd('/') is "api/v1/recipes/{id}" or "api/v1/recipes/{id:guid}") &&
                        (path.StartsWith("api/v1/recipes/{", StringComparison.Ordinal) ||
                        path.StartsWith("api/v1/me/recipes/{", StringComparison.Ordinal) ||
                        path.StartsWith("api/v1/admin/recipes/{", StringComparison.Ordinal) ||
                        (path.TrimEnd('/') == "api/v1/recipes" && context.Description.HttpMethod == "POST")))
                    {
                        response.Headers["ETag"] = new OpenApiHeader
                        {
                            Description = "Strong quoted aggregate version; send it unchanged in If-Match for recipe mutations.",
                            Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                            Example = JsonValue.Create("\"1\""),
                        };
                    }
                }

                if (operation.Responses.TryGetValue("301", out var redirect) && redirect is OpenApiResponse redirectResponse)
                {
                    redirectResponse.Headers = new Dictionary<string, IOpenApiHeader>
                    {
                        ["Location"] = new OpenApiHeader { Description = "Current published recipe URL.", Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri-reference" } },
                    };
                }

                if (path.StartsWith("api/v1/media/", StringComparison.Ordinal) && context.Description.HttpMethod == "GET" &&
                    operation.Responses["200"] is OpenApiResponse mediaResponse)
                {
                    mediaResponse.Content = ImageContentTypes
                            .ToDictionary(contentType => contentType, _ => new OpenApiMediaType { Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" } });
                }

                operation.Responses["429"] = new OpenApiResponse
                {
                    Description = "Shared IP quota exceeded: global fixed 100/minute; auth exact sliding 10/minute; upload fixed 5/minute. Requests consume the global window as well. Headers describe the binding quota. Redis mirrors the shared PostgreSQL clock and ledger; quotas and headers remain enforced during Redis outage and recovery.",
                    Headers = QuotaHeaders(includeRetry: true),
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/problem+json"] = new() { Schema = problemSchema },
                    },
                };
                operation.Responses["4XX"] = new OpenApiResponse
                {
                    Description = "Validation, authentication, authorization, concurrency or rate-limit error. The body also includes code and traceId.",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/problem+json"] = new() { Schema = problemSchema },
                    },
                };
                operation.Responses["5XX"] = new OpenApiResponse
                {
                    Description = "Server error (RFC 7807 Problem Details).",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/problem+json"] = new() { Schema = problemSchema },
                    },
                };
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                if (metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any())
                {
                    operation.Security = [new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = [],
                    },];
                }

                if (context.Description.RelativePath?.StartsWith("api/v1/recipes/{id", StringComparison.Ordinal) == true &&
                    context.Description.HttpMethod is "PUT" or "PATCH" or "POST" or "DELETE")
                {
                    operation.Parameters ??= [];
                    operation.Parameters.Add(new OpenApiParameter
                    {
                        Name = "If-Match",
                        In = ParameterLocation.Header,
                        Required = true,
                        Description = "Current recipe ETag, including quotes, obtained from a recipe response.",
                        Schema = new OpenApiSchema { Type = JsonSchemaType.String },
                        Example = JsonValue.Create("\"1\""),
                    });
                }

                if (context.Description.RelativePath?.StartsWith("api/v1/recipes/{id", StringComparison.Ordinal) == true &&
                    context.Description.RelativePath.EndsWith("/images", StringComparison.Ordinal) && context.Description.HttpMethod == "POST")
                {
                    operation.RequestBody = new OpenApiRequestBody
                    {
                        Required = true,
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["multipart/form-data"] = new()
                            {
                                Schema = new OpenApiSchema
                                {
                                    Type = JsonSchemaType.Object,
                                    Required = new HashSet<string> { "file" },
                                    Properties = new Dictionary<string, IOpenApiSchema>
                                    {
                                        ["file"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" },
                                        ["altText"] = new OpenApiSchema { Type = JsonSchemaType.String },
                                        ["isPrimary"] = new OpenApiSchema { Type = JsonSchemaType.Boolean },
                                    },
                                },
                            },
                        },
                    };
                }
            });
        });
        return services;
    }
}
