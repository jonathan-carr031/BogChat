using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.Extensions;
using BogChatDesktopClient.Models;

namespace BogChatDesktopClient.Services.ApiServices;

public class ApiService(HttpClient httpClient, IAppSessionService appSessionService) {
    private readonly Uri _hostUri = new("http://localhost:5146");

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
                Id = new Guid("5eee9a58-4e71-47a0-a200-201833799432"),
                ChannelType = ChannelType.Text,
                Name = "The Ancient Scrolls",
                Description = "Text Channel for texting"
            },
            new Channel {
                Id = new Guid("efd5ab04-0d12-4473-a78a-9d50bfc856c3"),
                ChannelType = ChannelType.Text,
                Name = "Pickled Herring",
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

    public async Task<List<ChannelMessage>> GetMessages(Guid channelId) {
        httpClient.AddAuthorizationHeader(appSessionService.JwtToken);
        var response = await httpClient.GetAsync(new Uri(_hostUri, $"channels/{channelId}/messages"));

        if (response.IsSuccessStatusCode) {
            var result = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(result)) {
                return [];
            }

            return JsonSerializer.Deserialize<List<ChannelMessage>>(result, new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            }) ?? [];
        }

        return [];
    }

    public async Task<bool> SaveMessage(Guid channelId, string message) {
        httpClient.AddAuthorizationHeader(appSessionService.JwtToken);
        var messageString = JsonSerializer.Serialize(new {
            userId = appSessionService.CurrentUser.Id,
            message,
            channelId,
        });
        var requestBody = new StringContent(messageString, Encoding.UTF8, "application/json");
        var requestUri = new Uri(_hostUri, $"/channels/{channelId}/messages");
        var response = await httpClient.PostAsync(requestUri, requestBody);

        return response.IsSuccessStatusCode;
    }

    public async Task<User?> GetUser(Guid userId) {
        httpClient.AddAuthorizationHeader(appSessionService.JwtToken);
        var response = await httpClient.GetAsync(new Uri(_hostUri, $"/users/{userId}"));

        if (!response.IsSuccessStatusCode) {
            return null;
        }

        var result = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<User>(result);

        return user;
    }

    public async Task<User?> GetOrCreateUser(Guid userId, User user) {
        httpClient.AddAuthorizationHeader(appSessionService.JwtToken);

        var userString = JsonSerializer.Serialize(user);
        var requestBody = new StringContent(userString, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(new Uri(_hostUri, $"/users/{userId}"), requestBody);

        if (!response.IsSuccessStatusCode) {
            return null;
        }

        return JsonSerializer.Deserialize<User>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true
        });
    }

    public async IAsyncEnumerable<List<ChannelMessage>?> ListenForMessages(Guid channelId) {
        httpClient.AddAuthorizationHeader(appSessionService.JwtToken);
        var stream = await httpClient.GetStreamAsync(new Uri(_hostUri, $"/channels/{channelId}/live-updates"));

        var parser = SseParser.Create(stream, (_, data) => Encoding.UTF8.GetString(data));

        await foreach (var sseItem in parser.EnumerateAsync()) {
            yield return JsonSerializer.Deserialize<List<ChannelMessage>>(sseItem.Data, new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            });
        }
    }
}