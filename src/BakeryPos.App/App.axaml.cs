using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BakeryPos.App.Payments;
using BakeryPos.App.ViewModels;
using BakeryPos.Core.Catalogue;
using BakeryPos.Core.Stock;

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

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(catalogue, new InteractiveMockTerminal(), SampleStock.Create(catalogue)),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
