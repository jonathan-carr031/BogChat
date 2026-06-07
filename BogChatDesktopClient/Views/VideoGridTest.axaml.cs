using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using BogChatDesktopClient.Controls;
using BogChatDesktopClient.Data;

namespace BogChatDesktopClient.Views;

public partial class VideoGridTest : UserControl {
    private int? _columnIndex;
    private int? _columnSpan;

    private TextBlock? _maximizedStream;
    private StreamPane? _maximizedStreamPane;
    private int? _rowIndex;
    private int? _rowSpan;
    private List<string> _users = [];

    public VideoGridTest() {
        InitializeComponent();

        _users.Add("self");
        _users.Add("trash");
        _users.Add("azytzeen");
        _users.Add("ahr102");
        _users.Add("koldmilk");

        InitializeGrid();

        RoomParticipantList.CollectionChanged += OnRoomParticipantListOnCollectionChanged;
    }

    public ObservableCollection<RoomParticipant> RoomParticipantList { get; set; } = [];

    private void OnRoomParticipantListOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        Console.WriteLine($"Collection Updated");
        Console.WriteLine($"Sender: {sender}");
        Console.WriteLine($"NotifyCollectionChangedEventArgs: {e}");
    }


    private void InitializeGrid() {
        VideoGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
        VideoGrid.VerticalAlignment = VerticalAlignment.Stretch;
        VideoGrid.Background = Brush.Parse("Transparent");

        var columns = Math.Ceiling(Math.Sqrt(_users.Count));
        var rows = (int)Math.Ceiling(_users.Count / columns);
        var unitsPerColumn = Math.Max(_users.Count % columns, 1);

        for (var c = 0; c < columns * unitsPerColumn; c++) {
            VideoGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        for (var r = 0; r < rows; r++) {
            VideoGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        }

        var unitsPerRow = unitsPerColumn * columns;
        var lastRowCount = _users.Count % columns;
        if (lastRowCount == 0) lastRowCount = columns;
        var spanningForFinalRow = unitsPerRow / lastRowCount;

        var count = 0;
        for (var row = 0; row < rows; row++) {
            for (var column = 0; column < columns; column++) {
                if (count >= _users.Count) {
                    break;
                }

                var child = new StreamPane(new RoomParticipant {
                    Username = _users[count]
                }) {
                    Margin = new Thickness(12)
                };

                child.PointerPressed += StreamClicked;

                VideoGrid.Children.Add(child);

                Grid.SetRow(child, row);

                var spanningColumns = (int)unitsPerColumn;
                if (row == rows - 1) {
                    spanningColumns = (int)spanningForFinalRow;
                }

                Grid.SetColumn(child, column * spanningColumns);
                Grid.SetColumnSpan(child, spanningColumns);

                count++;
            }
        }
    }

    private void StreamClicked(object? sender, PointerPressedEventArgs e) {
        if (e.Source is TextBlock textBlock) {
            if (_maximizedStream != null) {
                ResetTextBlock();
                return;
            }

            StoreTextBlockSettings(textBlock);

            MaximizeTextBlock(textBlock);
        }

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

    private void StoreTextBlockSettings(TextBlock textBlock) {
        _maximizedStream = textBlock;
        _rowIndex = Grid.GetRow(textBlock);
        _columnIndex = Grid.GetColumn(textBlock);
        _rowSpan = Grid.GetRowSpan(textBlock);
        _columnSpan = Grid.GetColumnSpan(textBlock);
    }

    private void StoreStreamPaneSettings(StreamPane streamPane) {
        _maximizedStreamPane = streamPane;
        _rowIndex = Grid.GetRow(streamPane);
        _columnIndex = Grid.GetColumn(streamPane);
        _rowSpan = Grid.GetRowSpan(streamPane);
        _columnSpan = Grid.GetColumnSpan(streamPane);
    }

    private void MaximizeTextBlock(TextBlock textBlock) {
        textBlock.ZIndex = 1;
        textBlock.Background = new SolidColorBrush(Colors.Green);

        Grid.SetRow(textBlock, 0);
        Grid.SetColumn(textBlock, 0);

        Grid.SetColumnSpan(textBlock, 99);
        Grid.SetRowSpan(textBlock, 99);
    }

    private void MaximizeStreamPane(StreamPane streamPane) {
        streamPane.ZIndex = 1;
        // textBlock.Background = new SolidColorBrush(Colors.Green);

        Grid.SetRow(streamPane, 0);
        Grid.SetColumn(streamPane, 0);

        Grid.SetColumnSpan(streamPane, 99);
        Grid.SetRowSpan(streamPane, 99);
    }

    private void ResetTextBlock() {
        Grid.SetRow(_maximizedStream, _rowIndex.Value);
        Grid.SetRowSpan(_maximizedStream, _rowSpan.Value);
        Grid.SetColumn(_maximizedStream, _columnIndex.Value);
        Grid.SetColumnSpan(_maximizedStream, _columnSpan.Value);

        _maximizedStream.ZIndex = -0;
        _maximizedStream.Background = new SolidColorBrush(Colors.Red);

        _maximizedStream = null;
        _rowIndex = null;
        _columnIndex = null;
        _rowSpan = null;
        _columnSpan = null;
    }

    private void ResetStreamPane() {
        Grid.SetRow(_maximizedStreamPane, _rowIndex.Value);
        Grid.SetRowSpan(_maximizedStreamPane, _rowSpan.Value);
        Grid.SetColumn(_maximizedStreamPane, _columnIndex.Value);
        Grid.SetColumnSpan(_maximizedStreamPane, _columnSpan.Value);

        _maximizedStreamPane.ZIndex = -0;
        // _maximizedStreamPane.Background = new SolidColorBrush(Colors.Red);

        _maximizedStreamPane = null;
        _rowIndex = null;
        _columnIndex = null;
        _rowSpan = null;
        _columnSpan = null;
    }
}