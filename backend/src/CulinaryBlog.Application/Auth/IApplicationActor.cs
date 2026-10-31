// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Auth;

/// <summary>Defines the application actor contract.</summary>
public interface IApplicationActor
{
    /// <summary>Gets the i application actor.</summary>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<ApplicationActor?> GetAsync(CancellationToken cancellationToken);
}
