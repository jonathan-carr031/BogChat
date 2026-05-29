using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BogChatDesktopClient.Views;

public partial class HomePageView : UserControl {
    public HomePageView() {
        InitializeComponent();

        VersionNumber.Text = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
    }

    private void Mute(object? sender, RoutedEventArgs routedEventArgs) {
        MuteButton.IsVisible = false;
        UnmuteButton.IsVisible = true;
    }

    private void UnMute(object? sender, RoutedEventArgs routedEventArgs) {
        MuteButton.IsVisible = true;
        UnmuteButton.IsVisible = false;
    }

    private void JoinRoom(object? sender, RoutedEventArgs e) {
        if (string.IsNullOrWhiteSpace(Username.Text)) return;

        LeaveRoomButton.IsVisible = true;
        JoinRoomButton.IsVisible = false;
        ContentPanel.IsVisible = true;
    }


    private void LeaveRoom(object? sender, RoutedEventArgs e) {
        JoinRoomButton.IsVisible = true;
        LeaveRoomButton.IsVisible = false;
        ContentPanel.IsVisible = false;
    }
}