using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace OceanStock.Helpers
{
    public static class ImageHelper
    {
        public static Bitmap LoadFromResource(Uri resourceUri)
        {
            return new Bitmap(AssetLoader.Open(resourceUri));
        }
        public static Bitmap LoadFromFile(string filePath)
        {
            return new Bitmap(filePath);
        }

        public static async Task<Bitmap?> LoadFromWeb(Uri url)
        {
            using var httpClient = new HttpClient();
            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var data = await response.Content.ReadAsByteArrayAsync();
                return new Bitmap(new MemoryStream(data));
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"An error occurred while downloading image '{url}' : {ex.Message}");
                return null;
            }
        }
        
        public static string EnsureInAssets(string? path, string fishId)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "avares://OceanStock/Assets/placeholder.png";

            // ✅ Déjà OK → on garde
            if (path.StartsWith("avares://"))
                return path;

            try
            {
                var assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");
                Directory.CreateDirectory(assetsDir);

                var fileName = Path.GetFileName(path);
                var destPath = Path.Combine(assetsDir, fileName);

                // ✅ Si le fichier n'existe pas → on copie
                if (!File.Exists(destPath))
                {
                    File.Copy(path, destPath, overwrite: false);
                }

                // ✅ On retourne TOUJOURS un path propre
                return $"avares://OceanStock/Assets/{fileName}";
            }
            catch
            {
                return "avares://OceanStock/Assets/placeholder.png";
            }
        }
    }
}