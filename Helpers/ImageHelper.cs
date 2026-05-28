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

    // Dossier permanent dans AppData (survit aux rebuilds et aux cleans)
    private static string GetAssetsDir()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "OceanStock", "Assets");
    }

    // Copie l'image dans AppData\OceanStock\Assets\ et retourne le chemin absolu.
    // Retourne "" si la source est invalide ou introuvable.
    public static string EnsureInAssets(string? sourcePath, string fishId)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            return "";

        var assetsDir = GetAssetsDir();

        // Chemin avares:// (ancienne convention) : on cherche dans AppData
        if (sourcePath.StartsWith("avares://OceanStock/Assets/", StringComparison.OrdinalIgnoreCase))
        {
            var fileName = sourcePath["avares://OceanStock/Assets/".Length..];
            var diskPath = Path.Combine(assetsDir, fileName);
            return File.Exists(diskPath) ? diskPath : "";
        }

        // Déjà dans notre dossier AppData → rien à faire
        if (sourcePath.StartsWith(assetsDir, StringComparison.OrdinalIgnoreCase))
            return sourcePath;

        // Migration : chemin venant de l'ancien bin/Assets → on tente de trouver le fichier dans AppData
        var binAssets = Path.Combine(AppContext.BaseDirectory, "Assets");
        if (sourcePath.StartsWith(binAssets, StringComparison.OrdinalIgnoreCase))
        {
            var fileName = Path.GetFileName(sourcePath);
            var migrated = Path.Combine(assetsDir, fileName);
            if (File.Exists(migrated))
                return migrated;
            // tenter de copier depuis bin si le fichier existe encore
            if (File.Exists(sourcePath))
            {
                try
                {
                    Directory.CreateDirectory(assetsDir);
                    File.Copy(sourcePath, migrated, overwrite: false);
                    return migrated;
                }
                catch { }
            }
            return "";
        }

        // Chemin externe → copier dans AppData
        if (!File.Exists(sourcePath))
            return "";

        try
        {
            Directory.CreateDirectory(assetsDir);
            var destFileName = Path.GetFileName(sourcePath);
            var destPath = Path.Combine(assetsDir, destFileName);

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
