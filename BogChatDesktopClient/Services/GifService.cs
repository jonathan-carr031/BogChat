using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using BogChatDesktopClient.Models;

namespace BogChatDesktopClient.Services;

public class GifService(HttpClient httpClient) {
    private const string ApiKey = "NCRJl43bkjhojYvQW1uEye9LXyAYZNrj";
    private const string HostUrl = "https://api.giphy.com/v1/gifs";

    private readonly Uri _searchUri = new($"{HostUrl}/search");


    public async Task<GifDataResponse?> SearchGifs(string searchTerm, int limit = 10, int offset = 0) {
        var request = new UriBuilder(_searchUri);
        var query = HttpUtility.ParseQueryString(request.Query);
        query["api_key"] = ApiKey;
        query["q"] = searchTerm;
        query["limit"] = limit.ToString();
        query["offset"] = offset.ToString();
        request.Query = query.ToString();

        var httpResponse = await httpClient.GetAsync(request.ToString());

        if (!httpResponse.IsSuccessStatusCode) {
            Console.WriteLine(httpResponse.StatusCode);
        }

        var responseContent = await httpResponse.Content.ReadAsStringAsync();

        try {
            return JsonSerializer.Deserialize<GifDataResponse>(responseContent, new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception e) {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }

        return null;
    }
}