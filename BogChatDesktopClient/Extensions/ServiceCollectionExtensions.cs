using System;
using System.Net.Http;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.Factories;
using BogChatDesktopClient.Features.VideoCapture;
using BogChatDesktopClient.ScreenCapture;
using BogChatDesktopClient.Services;
using BogChatDesktopClient.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddTransient<StreamPaneViewModel>();

        return services;
    }

    private static IServiceCollection AddFactories(this IServiceCollection services) {
        services.AddSingleton<PageFactory>();

        return services;
    }

    private static IServiceCollection AddDelegates(this IServiceCollection services) {
        services.AddSingleton<Func<PageNames, PageViewModel>>(provider => pageName => pageName switch {
            PageNames.HomePage => provider.GetRequiredService<HomePageViewModel>(),
            PageNames.LoginPage => provider.GetRequiredService<LoginPageViewModel>(),
            _ => throw new ArgumentOutOfRangeException(nameof(pageName), pageName, null)
        });

        return services;
    }

    private static IServiceCollection AddCustomServices(this IServiceCollection services) {
        services.AddTransient<LiveKitService>();
        services.AddTransient<IScreenCapture, CopyScreenCapture>();
        services.AddTransient<AuthentikService>();

        return services;
    }
}