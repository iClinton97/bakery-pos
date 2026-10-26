namespace BakeryPos.Core.Catalogue;

/// <summary>
/// Sample products for development, tests and demos. Prices and stock levels are
/// placeholders, not the real shop's figures.
/// </summary>
public static class SampleCatalogue
{
    public static ProductCatalogue Create() => new(Products);

    private static readonly Product[] Products =
    [
        new("Small dim sim", Category.HotFood, 150, 60, 15, null, null),
        new("Large dim sim", Category.HotFood, 250, 40, 10, null, null),
        new("Skewers", Category.HotFood, 400, 24, 6, null, null),
        new("Tenders", Category.HotFood, 350, 40, 10, null, null),
        new("Egg & Bacon Sandwich", Category.HotFood, 650, 20, 5, null, null),
        new("Baked Hotdog", Category.HotFood, 500, 16, 4, null, null),
        new("Hot Dog", Category.HotFood, 450, 24, 6, null, null),
        new("Meat Ball", Category.HotFood, 150, 40, 8, null, null),
        new("Plain Pies", Category.Pies, 550, 36, 8, 1, PieType.Beef),
        new("Sausage Rolls", Category.Pies, 500, 30, 8, 2, PieType.SausageRoll),
        new("Pepper", Category.Pies, 600, 12, 5, 3, PieType.Beef),
        new("Mushroom", Category.Pies, 600, 12, 5, 4, PieType.Beef),
        new("Hot Chilli", Category.Pies, 600, 12, 5, 5, PieType.Beef),
        new("Curry", Category.Pies, 600, 12, 5, null, PieType.Beef),
        new("Lamb Curry", Category.Pies, 650, 12, 5, null, PieType.Lamb),
        new("Lamb Rosemary", Category.Pies, 650, 12, 5, null, PieType.Lamb),
        new("Meat Pastie", Category.Pies, 550, 12, 5, 6, PieType.Beef),
        new("Party Pie", Category.Pies, 180, 80, 30, 7, PieType.Beef),
        new("Party Sausage Rolls", Category.Pies, 180, 80, 30, 8, PieType.SausageRoll),
        new("Beef Burgundy", Category.Pies, 650, 12, 5, null, PieType.Beef),
        new("Chunky", Category.Pies, 650, 12, 5, null, PieType.Beef),
        new("Beef Satay", Category.Pies, 650, 12, 5, null, PieType.Beef),
        new("Kidney", Category.Pies, 600, 12, 5, null, PieType.Beef),
        new("Bacon & Egg", Category.Pies, 650, 12, 5, null, PieType.Bacon),
        new("Onion Cheese", Category.Pies, 600, 12, 5, null, PieType.VegAndCheese),
        new("Onion Tomato", Category.Pies, 600, 12, 5, null, PieType.VegAndCheese),
        new("Onion", Category.Pies, 600, 12, 5, null, PieType.VegAndCheese),
        new("Bacon & Cheese", Category.Pies, 650, 12, 5, null, PieType.Bacon),
        new("Cheese", Category.Pies, 600, 12, 5, null, PieType.VegAndCheese),
        new("Chicken Mushroom", Category.Pies, 650, 12, 5, null, PieType.Chicken),
        new("Chicken Vegetable", Category.Pies, 650, 12, 5, null, PieType.Chicken),
        new("Chicken Corn", Category.Pies, 650, 12, 5, null, PieType.Chicken),
        new("Chicken Satay", Category.Pies, 650, 12, 5, null, PieType.Chicken),
        new("Spinach Roll", Category.Pies, 550, 12, 5, null, PieType.VegAndCheese),
        new("French Vanilla Slice", Category.Cakes, 550, 12, 3, null, null),
        new("Vanilla Slice", Category.Cakes, 450, 12, 3, null, null),
        new("Fruit Tart", Category.Cakes, 450, 12, 3, null, null),
        new("Custard Tart", Category.Cakes, 350, 12, 3, null, null),
        new("Apple Cake", Category.Cakes, 400, 12, 3, null, null),
        new("Apple Slice", Category.Cakes, 400, 12, 3, null, null),
        new("Apple Crumble", Category.Cakes, 450, 12, 3, null, null),
        new("Lemon Slice", Category.Cakes, 400, 12, 3, null, null),
        new("Hedge Choc", Category.Cakes, 450, 12, 3, null, null),
        new("Caramel Slice", Category.Cakes, 450, 12, 3, null, null),
        new("Raspberry Slice", Category.Cakes, 450, 12, 3, null, null),
        new("Pizza", Category.Savoury, 500, 16, 4, null, null),
        new("Vegemite Scroll small", Category.Savoury, 300, 24, 6, null, null),
        new("Vegemite Scroll large", Category.Savoury, 450, 16, 4, null, null),
        new("Large Donut (no jam)", Category.Savoury, 300, 24, 6, null, null),
        new("Large Jam Donut", Category.Savoury, 350, 24, 6, null, null),
        new("Small Iced Donut", Category.Savoury, 250, 30, 8, null, null),
        new("Pineapple Donut", Category.Savoury, 350, 16, 4, null, null),
        new("Crostoli", Category.Savoury, 300, 30, 8, null, null),
        new("Croissant", Category.Savoury, 450, 14, 4, null, null),
        new("Monkey Face", Category.Savoury, 300, 12, 4, null, null),
    ];
}
