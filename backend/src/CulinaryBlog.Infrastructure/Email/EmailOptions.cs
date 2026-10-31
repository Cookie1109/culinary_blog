// Culinary Blog. Authorship is recorded in the repository history.

using System.ComponentModel.DataAnnotations;

namespace CulinaryBlog.Infrastructure.Email;

/// <summary>Represents the email options.</summary>
public sealed class EmailOptions
{
    /// <summary>The section name.</summary>
    public const string SectionName = "Email";

    /// <summary>Gets or initializes a value indicating whether enabled.</summary>
    public bool Enabled { get; init; }

    /// <summary>Gets or initializes the host.</summary>
    [Required]
    public string Host { get; init; } = "localhost";

    /// <summary>Gets or initializes the port.</summary>
    [Range(1, 65535)]
    public int Port { get; init; } = 1025;

    /// <summary>Gets or initializes a value indicating whether enable ssl.</summary>
    public bool EnableSsl { get; init; }

    /// <summary>Gets or initializes the username.</summary>
    public string? Username { get; init; }

    /// <summary>Gets or initializes the password.</summary>
    public string? Password { get; init; }

    /// <summary>Gets or initializes the from address.</summary>
    [Required]
    [EmailAddress]
    public string FromAddress { get; init; } = "hello@culinaryblog.local";

    /// <summary>Gets or initializes the from name.</summary>
    [Required]
    public string FromName { get; init; } = "Culinary Blog";
}
