// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Sockets;
using CulinaryBlog.Application.Media;
using CulinaryBlog.Infrastructure.Configuration;
using CulinaryBlog.Infrastructure.Media;
using CulinaryBlog.Infrastructure.Persistence;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Testcontainers.Minio;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Represents the controlled media storage.</summary>
/// <param name="inner">The inner supplied to this operation.</param>
public sealed class ControlledMediaStorage(IFileStorageService inner) : IFileStorageService
{
    /// <summary>Gets or sets a value indicating whether fail variants.</summary>
    public bool FailVariants { get; set; }

    /// <summary>Gets or sets a value indicating whether fail after original.</summary>
    public bool FailAfterOriginal { get; set; }

    /// <summary>Gets or sets a value indicating whether fail deletes.</summary>
    public bool FailDeletes { get; set; }

    /// <summary>Gets or sets the after upload.</summary>
    public Func<string, Task>? AfterUpload { get; set; }

    /// <summary>Uploads the controlled media storage.</summary>
    /// <param name="objectKey">The object key supplied to this operation.</param>
    /// <param name="content">The content supplied to this operation.</param>
    /// <param name="length">The length supplied to this operation.</param>
    /// <param name="contentType">The content type supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task UploadAsync(string objectKey, Stream content, long length, string contentType, CancellationToken cancellationToken)
    {
        if (this.FailVariants && objectKey.EndsWith(".webp", StringComparison.Ordinal) && !objectKey.Contains("original", StringComparison.Ordinal))
        {
            throw new IOException("Controlled variant failure.");
        }

        await inner.UploadAsync(objectKey, content, length, contentType, cancellationToken);
        if (this.FailAfterOriginal && objectKey.Contains("original", StringComparison.Ordinal))
        {
            throw new IOException("Controlled failure after original storage.");
        }

        if (this.AfterUpload is not null)
        {
            await this.AfterUpload(objectKey);
        }
    }

    /// <summary>Performs the open read operation.</summary>
    /// <param name="objectKey">The object key supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken) => inner.OpenReadAsync(objectKey, cancellationToken);

    /// <summary>Deletes the controlled media storage.</summary>
    /// <param name="objectKey">The object key supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) => this.FailDeletes ? Task.FromException(new IOException("Controlled deletion failure.")) : inner.DeleteAsync(objectKey, cancellationToken);

    /// <summary>Performs the exists operation.</summary>
    /// <param name="objectKey">The object key supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken) => inner.ExistsAsync(objectKey, cancellationToken);

    /// <summary>Lists keys.</summary>
    /// <param name="prefix">The prefix supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>The list keys result.</returns>
    public IAsyncEnumerable<string> ListKeysAsync(string prefix, CancellationToken cancellationToken) => inner.ListKeysAsync(prefix, cancellationToken);
}
