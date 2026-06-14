using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.Extensions;
using BogChatDesktopClient.ViewModels;
using BogChatDesktopClient.Views;
using Microsoft.Extensions.DependencyInjection;
using SplashScreen = BogChatDesktopClient.Views.SplashScreen;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "BogChatDesktopClient.Controls")]

namespace BogChatDesktopClient;

[SupportedOSPlatform("windows")]
public class App() : Application {
    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted() {
        var services = new ServiceCollection().AddApplicationServices();

        var provider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            var splashScreenViewModel = provider.GetRequiredService<SplashScreenViewModel>();
            var splashScreen = new SplashScreen {
                DataContext = splashScreenViewModel
            };

            desktop.MainWindow = splashScreen;
            PageName startingPage;

            try {
                startingPage = await splashScreenViewModel.InitializeApplication();
            }
            catch (TaskCanceledException) {
                splashScreen.Close();
                return;
            }

            var mainWindowViewModel = provider.GetRequiredService<MainWindowViewModel>();
            mainWindowViewModel.SetCurrentPage(startingPage);

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
        Console.WriteLine("Exiting...");
    }
}