using System;
using System.Collections.ObjectModel;
using BogChatDesktopClient.Data;

namespace BogChatDesktopClient.Models;

public class Channel {
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public ChannelType ChannelType { get; set; }

    public ObservableCollection<string> Participants { get; set; } = [];
}