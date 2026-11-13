// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.Infrastructure.RateLimiting;

/// <summary>Carries rate limit reservation data.</summary>
/// <param name="Allowed">The allowed supplied to this operation.</param>
/// <param name="Limit">The limit supplied to this operation.</param>
/// <param name="Remaining">The remaining supplied to this operation.</param>
/// <param name="Reset">The reset supplied to this operation.</param>
/// <param name="RetryAfter">The retry after supplied to this operation.</param>
/// <param name="NowMilliseconds">The now milliseconds supplied to this operation.</param>
public sealed record RateLimitReservation(bool Allowed, int Limit, int Remaining, long Reset, int RetryAfter, long NowMilliseconds);
