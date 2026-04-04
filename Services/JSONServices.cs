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
            // ⚠️ Autorise les certificats non sécurisés (API locale)
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        });

        //  Adresse du serveur JSON Docker
        private const string BaseUrl = "http://185.157.245.38:8080/json";
        //  Charge la liste de produits depuis le serveur
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

        // ✅ Envoie la liste des produits au serveur
        internal async Task SetProductsAsync(List<ProductFish> products)
        {
            var url = BaseUrl;

            using var memoryStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(memoryStream, products);
            memoryStream.Position = 0;

            var fileContent = new StreamContent(memoryStream)
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json") }
            };

            // ✅ L’API attend "file" + nom du fichier JSON
            var content = new MultipartFormDataContent
            {
                { fileContent, "file", "ProductFish.json" }
            };

            using var response = await _httpClient.PostAsync(url, content);

            // Tu peux ajouter un log ici si tu veux surveiller :
            // Console.WriteLine("API Response: " + response.StatusCode);
        }
    }
}