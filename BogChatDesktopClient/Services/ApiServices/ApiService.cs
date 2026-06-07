using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.Models;

namespace BogChatDesktopClient.Services.ApiServices;

public class ApiService {
    private Uri _hostUri = new("https://localhost:5001");
    private HttpClient _httpClient;

    public ApiService(HttpClient httpClient) {
        _httpClient = httpClient;
    }

    public async Task<List<Channel>> GetChannels() {
        await Task.Delay(1);
        return [
            new Channel {
                Id = new Guid("32a561e3-3461-409c-9cf0-373f44edc189"),
                ChannelType = ChannelType.Voice,
                Name = "The Bog",
                Description = "Voice Channel for Talking"
            },
            new Channel {
                Id = new Guid("63859e68-bfa2-452b-bae0-349da815f531"),
                ChannelType = ChannelType.Text,
                Name = "The Ancient Scrolls",
                Description = "Text Channel for texting"
            },
            new Channel {
                Id = new Guid("ec5e0b82-b67d-447e-9343-b074ed1e6ce9"),
                ChannelType = ChannelType.Afk,
                Name = "AFK",
                Description = "Afk Channel"
            }
        ];
    }
}