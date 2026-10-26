using System;
using System.Collections.Generic;
using System.Linq;

namespace BakeryPos.Core.Catalogue;

/// <summary>The full list of products, with the lookups the cashier screen needs.</summary>
public sealed class ProductCatalogue
{
    private readonly Dictionary<string, Product> _byId;

    public ProductCatalogue(IEnumerable<Product> products)
    {
        ArgumentNullException.ThrowIfNull(products);

        All = products.ToList();
        _byId = new Dictionary<string, Product>();

        foreach (var product in All)
        {
            if (product.PriceCents <= 0)
            {
                throw new ArgumentException($"{product.Name} must have a price above zero.", nameof(products));
            }

            if (product.AlertAt > product.ParLevel)
            {
                throw new ArgumentException($"{product.Name} cannot alert above its par level.", nameof(products));
            }

            if (!_byId.TryAdd(product.Id, product))
            {
                throw new ArgumentException($"Two products share the id '{product.Id}'.", nameof(products));
            }
        }
    }

    public IReadOnlyList<Product> All { get; }

    public IReadOnlyList<Product> InCategory(Category category) =>
        All.Where(p => p.Category == category).ToList();

    /// <summary>Popular products, most popular first.</summary>
    public IReadOnlyList<Product> Popular(Category category) =>
        InCategory(category)
            .Where(p => p.PopularRank is not null)
            .OrderBy(p => p.PopularRank)
            .ToList();

    /// <summary>Products that are not listed as popular, in their original order.</summary>
    public IReadOnlyList<Product> NotPopular(Category category) =>
        InCategory(category).Where(p => p.PopularRank is null).ToList();

    public Product? Find(string id) => _byId.GetValueOrDefault(id);
}
