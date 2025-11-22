using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using XDM.Core;
using XDM.Core.BrowserMonitoring;
using XDM.Core.UI;
using XDM.Desktop.ViewModels;
using XDM.Desktop.Views;

namespace XDM.Desktop;

public partial class App : global::Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var desktopApplication = new DesktopApplication();
        var mainViewModel = new MainWindowViewModel(desktopApplication);

        ApplicationContext.Configurer()
            .RegisterApplication(desktopApplication)
            .RegisterApplicationCore(new ApplicationCore())
            .RegisterClipboardMonitor(new XDM.Core.ClipboardMonitor())
            .RegisterLinkRefresher(new LinkRefresher())
            .RegisterPlatformUIService(new PlatformUIService())
            .RegisterCapturedVideoTracker(new VideoTracker())
            .RegisterApplicationWindow(new ApplicationWindow())
            .Configure();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
