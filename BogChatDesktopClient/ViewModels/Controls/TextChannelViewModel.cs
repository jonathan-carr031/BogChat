using System;
using System.Collections.ObjectModel;
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
    private readonly Guid _channelId;
    [ObservableProperty] private string _channelName = "Text Channel";

    [ObservableProperty] private string? _messageText;

    public TextChannelViewModel(ApiService apiService, Channel channel, IAppSessionService appSessionService) {
        _apiService = apiService;
        _channelId = channel.Id;
        _channelName = channel.Name;
        _appSessionService = appSessionService;

        Task.Run(() => {
            _ = FetchMessages();
            _ = ListenForMessages();
        });
    }

    public ObservableCollection<ChannelMessage> Messages { get; set; } = [];

    private async Task FetchMessages() {
        var messages = await _apiService.GetMessages(_channelId);
        messages.Reverse();

        foreach (var message in messages) {
            message.IsSelf = message.UserId == _appSessionService.CurrentUser.Id;
            Messages.Add(message);
        }
    }

    [RelayCommand]
    private async Task SendMessage() {
        if (string.IsNullOrWhiteSpace(MessageText)) return;

        if (await _apiService.SaveMessage(_channelId, MessageText)) {
            MessageText = string.Empty;
        }
    }

    private async Task ListenForMessages() {
        var newMessages = _apiService.ListenForMessages(_channelId);

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
}