// Culinary Blog. Authorship is recorded in the repository history.

using System.Linq.Expressions;
using System.Text.RegularExpressions;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Infrastructure.Content;

/// <content>Contains database-only expressions used by recipe discovery.</content>
internal sealed partial class ContentService
{
    /// <summary>Builds a PostgreSQL whole-word predicate for an escaped literal search term.</summary>
    /// <param name="term">The literal accented term, never a caller-supplied regex pattern.</param>
    /// <returns>The expression translated by Npgsql to its database regex operator.</returns>
    internal static Expression<Func<Recipe, bool>> BuildAccentWordPredicate(string term)
    {
        var pattern = @"\m" + Regex.Escape(term) + @"\M";

        // Npgsql translates this expression to SQL; DB commands have a 30s timeout.
#pragma warning disable S6444
        return recipe => Regex.IsMatch(recipe.Title, pattern, RegexOptions.IgnoreCase);
#pragma warning restore S6444
    }
}
