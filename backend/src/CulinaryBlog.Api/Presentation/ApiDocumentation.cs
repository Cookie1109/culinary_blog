using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

namespace CulinaryBlog.Api.Presentation;

internal static class ApiDocumentation
{
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.ShouldInclude = description => description.RelativePath?.StartsWith("api/v1", StringComparison.Ordinal) == true;
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
                var problemSchema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                operation.Responses ??= new OpenApiResponses();
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
                    }];
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
