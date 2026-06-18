using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using BogChatDesktopClient.Models;
using BogChatDesktopClient.Services;
using BogChatDesktopClient.Services.ApiServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BogChatDesktopClient.ViewModels.Controls;

public partial class TextChannelViewModel : ViewModelBase {
    private readonly ApiService _apiService;
    private readonly IAppSessionService _appSessionService;
    private readonly GifService _gifService;
    public readonly Guid ChannelId;

    private CancellationTokenSource _cancellationTokenSource = new();


    [ObservableProperty] private string _channelName;
    [ObservableProperty] private string _gifSearchTerm = "";
    [ObservableProperty] private bool _gifSectionOpen;
    [ObservableProperty] private GifResponse? _gifToSend;
    [ObservableProperty] private bool _hasGifPreview;
    [ObservableProperty] private string? _messageText;
    private CancellationToken _token = CancellationToken.None;

    public TextChannelViewModel(ApiService apiService, Channel channel, IAppSessionService appSessionService,
        GifService gifService) {
        _apiService = apiService;
        ChannelId = channel.Id;
        _channelName = channel.Name;
        _appSessionService = appSessionService;
        _gifService = gifService;

        Task.Run(() => {
            _ = FetchMessages();
            _ = ListenForMessages();
        });
    }

    public ObservableCollection<ChannelMessage> Messages { get; set; } = [];
    public ObservableCollection<GifResponse> GifDataList { get; set; } = [];


    private async Task FetchMessages() {
        var messages = await _apiService.GetMessages(ChannelId);

        foreach (var message in messages) {
            message.IsSelf = message.UserId == _appSessionService.CurrentUser.Id;
            Messages.Add(message);
        }
    }

    [RelayCommand]
    private async Task SendMessage() {
        var tempStringToSend = GifToSend?.Images?.Original.Url + MessageText;
        if (string.IsNullOrWhiteSpace(tempStringToSend)) return;

        if (await _apiService.SaveMessage(ChannelId, tempStringToSend)) {
            MessageText = string.Empty;
            GifToSend = null;
            HasGifPreview = false;
            GifDataList.Clear();
        }
    }

    private async Task ListenForMessages() {
        var newMessages = _apiService.ListenForMessages(ChannelId);

        var enumerator = newMessages.GetAsyncEnumerator();

        while (await enumerator.MoveNextAsync()) {
            var messages = enumerator.Current;
            if (messages == null) continue;
            foreach (var message in messages) {
                message.IsSelf = message.UserId == _appSessionService.CurrentUser.Id;
                Messages.Add(message);
            }
        }
    }

    private async Task FetchGifs() {
        GifDataList.Clear();
        var gifDataResponse = await _gifService.SearchGifs(GifSearchTerm);

        if (gifDataResponse == null) {
            Console.WriteLine("No gifs found");
            return;
        }

        foreach (var gifResponse in gifDataResponse.Data) {
            GifDataList.Add(gifResponse);
        }
    }

    public async Task SearchGifs() {
        GifDataList.Clear();

        await _cancellationTokenSource.CancelAsync();
        _cancellationTokenSource = new CancellationTokenSource();
        _token = _cancellationTokenSource.Token;

        try {
            await Task.Delay(TimeSpan.FromMilliseconds(750), _token);

            Console.WriteLine($"operation completed @ {DateTime.Now}");
            await FetchGifs();
        }
        catch (OperationCanceledException) {
            Console.WriteLine("SearchGifs cancelled");
        }
    }

    [RelayCommand]
    public void AddGifToMessage(GifResponse? gifResponse) {
        HasGifPreview = false;
        if (gifResponse?.Images == null) {
            return;
        }

        GifSearchTerm = string.Empty;
        GifSectionOpen = false;
        GifToSend = gifResponse;
        Console.WriteLine(GifToSend);
        HasGifPreview = true;
    }

    [RelayCommand]
    private void RemoveGif() {
        GifToSend = null;
        HasGifPreview = false;
    }
}