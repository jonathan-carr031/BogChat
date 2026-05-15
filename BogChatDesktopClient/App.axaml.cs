using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using BogChatDesktopClient.Services;
using BogChatDesktopClient.ViewModels;
using BogChatDesktopClient.Views;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "BogChatDesktopClient.Controls")]

namespace BogChatDesktopClient;

public partial class App : Application
{
    private AuthentikService _authentikService = new();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splashScreenViewModel = new SplashScreenViewModel();
            var splashScreen = new SplashScreen
            {
                DataContext = splashScreenViewModel
            };

            desktop.MainWindow = splashScreen;

            try
            {
                splashScreenViewModel.StartupMessage = "Checking For Updates...";
                await splashScreenViewModel.CheckForUpdates();
                // await Task.Delay(10000);
            }
            catch (TaskCanceledException)
            {
                splashScreen.Close();
                return;
            }

            var mainWindowViewModel = new MainWindowViewModel();
            var mainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };

            desktop.MainWindow = mainWindow;
            mainWindow.Show();

            splashScreen.Close();

            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    void OnExit(object sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = (MainWindowViewModel)desktop.MainWindow?.DataContext;
            if (vm != null)
                vm.Dispose();
        }
    }
}