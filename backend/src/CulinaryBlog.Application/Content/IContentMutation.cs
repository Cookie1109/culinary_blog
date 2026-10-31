// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Content;

/// <summary>Defines the content mutation contract.</summary>
public interface IContentMutation;

/// <summary>Defines the public content refresh contract.</summary>
public interface IPublicContentRefresh
{
    /// <summary>Schedules the i public content refresh.</summary>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task ScheduleAsync(CancellationToken cancellationToken = default);
}
