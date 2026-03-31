using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using MyProjectBase.Models;

namespace MyProjectBase.Services
{
    public class JSONServices
    {
        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        });
        
        private const string BaseUrl = "http://localhost:8080/json";

        internal async Task<List<StrangeAnimal>> GetStrangeAnimalsAsync()
        {
            const string url = $"{BaseUrl}?FileName=MyStrangeAnimals.json";

            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)return new List<StrangeAnimal>();
            
            await using var contentStream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<List<StrangeAnimal>>(contentStream) ?? new List<StrangeAnimal>();
        }

        internal async Task SetStrangeAnimalsAsync(List<StrangeAnimal> strangeAnimals)
        {
            var url = BaseUrl;

            using var memoryStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(memoryStream, strangeAnimals);
            memoryStream.Position = 0;

            var fileContent = new StreamContent(memoryStream)
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json") }
            };

            var content = new MultipartFormDataContent
            {
                { fileContent, "file", "MyStrangeAnimals.json" }
            };

            using var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                
            }
        }
    }
}