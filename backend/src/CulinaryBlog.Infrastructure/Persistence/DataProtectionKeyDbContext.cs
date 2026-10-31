// Culinary Blog. Authorship is recorded in the repository history.

using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

/// <summary>Keeps keyring migrations separate from the application schema.</summary>
/// <param name="options">The options supplied to this operation.</param>
public sealed class DataProtectionKeyDbContext(DbContextOptions<DataProtectionKeyDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    /// <summary>Gets the encrypted data protection keyring.</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => this.Set<DataProtectionKey>();
}
