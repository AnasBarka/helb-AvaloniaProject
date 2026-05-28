using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using OceanStock.Models;

namespace OceanStock.Services;

public class JSONServices
{
    private static readonly HttpClient _httpClient = new HttpClient();

    private const string BaseUrl = "http://185.157.245.38:8080/json";

    internal async Task<List<ProductFish>> GetProductsAsync()
    {
        const string url = $"{BaseUrl}?FileName=ProductFish.json";

        using var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return new List<ProductFish>();

        await using var contentStream = await response.Content.ReadAsStreamAsync();

        return await JsonSerializer.DeserializeAsync<List<ProductFish>>(contentStream)
               ?? new List<ProductFish>();
    }

    internal async Task SetProductsAsync(List<ProductFish> products)
    {
        using var memoryStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(memoryStream, products);
        memoryStream.Position = 0;

        var fileContent = new StreamContent(memoryStream)
        {
            Headers = { ContentType = new MediaTypeHeaderValue("application/json") }
        };

        var content = new MultipartFormDataContent
        {
            { fileContent, "file", "ProductFish.json" }
        };

        using var response = await _httpClient.PostAsync(BaseUrl, content);
    }
}
