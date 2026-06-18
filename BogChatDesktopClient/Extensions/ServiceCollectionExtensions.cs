using System;
using System.Net.Http;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.Factories;
using BogChatDesktopClient.Features.VideoCapture;
using BogChatDesktopClient.ScreenCapture;
using BogChatDesktopClient.Services;
using BogChatDesktopClient.Services.ApiServices;
using BogChatDesktopClient.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using HomePageViewModel = BogChatDesktopClient.ViewModels.Pages.HomePageViewModel;
using LoginPageViewModel = BogChatDesktopClient.ViewModels.Pages.LoginPageViewModel;
using PageViewModel = BogChatDesktopClient.ViewModels.Pages.PageViewModel;

namespace BogChatDesktopClient.Extensions;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddApplicationServices(this IServiceCollection services) {
        return services.AddCommonServices()
            .AddFactories()
            .AddDelegates()
            .AddViewModels()
            .AddCustomServices();
    }

    private static IServiceCollection AddCommonServices(this IServiceCollection services) {
        services.AddLogging();
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        services.AddTransient<HttpClient>();

        return services;
    }

    private static IServiceCollection AddViewModels(this IServiceCollection services) {
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<HomePageViewModel>();
        services.AddTransient<LoginPageViewModel>();
        services.AddTransient<SplashScreenViewModel>();
        services.AddTransient<StreamPaneViewModel>();

        return services;
    }

    private static IServiceCollection AddFactories(this IServiceCollection services) {
        services.AddSingleton<PageFactory>();

        return services;
    }

    private static IServiceCollection AddDelegates(this IServiceCollection services) {
        services.AddSingleton<Func<PageName, PageViewModel>>(provider => pageName => pageName switch {
            PageName.HomePage => provider.GetRequiredService<HomePageViewModel>(),
            PageName.LoginPage => provider.GetRequiredService<LoginPageViewModel>(),
            PageName.SplashScreen => provider.GetRequiredService<SplashScreenViewModel>(),
            _ => throw new ArgumentOutOfRangeException(nameof(pageName), pageName, null)
        });

        return services;
    }

    private static IServiceCollection AddCustomServices(this IServiceCollection services) {
        services.AddTransient<LiveKitService>();
        services.AddTransient<IScreenCapture, CopyScreenCapture>();
        services.AddTransient<AuthentikService>();
        services.AddTransient<ApiService>();
        services.AddSingleton<IAppSessionService, AppSessionService>();
        services.AddSingleton<OAuthService>();
        services.AddTransient<GifService>();

        return services;
    }
}