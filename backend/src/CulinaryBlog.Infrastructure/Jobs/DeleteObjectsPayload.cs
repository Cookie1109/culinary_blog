// Culinary Blog. Authorship is recorded in the repository history.

using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>Carries delete objects payload data.</summary>
/// <param name="ObjectKeys">The object keys supplied to this operation.</param>
internal sealed record DeleteObjectsPayload(string[] ObjectKeys);
