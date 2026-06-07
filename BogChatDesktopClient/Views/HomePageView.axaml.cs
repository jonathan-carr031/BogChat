using System.Collections.ObjectModel;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BogChatDesktopClient.Controls;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.ViewModels;

namespace BogChatDesktopClient.Views;

public partial class HomePageView : UserControl {
    private int? _columnIndex;
    private int? _columnSpan;

    private StreamPane? _maximizedStreamPane;
    private int? _rowIndex;
    private int? _rowSpan;
    private ObservableCollection<RoomParticipant> _users = [];
    private WindowState _windowState;

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
        VideoPanel.IsVisible = true;
    }

    private void LeaveRoom(object? sender, RoutedEventArgs e) {
        // JoinRoomButton.IsVisible = true;
        VideoPanel.IsVisible = false;
    }

    private void StreamClicked(object? sender, PointerPressedEventArgs e) {
        if (e.Source is Border userCard) {
            if (_maximizedStreamPane != null) {
                ResetStreamPane();
                return;
            }

            if (userCard.Parent is StreamPane streamPane) {
                StoreStreamPaneSettings(streamPane);

                MaximizeStreamPane(streamPane);
            }
        }
    }

    private void StoreStreamPaneSettings(StreamPane streamPane) {
        _maximizedStreamPane = streamPane;
        _rowIndex = Grid.GetRow(streamPane);
        _columnIndex = Grid.GetColumn(streamPane);
        _rowSpan = Grid.GetRowSpan(streamPane);
        _columnSpan = Grid.GetColumnSpan(streamPane);

        if (TopLevel.GetTopLevel(this) is Window window) {
            _windowState = window.WindowState;
        }
    }

    private void MaximizeStreamPane(StreamPane streamPane) {
        streamPane.ZIndex = 1;
        streamPane.Margin = new Thickness(0);

        Grid.SetRow(streamPane, 0);
        Grid.SetColumn(streamPane, 0);

        Grid.SetColumnSpan(streamPane, 99);
        Grid.SetRowSpan(streamPane, 99);


        if (TopLevel.GetTopLevel(this) is Window window) {
            window.WindowState = WindowState.FullScreen;
            window.Padding = new Thickness(0);
            LeftPanel.IsVisible = false;
            RightPanel.IsVisible = false;
        }
    }

    private void ResetStreamPane() {
        Grid.SetRow(_maximizedStreamPane, _rowIndex.Value);
        Grid.SetRowSpan(_maximizedStreamPane, _rowSpan.Value);
        Grid.SetColumn(_maximizedStreamPane, _columnIndex.Value);
        Grid.SetColumnSpan(_maximizedStreamPane, _columnSpan.Value);

        _maximizedStreamPane.ZIndex = 0;
        _maximizedStreamPane.Margin = new Thickness(12);

        _maximizedStreamPane = null;
        _rowIndex = null;
        _columnIndex = null;
        _rowSpan = null;
        _columnSpan = null;

        if (TopLevel.GetTopLevel(this) is Window window) {
            window.WindowState = _windowState;
            window.Padding = new Thickness(0);
            LeftPanel.IsVisible = true;
            RightPanel.IsVisible = true;
        }
    }

    private void MaximizeContentPane() {
        if (TopLevel.GetTopLevel(this) is not Window window) return;

        _windowState = window.WindowState;
        window.WindowState = WindowState.FullScreen;
        window.Padding = new Thickness(0);
        LeftPanel.IsVisible = false;
        RightPanel.IsVisible = false;
    }

    private void RestoreContentPane() {
        if (TopLevel.GetTopLevel(this) is not Window window) return;

        window.WindowState = _windowState;
        window.Padding = new Thickness(0);
        LeftPanel.IsVisible = true;
        RightPanel.IsVisible = true;
    }

    private void Enlarge(object? sender, PointerPressedEventArgs e) {
        if (e.Source is Image userCard) {
            if (DataContext is HomePageViewModel homePageViewModel) {
                homePageViewModel.MaximizedParticipant = userCard.DataContext as RoomParticipant;
                MaximizeContentPane();
            }
        }

        if (e.Source is Border userCardBorder) {
            if (DataContext is HomePageViewModel homePageViewModel) {
                homePageViewModel.MaximizedParticipant = userCardBorder.DataContext as RoomParticipant;
                MaximizeContentPane();
            }
        }
    }

    private void ResetStreamPaneSize(object? sender, RoutedEventArgs routedEventArgs) {
        if (DataContext is not HomePageViewModel homePageViewModel) return;

        homePageViewModel.MaximizedParticipant = null;
        RestoreContentPane();
    }

    private void Logout_Event(object? sender, RoutedEventArgs e) {
        if (DataContext is not HomePageViewModel homePageViewModel) return;
        DataSaver.DeleteCredentials();
        homePageViewModel.Logout();
    }
}