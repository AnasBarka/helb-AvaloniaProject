using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace OceanStock.Helpers;

public static class ImageHelper
{
    public static Bitmap? LoadFromDisk(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        return new Bitmap(path);
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
            Console.WriteLine($"Erreur téléchargement image '{url}' : {ex.Message}");
            return null;
        }
    }

    // Copie l'image dans le dossier Assets local et retourne le chemin disque.
    public static string EnsureInAssets(string? sourcePath, string fishId)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            return "";

        var assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");

        // Chemin avares:// stocké avant la correction → on tente de retrouver le fichier sur disque
        if (sourcePath.StartsWith("avares://OceanStock/Assets/", StringComparison.OrdinalIgnoreCase))
        {
            var fileName = sourcePath["avares://OceanStock/Assets/".Length..];
            var diskPath = Path.Combine(assetsDir, fileName);
            return File.Exists(diskPath) ? diskPath : "";
        }

        // Déjà dans notre dossier Assets sur disque
        if (sourcePath.StartsWith(assetsDir, StringComparison.OrdinalIgnoreCase))
            return sourcePath;

        // Chemin disque externe → copier dans Assets
        try
        {
            Directory.CreateDirectory(assetsDir);
            var fileName = Path.GetFileName(sourcePath);
            var destPath = Path.Combine(assetsDir, fileName);

            if (!File.Exists(destPath))
                File.Copy(sourcePath, destPath);

            return destPath;
        }
        catch
        {
            return "";
        }
    }
}
