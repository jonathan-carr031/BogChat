using BogChatDesktopClient.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BogChatDesktopClient.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection services)
    {
        services.AddTransient<LiveKitService>();
    }
}