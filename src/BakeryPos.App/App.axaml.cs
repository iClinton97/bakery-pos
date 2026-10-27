using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BakeryPos.App.Payments;
using BakeryPos.App.ViewModels;
using BakeryPos.Core.Catalogue;

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
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(SampleCatalogue.Create(), new InteractiveMockTerminal()),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
