namespace BakeryPos.Core.Catalogue;

/// <summary>The tabs on the cashier screen. Miscellaneous is not a category because it has no products.</summary>
public enum Category
{
    Pies,
    HotFood,
    Cakes,
    Savoury,
}

/// <summary>Used only for pies, so the cashier screen can colour-code them.</summary>
public enum PieType
{
    Beef,
    Lamb,
    Chicken,
    Bacon,
    SausageRoll,
    VegAndCheese,
}
