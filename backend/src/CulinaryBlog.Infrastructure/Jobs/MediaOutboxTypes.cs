// Culinary Blog. Authorship is recorded in the repository history.

using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>Represents the media outbox types.</summary>
internal static class MediaOutboxTypes
{
    /// <summary>The resize image.</summary>
    public const string ResizeImage = "image.resize";

    /// <summary>The delete objects.</summary>
    public const string DeleteObjects = "objects.delete";
}
