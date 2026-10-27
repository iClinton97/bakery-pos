using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BakeryPos.App.Payments;
using BakeryPos.App.ViewModels;
using BakeryPos.Core.Catalogue;
using BakeryPos.Core.Stock;
using BakeryPos.Data;

namespace BakeryPos.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var catalogue = SampleCatalogue.Create();

            // The database lives in the user's local app data folder, outside the repository.
            var databasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BakeryPos",
                "bakery.db");

            var store = new SqliteShopStore(databasePath);
            store.Initialise();

            var stock = SampleStock.Create(catalogue);
            if (store.LoadStock(stock) == 0)
            {
                // First run: keep the sample starting counts.
                store.SaveStock(stock);
            }

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(catalogue, new InteractiveMockTerminal(), stock, store),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
