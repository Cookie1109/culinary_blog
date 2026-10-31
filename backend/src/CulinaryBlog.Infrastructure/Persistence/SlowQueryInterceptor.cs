// Culinary Blog. Authorship is recorded in the repository history.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Persistence;

/// <summary>Represents the slow query interceptor.</summary>
/// <param name="logger">The logger supplied to this operation.</param>
public sealed class SlowQueryInterceptor(ILogger<SlowQueryInterceptor> logger) : DbCommandInterceptor
{
    private static readonly Action<ILogger, Guid, double, Exception?> SlowQuery = LoggerMessage.Define<Guid, double>(
        LogLevel.Warning, new EventId(4201, nameof(SlowQuery)), "Slow database command {CommandId} took {ElapsedMilliseconds} ms");

    /// <summary>Performs the reader executed operation.</summary>
    /// <param name="command">The command supplied to this operation.</param>
    /// <param name="eventData">The event data supplied to this operation.</param>
    /// <param name="result">The result supplied to this operation.</param>
    /// <returns>The reader executed result.</returns>
    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        this.Record(eventData);
        return result;
    }

    /// <summary>Performs the reader executed operation.</summary>
    /// <param name="command">The command supplied to this operation.</param>
    /// <param name="eventData">The event data supplied to this operation.</param>
    /// <param name="result">The result supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        this.Record(eventData);
        return ValueTask.FromResult(result);
    }

    /// <summary>Performs the non query executed operation.</summary>
    /// <param name="command">The command supplied to this operation.</param>
    /// <param name="eventData">The event data supplied to this operation.</param>
    /// <param name="result">The result supplied to this operation.</param>
    /// <returns>The non query executed result.</returns>
    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        this.Record(eventData);
        return result;
    }

    /// <summary>Performs the non query executed operation.</summary>
    /// <param name="command">The command supplied to this operation.</param>
    /// <param name="eventData">The event data supplied to this operation.</param>
    /// <param name="result">The result supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        this.Record(eventData);
        return ValueTask.FromResult(result);
    }

    /// <summary>Performs the scalar executed operation.</summary>
    /// <param name="command">The command supplied to this operation.</param>
    /// <param name="eventData">The event data supplied to this operation.</param>
    /// <param name="result">The result supplied to this operation.</param>
    /// <returns>The scalar executed result.</returns>
    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        this.Record(eventData);
        return result;
    }

    /// <summary>Performs the scalar executed operation.</summary>
    /// <param name="command">The command supplied to this operation.</param>
    /// <param name="eventData">The event data supplied to this operation.</param>
    /// <param name="result">The result supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        this.Record(eventData);
        return ValueTask.FromResult(result);
    }

    private void Record(CommandExecutedEventData data)
    {
        if (data.Duration.TotalMilliseconds > 100)
        {
            SlowQuery(logger, data.CommandId, data.Duration.TotalMilliseconds, null);
        }
    }
}
