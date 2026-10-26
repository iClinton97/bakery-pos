using System;
using System.Text;

namespace BakeryPos.Core.Catalogue;

/// <summary>
/// Something the shop sells. Money is stored in whole cents to avoid floating point errors.
/// </summary>
/// <param name="Name">Display name, also the source of <see cref="Id"/>.</param>
/// <param name="ParLevel">How many should be ready to sell when the booth is fully stocked.</param>
/// <param name="AlertAt">At or below this count the product counts as low and joins the refill list.</param>
/// <param name="PopularRank">1 is the most popular. Null means not listed as popular.</param>
/// <param name="PieType">Only set for pies.</param>
public sealed record Product(
    string Name,
    Category Category,
    int PriceCents,
    int ParLevel,
    int AlertAt,
    int? PopularRank = null,
    PieType? PieType = null)
{
    /// <summary>A stable, lower-case identifier such as "bacon-and-egg".</summary>
    public string Id { get; } = MakeId(Name);

    private static string MakeId(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var builder = new StringBuilder();
        foreach (var ch in name.Replace("&", " and ").ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        return builder.ToString().Trim('-');
    }
}
