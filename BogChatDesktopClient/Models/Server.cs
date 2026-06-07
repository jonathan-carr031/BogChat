using System;
using System.Collections.Generic;

namespace BogChatDesktopClient.Models;

public class Server {
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public List<Channel> Channels { get; set; } = [];
}