using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BogChatDesktopClient.Services;
using BogChatDesktopClient.ViewModels;
using BogChatDesktopClient.Views;

namespace BogChatDesktopClient;

public partial class App : Application
{
    private AuthentikService _authentikService = new();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splashScreen = new SplashScreen
            {
                DataContext = new SplashScreenViewModel()
            };


            desktop.MainWindow = splashScreen;
        }

        base.OnFrameworkInitializationCompleted();

        // _ = _authentikService.GetAuthentikStuff();
    }
}