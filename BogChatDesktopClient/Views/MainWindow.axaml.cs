using System.Reflection;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BogChatDesktopClient.Views;

public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();

        VersionNumber.Text = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        // var username = DataSaver.FetchData();
        // Console.WriteLine($"Username: {username}");
        // if (!string.IsNullOrWhiteSpace(username)) {
        // Username.Text = username;
        // }
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
        var username = Username.Text;
        if (string.IsNullOrWhiteSpace(username)) return;

        LeaveRoomButton.IsVisible = true;
        JoinRoomButton.IsVisible = false;

        // DataSaver.TestEncryptionAndDecryption(username);
        // DataSaver.SaveData(username);
    }


    private void LeaveRoom(object? sender, RoutedEventArgs e) {
        JoinRoomButton.IsVisible = true;
        LeaveRoomButton.IsVisible = false;
    }

    private void StartStreaming(object? sender, RoutedEventArgs e) {
        StartStreamButton.IsVisible = false;
        StopStreamButton.IsVisible = true;
    }

    private void StopStreaming(object? sender, RoutedEventArgs e) {
        StartStreamButton.IsVisible = true;
        StopStreamButton.IsVisible = false;
    }
}