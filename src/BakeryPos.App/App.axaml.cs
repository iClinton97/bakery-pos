using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BakeryPos.App.Payments;
using BakeryPos.App.ViewModels;
using BakeryPos.Core.Catalogue;
using BakeryPos.Core.Security;
using BakeryPos.Core.Stock;
using BakeryPos.Data;
using BakeryPos.Web;

namespace BakeryPos.App;

public partial class App : Application
{
    private const int StockPagePort = 5080;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var catalogue = SampleCatalogue.Create();

            // The database and the PIN file live in the user's local app data folder, outside the repository.
            var dataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BakeryPos");

            var store = new SqliteShopStore(Path.Combine(dataFolder, "bakery.db"));
            store.Initialise();

            var stock = SampleStock.Create(catalogue);
            if (store.LoadStock(stock) == 0)
            {
                // First run: keep the sample starting counts.
                store.SaveStock(stock);
            }

            var feed = new LiveStockFeed();
            var viewModel = new MainWindowViewModel(catalogue, new InteractiveMockTerminal(), stock, store, feed);

            desktop.MainWindow = new MainWindow { DataContext = viewModel };

            StartStockPage(desktop, feed, viewModel, dataFolder);
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Starts the view-only stock page for other devices on the shop network, if a PIN has been set up.</summary>
    private static void StartStockPage(
        IClassicDesktopStyleApplicationLifetime desktop,
        LiveStockFeed feed,
        MainWindowViewModel viewModel,
        string dataFolder)
    {
        var pin = StockPinSource.Read(dataFolder, Environment.GetEnvironmentVariable);
        if (pin is null)
        {
            viewModel.SetStockPageStatus($"Stock page is off: no PIN set ({StockPinSource.FileName})");
            return;
        }

        StockWebServer server;
        try
        {
            server = new StockWebServer(feed, new StockWebOptions(pin, StockPagePort));
        }
        catch (ArgumentException ex)
        {
            viewModel.SetStockPageStatus($"Stock page is off: {ex.Message}");
            return;
        }

        server.StartAsync().ContinueWith(task =>
        {
            string message;
            if (task.IsFaulted)
            {
                message = $"Stock page could not start. Is port {StockPagePort} already in use?";
            }
            else
            {
                var address = NetworkAddress.FindLanAddress();
                message = address is null
                    ? $"Stock page is on, port {StockPagePort}"
                    : $"Stock page: http://{address}:{StockPagePort}";
            }

            Dispatcher.UIThread.Post(() => viewModel.SetStockPageStatus(message));
        });

        desktop.Exit += (_, _) =>
        {
            try
            {
                server.StopAsync().Wait(TimeSpan.FromSeconds(3));
            }
            catch (Exception)
            {
                // Closing the till must never fail because the web server was slow to stop.
            }
        };
    }
}
