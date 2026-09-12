using System.ComponentModel.DataAnnotations;

namespace CulinaryBlog.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; init; }

    [Required]
    public string Host { get; init; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; init; } = 1025;

    [Required]
    [EmailAddress]
    public string FromAddress { get; init; } = "hello@culinaryblog.local";

    [Required]
    public string FromName { get; init; } = "Culinary Blog";
}
