// Culinary Blog. Authorship is recorded in the repository history.

using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>Carries resize image payload data.</summary>
/// <param name="ImageId">The image id supplied to this operation.</param>
internal sealed record ResizeImagePayload(Guid ImageId);
