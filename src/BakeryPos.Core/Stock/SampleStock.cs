using System;
using System.Linq;
using BakeryPos.Core.Catalogue;

namespace BakeryPos.Core.Stock;

/// <summary>
/// Starting stock counts for development and demos. These are placeholders, not the shop's real figures.
/// Some items are deliberately low or sold out so the warnings are visible.
/// </summary>
public static class SampleStock
{
    public static StockBoard Create(ProductCatalogue catalogue)
    {
        var board = new StockBoard(catalogue);

        void Set(string name, int booth, int fridge = 0)
        {
            var product = catalogue.All.FirstOrDefault(p => p.Name == name)
                ?? throw new InvalidOperationException($"Sample stock names a product that is not in the catalogue: {name}");
            board.SetBooth(product.Id, booth);
            board.SetFridge(product.Id, fridge);
        }

        // Hot food
        Set("Small dim sim", 18);
        Set("Large dim sim", 8);
        Set("Skewers", 14);
        Set("Tenders", 0);
        Set("Egg & Bacon Sandwich", 10);
        Set("Baked Hotdog", 8);
        Set("Hot Dog", 12);
        Set("Meat Ball", 20);

        // Pies: booth, fridge
        Set("Plain Pies", 22, 48);
        Set("Sausage Rolls", 15, 36);
        Set("Pepper", 9, 18);
        Set("Mushroom", 12, 24);
        Set("Hot Chilli", 4, 20);
        Set("Curry", 10, 22);
        Set("Lamb Curry", 7, 12);
        Set("Lamb Rosemary", 6, 18);
        Set("Meat Pastie", 11, 24);
        Set("Party Pie", 40, 150);
        Set("Party Sausage Rolls", 25, 120);
        Set("Beef Burgundy", 8, 14);
        Set("Chunky", 13, 30);
        Set("Beef Satay", 5, 15);
        Set("Kidney", 6, 10);
        Set("Bacon & Egg", 9, 20);
        Set("Onion Cheese", 8, 16);
        Set("Onion Tomato", 0, 0);
        Set("Onion", 7, 12);
        Set("Bacon & Cheese", 10, 22);
        Set("Cheese", 8, 18);
        Set("Chicken Mushroom", 11, 24);
        Set("Chicken Vegetable", 9, 20);
        Set("Chicken Corn", 12, 26);
        Set("Chicken Satay", 3, 6);
        Set("Spinach Roll", 10, 16);

        // Cakes
        Set("French Vanilla Slice", 6);
        Set("Vanilla Slice", 8);
        Set("Fruit Tart", 5);
        Set("Custard Tart", 9);
        Set("Apple Cake", 7);
        Set("Apple Slice", 6);
        Set("Apple Crumble", 4);
        Set("Lemon Slice", 10);
        Set("Hedge Choc", 8);
        Set("Caramel Slice", 2);
        Set("Raspberry Slice", 0);

        // Savoury
        Set("Pizza", 10);
        Set("Vegemite Scroll small", 14);
        Set("Vegemite Scroll large", 9);
        Set("Large Donut (no jam)", 12);
        Set("Large Jam Donut", 3);
        Set("Small Iced Donut", 16);
        Set("Pineapple Donut", 7);
        Set("Crostoli", 20);
        Set("Croissant", 8);
        Set("Monkey Face", 5);

        return board;
    }
}
