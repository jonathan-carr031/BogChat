using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using BogChatDesktopClient.Extensions;
using BogChatDesktopClient.ViewModels;
using BogChatDesktopClient.Views;
using Microsoft.Extensions.DependencyInjection;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "BogChatDesktopClient.Controls")]

namespace BogChatDesktopClient;

[SupportedOSPlatform("windows")]
public class App : Application {
    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted() {
        var services = new ServiceCollection().AddApplicationServices();

        var provider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            var splashScreenViewModel = new SplashScreenViewModel();
            var splashScreen = new SplashScreen {
                DataContext = splashScreenViewModel
            };

            desktop.MainWindow = splashScreen;

            try {
                splashScreenViewModel.StartupMessage = "Checking For Updates...";
                await splashScreenViewModel.CheckForUpdates();
            }
            catch (TaskCanceledException) {
                splashScreen.Close();
                return;
            }

            var mainWindowViewModel = provider.GetRequiredService<MainWindowViewModel>();

            var mainWindow = new MainWindow {
                DataContext = mainWindowViewModel
            };

            desktop.MainWindow = mainWindow;
            mainWindow.Show();

            splashScreen.Close();

            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e) {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            // var vm = (MainWindowViewModel)desktop.MainWindow?.DataContext!;
        }
    }
}