using System.Globalization;
using System.Text.Json;
using CulinaryBlog.Application.Audit;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CulinaryBlog.Infrastructure.Audit;

public sealed class AuditLogService(
    AppDbContext dbContext,
    IConfiguration configuration) : IAuditLogService
{
    public async Task<AuditLogsEnvelope> ListAuditLogsAsync(
        string? level,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var targetPage = Math.Max(1, page);
        var targetPageSize = Math.Clamp(pageSize, 1, 100);

        var entries = new List<AuditLogEntryDto>();

        // 1. Try reading from Serilog rolling log files if available
        try
        {
            var logFiles = GetLogFiles();
            foreach (var file in logFiles)
            {
                var fileEntries = ReadEntriesFromFile(file);
                entries.AddRange(fileEntries);
                if (entries.Count >= 500)
                {
                    break;
                }
            }
        }
        catch
        {
            // Log file reading is best-effort
        }

        // 2. Synthesize audit entries from entity change tracking in database
        var dbEntries = await GetDatabaseAuditEntriesAsync(cancellationToken).ConfigureAwait(false);
        entries.AddRange(dbEntries);

        // Deduplicate and order by timestamp descending
        var query = entries
            .GroupBy(e => e.Id)
            .Select(g => g.First())
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(level) && !string.Equals(level, "all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(e => string.Equals(e.Level, level, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                e.Message.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (e.CorrelationId?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) ||
                (e.UserId?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) ||
                (e.RequestPath?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) ||
                (e.EventName.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        var ordered = query.OrderByDescending(e => e.Timestamp).ToList();
        var total = ordered.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling((double)total / targetPageSize);

        var pagedData = ordered
            .Skip((targetPage - 1) * targetPageSize)
            .Take(targetPageSize)
            .ToList();

        var meta = new PageMeta(
            targetPage,
            targetPageSize,
            total,
            totalPages,
            targetPage < totalPages,
            targetPage > 1);

        var telemetry = new TelemetryStatusDto(
            SeqConfigured: !string.IsNullOrWhiteSpace(configuration["Seq:ServerUrl"]),
            OtlpConfigured: !string.IsNullOrWhiteSpace(configuration["Telemetry:OtlpEndpoint"]),
            OperationalNetworkRestricted: true);

        return new AuditLogsEnvelope(pagedData, meta, telemetry);
    }

    private static IEnumerable<string> GetLogFiles()
    {
        var logDirs = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "logs"),
            Path.Combine(AppContext.BaseDirectory, "logs"),
        };

        foreach (var dir in logDirs)
        {
            if (Directory.Exists(dir))
            {
                return Directory.GetFiles(dir, "api-*.json")
                    .OrderByDescending(File.GetLastWriteTimeUtc);
            }
        }

        return [];
    }

    private static List<AuditLogEntryDto> ReadEntriesFromFile(string filePath)
    {
        var result = new List<AuditLogEntryDto>();
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                var timestamp = root.TryGetProperty("@t", out var tProp) && tProp.TryGetDateTimeOffset(out var dto)
                    ? dto
                    : DateTimeOffset.UtcNow;

                var level = root.TryGetProperty("@l", out var lProp)
                    ? lProp.GetString() ?? "Information"
                    : "Information";

                var message = root.TryGetProperty("@m", out var mProp)
                    ? mProp.GetString() ?? string.Empty
                    : (root.TryGetProperty("@mt", out var mtProp) ? mtProp.GetString() ?? string.Empty : string.Empty);

                var correlationId = root.TryGetProperty("CorrelationId", out var cProp) ? cProp.GetString() : null;
                var userId = root.TryGetProperty("UserId", out var uProp) ? uProp.GetString() : null;
                var requestPath = root.TryGetProperty("RequestPath", out var pProp) ? pProp.GetString() : null;
                var requestMethod = root.TryGetProperty("RequestMethod", out var rProp) ? rProp.GetString() : null;
                int? statusCode = root.TryGetProperty("StatusCode", out var sProp) && sProp.TryGetInt32(out var sc) ? sc : null;

                var eventName = "AppLog";
                if (root.TryGetProperty("EventId", out var evProp))
                {
                    if (evProp.ValueKind == JsonValueKind.Object && evProp.TryGetProperty("Name", out var evNameProp))
                    {
                        eventName = evNameProp.GetString() ?? "AppLog";
                    }
                    else if (evProp.ValueKind == JsonValueKind.Number)
                    {
                        eventName = $"Event_{evProp.GetInt32()}";
                    }
                }
                else if (message.Contains("Audit event", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var idx = Array.FindIndex(parts, p => string.Equals(p, "event", StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0 && idx + 1 < parts.Length)
                    {
                        eventName = parts[idx + 1];
                    }
                }

                var properties = new Dictionary<string, string>();
                foreach (var prop in root.EnumerateObject())
                {
                    if (!prop.Name.StartsWith('@'))
                    {
                        properties[prop.Name] = prop.Value.ToString();
                    }
                }

                result.Add(new AuditLogEntryDto(
                    Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                    timestamp,
                    level,
                    eventName,
                    message,
                    userId,
                    correlationId,
                    requestPath,
                    requestMethod,
                    statusCode,
                    properties));
            }
            catch
            {
                // Ignore malformed individual lines
            }
        }

        return result;
    }

    private async Task<List<AuditLogEntryDto>> GetDatabaseAuditEntriesAsync(CancellationToken cancellationToken)
    {
        var result = new List<AuditLogEntryDto>();

        // Recipes audit events
        var recentRecipes = await dbContext.Recipes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var r in recentRecipes)
        {
            if (r.IsDeleted && r.DeletedAt.HasValue)
            {
                result.Add(new AuditLogEntryDto(
                    $"recipe-del-{r.Id}-{r.DeletedAt.Value.ToUnixTimeSeconds()}",
                    r.DeletedAt.Value,
                    "Warning",
                    "recipe.deleted",
                    $"Recipe '{r.Title}' was soft-deleted.",
                    r.DeletedBy ?? r.AuthorId.ToString(),
                    null,
                    $"/api/v1/recipes/{r.Id}",
                    "DELETE",
                    204,
                    new Dictionary<string, string> { ["RecipeId"] = r.Id.ToString(), ["Title"] = r.Title }));
            }

            if (r.PublishedAt.HasValue)
            {
                result.Add(new AuditLogEntryDto(
                    $"recipe-pub-{r.Id}-{r.PublishedAt.Value.ToUnixTimeSeconds()}",
                    r.PublishedAt.Value,
                    "Information",
                    "recipe.published",
                    $"Recipe '{r.Title}' was published.",
                    r.UpdatedBy ?? r.AuthorId.ToString(),
                    null,
                    $"/api/v1/recipes/{r.Id}/publish",
                    "POST",
                    200,
                    new Dictionary<string, string> { ["RecipeId"] = r.Id.ToString(), ["Title"] = r.Title, ["Slug"] = r.Slug }));
            }

            if (r.UpdatedAt.HasValue && r.UpdatedAt != r.CreatedAt)
            {
                result.Add(new AuditLogEntryDto(
                    $"recipe-upd-{r.Id}-{r.UpdatedAt.Value.ToUnixTimeSeconds()}",
                    r.UpdatedAt.Value,
                    "Information",
                    "recipe.updated",
                    $"Recipe '{r.Title}' updated to version {r.Version}.",
                    r.UpdatedBy ?? r.AuthorId.ToString(),
                    null,
                    $"/api/v1/recipes/{r.Id}",
                    "PUT",
                    200,
                    new Dictionary<string, string> { ["RecipeId"] = r.Id.ToString(), ["Title"] = r.Title, ["Version"] = r.Version.ToString(CultureInfo.InvariantCulture) }));
            }

            result.Add(new AuditLogEntryDto(
                $"recipe-cre-{r.Id}-{r.CreatedAt.ToUnixTimeSeconds()}",
                r.CreatedAt,
                "Information",
                "recipe.created",
                $"Recipe '{r.Title}' created.",
                r.CreatedBy ?? r.AuthorId.ToString(),
                null,
                "/api/v1/recipes",
                "POST",
                201,
                new Dictionary<string, string> { ["RecipeId"] = r.Id.ToString(), ["Title"] = r.Title }));
        }

        // Categories audit events
        var recentCategories = await dbContext.Categories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .Take(25)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var c in recentCategories)
        {
            if (c.IsDeleted && c.DeletedAt.HasValue)
            {
                result.Add(new AuditLogEntryDto(
                    $"cat-del-{c.Id}-{c.DeletedAt.Value.ToUnixTimeSeconds()}",
                    c.DeletedAt.Value,
                    "Warning",
                    "category.deleted",
                    $"Category '{c.Name}' was deleted.",
                    c.DeletedBy,
                    null,
                    $"/api/v1/categories/{c.Id}",
                    "DELETE",
                    204,
                    new Dictionary<string, string> { ["CategoryId"] = c.Id.ToString(), ["Name"] = c.Name }));
            }

            if (c.UpdatedAt.HasValue && c.UpdatedAt != c.CreatedAt)
            {
                result.Add(new AuditLogEntryDto(
                    $"cat-upd-{c.Id}-{c.UpdatedAt.Value.ToUnixTimeSeconds()}",
                    c.UpdatedAt.Value,
                    "Information",
                    "category.updated",
                    $"Category '{c.Name}' was updated (OrderIndex: {c.OrderIndex}).",
                    c.UpdatedBy,
                    null,
                    $"/api/v1/categories/{c.Id}",
                    "PUT",
                    200,
                    new Dictionary<string, string> { ["CategoryId"] = c.Id.ToString(), ["Name"] = c.Name, ["OrderIndex"] = c.OrderIndex.ToString(CultureInfo.InvariantCulture) }));
            }

            result.Add(new AuditLogEntryDto(
                $"cat-cre-{c.Id}-{c.CreatedAt.ToUnixTimeSeconds()}",
                c.CreatedAt,
                "Information",
                "category.created",
                $"Category '{c.Name}' was created.",
                c.CreatedBy,
                null,
                "/api/v1/categories",
                "POST",
                201,
                new Dictionary<string, string> { ["CategoryId"] = c.Id.ToString(), ["Name"] = c.Name }));
        }

        return result;
    }
}
