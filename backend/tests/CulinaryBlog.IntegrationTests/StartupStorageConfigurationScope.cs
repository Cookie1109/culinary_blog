// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Infrastructure;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Represents the startup storage configuration scope.</summary>
[CollectionDefinition(nameof(StartupStorageConfigurationScope), DisableParallelization = true)]
public sealed class StartupStorageConfigurationScope;
